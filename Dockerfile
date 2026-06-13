# =============================================================================
# Stage 1 — base runtime image (slim, ~220MB)
# This is what actually runs in production. We define it first so the final
# stage can reference it by alias.
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Tell Kestrel (ASP.NET's built-in web server) to listen on plain HTTP port 8080.
# Railway terminates TLS at its proxy; the container only ever sees HTTP internally.
ENV ASPNETCORE_URLS=http://+:8080

# Enable ASP.NET Core's built-in forwarded-headers middleware so that
# UseHttpsRedirection() sees the original HTTPS request from Railway's proxy
# instead of the plain HTTP that actually arrives at the container.
# Without this, you'd get an infinite redirect loop in Production.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# =============================================================================
# Stage 2 — build (full SDK, ~700MB — never ships to production)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ── Layer-caching trick ──────────────────────────────────────────────────────
# Copy ONLY the .csproj / .sln files first, then run dotnet restore.
# Docker caches each instruction as a layer. If none of these project files
# change between builds, Docker reuses the cached restore layer and skips
# re-downloading hundreds of NuGet packages — shaving minutes off CI builds.
# The layer is only invalidated when a dependency actually changes.
COPY ["MiChoice.MealMoney.sln", "."]
COPY ["src/MiChoice.MealMoney.Web/MiChoice.MealMoney.Web.csproj", "src/MiChoice.MealMoney.Web/"]
COPY ["src/MiChoice.MealMoney.Domain/MiChoice.MealMoney.Domain.csproj", "src/MiChoice.MealMoney.Domain/"]
COPY ["src/MiChoice.MealMoney.Infrastructure/MiChoice.MealMoney.Infrastructure.csproj", "src/MiChoice.MealMoney.Infrastructure/"]

RUN dotnet restore "src/MiChoice.MealMoney.Web/MiChoice.MealMoney.Web.csproj"

# Now copy all source code (invalidates cache only when source changes)
COPY . .

WORKDIR "/src/src/MiChoice.MealMoney.Web"
RUN dotnet build "MiChoice.MealMoney.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =============================================================================
# Stage 3 — publish (same SDK image, separate layer for cleanliness)
# dotnet publish = compile + tree-shake + write self-contained output to /app/publish
# /p:UseAppHost=false means no platform-specific executable wrapper is generated —
# we'll invoke the .dll directly via `dotnet` in the runtime image.
# =============================================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MiChoice.MealMoney.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =============================================================================
# Stage 4 — final (back to the slim runtime image)
# Only the compiled output from Stage 3 is copied in — the SDK never touches
# the final image. Result: ~220MB instead of ~700MB.
# =============================================================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MiChoice.MealMoney.Web.dll"]
