// Prevent double-submits and show busy feedback on form submit buttons.
// Skips forms that opt out via data-no-busy, and buttons that open editors/modals.
(function () {
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.hasAttribute('data-no-busy')) return;

        // Use the button that triggered the submit when available.
        var button = form.querySelector('button[type="submit"]:not([data-no-busy])');
        if (!button) return;
        if (button.disabled) return;

        // Defer so client-side validation can cancel the submit first.
        setTimeout(function () {
            if (form.checkValidity && !form.checkValidity()) return;
            button.disabled = true;
            button.dataset.originalHtml = button.innerHTML;
            button.innerHTML =
                '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>' +
                '<span>Working…</span>';
        }, 0);
    }, true);
})();
