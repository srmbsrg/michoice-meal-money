// MiChoice unified dark/light theme toggle (pure-JS, works on static SSR + interactive pages).
// Sets data-theme (custom CSS vars) and data-bs-theme (Bootstrap 5.3 native dark mode) on <html>.
// Persists to localStorage; respects prefers-color-scheme on first visit.
(function () {
    var KEY = 'mc-theme';
    function apply(t) {
        var el = document.documentElement;
        el.setAttribute('data-theme', t);
        el.setAttribute('data-bs-theme', t);
    }
    window.mcTheme = {
        current: function () {
            try {
                var v = localStorage.getItem(KEY);
                if (v === 'dark' || v === 'light') return v;
                return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
            } catch (e) { return 'light'; }
        },
        set: function (t) {
            try { localStorage.setItem(KEY, t); } catch (e) { }
            apply(t);
        },
        toggle: function () { this.set(this.current() === 'dark' ? 'light' : 'dark'); }
    };
    // Apply before first paint to avoid a flash of the wrong theme.
    apply(window.mcTheme.current());
})();
