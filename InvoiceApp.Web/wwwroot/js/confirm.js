// Intercepts submit buttons that carry data-confirm="message" and shows a Bootstrap
// confirmation modal. The owning form is submitted only after the user confirms.
(function () {
    var modalEl = document.getElementById('confirmModal');
    if (!modalEl || !window.bootstrap) return;

    var messageEl = document.getElementById('confirmModalMessage');
    var yesBtn = document.getElementById('confirmModalYes');
    var modal = window.bootstrap.Modal.getOrCreateInstance(modalEl);
    var pendingForm = null;
    var pendingButton = null;

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-confirm]');
        if (!button) return;
        var form = button.closest('form');
        if (!form) return;

        event.preventDefault();
        pendingForm = form;
        pendingButton = button;
        messageEl.textContent = button.getAttribute('data-confirm') || 'Are you sure?';
        modal.show();
    });

    yesBtn.addEventListener('click', function () {
        if (!pendingForm) return;
        var form = pendingForm;
        var button = pendingButton;
        pendingForm = null;
        pendingButton = null;
        modal.hide();

        // Preserve the clicked button's name/value (for handlers that rely on it).
        if (button && button.name) {
            var hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = button.name;
            hidden.value = button.value;
            form.appendChild(hidden);
        }
        // Bypass the busy-button handler's disable so the submit goes through.
        form.setAttribute('data-no-busy', 'true');
        form.requestSubmit ? form.requestSubmit() : form.submit();
    });
})();
