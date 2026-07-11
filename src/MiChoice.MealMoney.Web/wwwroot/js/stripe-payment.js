// Stripe Payment Element interop for MiMealMoney (live installs only).
// The Blazor page calls mount() with the publishable key + PaymentIntent client secret, then
// confirm() when the parent submits. Card data is entered into Stripe's iframe and never touches
// our server. Loaded only matters when real Stripe keys are configured; harmless otherwise.
window.miStripe = (function () {
    let stripe = null;
    let elements = null;

    return {
        // Mount the Payment Element into #payment-element. Returns { ok, error }.
        mount: async function (publishableKey, clientSecret) {
            try {
                if (typeof Stripe === "undefined") {
                    return { ok: false, error: "Stripe.js failed to load." };
                }
                stripe = Stripe(publishableKey);
                elements = stripe.elements({ clientSecret });
                const paymentElement = elements.create("payment");
                paymentElement.mount("#payment-element");
                return { ok: true, error: null };
            } catch (e) {
                return { ok: false, error: (e && e.message) ? e.message : "Could not initialize payment form." };
            }
        },

        // Confirm the payment with the entered card. No redirect (card-only, user present).
        // Returns { status, paymentIntentId, error }.
        confirm: async function () {
            if (!stripe || !elements) {
                return { status: "error", paymentIntentId: null, error: "Payment form is not ready." };
            }
            try {
                const result = await stripe.confirmPayment({ elements, redirect: "if_required" });
                if (result.error) {
                    return { status: "error", paymentIntentId: null, error: result.error.message || "Payment failed." };
                }
                const pi = result.paymentIntent;
                return { status: pi ? pi.status : "unknown", paymentIntentId: pi ? pi.id : null, error: null };
            } catch (e) {
                return { status: "error", paymentIntentId: null, error: (e && e.message) ? e.message : "Payment failed." };
            }
        }
    };
})();
