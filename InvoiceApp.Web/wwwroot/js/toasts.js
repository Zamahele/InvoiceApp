// Show any server-rendered toasts and provide a client-side helper.
(function () {
    function showToastElement(el) {
        if (window.bootstrap && window.bootstrap.Toast) {
            window.bootstrap.Toast.getOrCreateInstance(el).show();
        }
    }

    // Auto-show toasts rendered from TempData.
    document.querySelectorAll('[data-toast-autoshow]').forEach(showToastElement);

    // Client-side helper: window.showToast(message, isError)
    window.showToast = function (message, isError) {
        var container = document.getElementById('toastContainer');
        if (!container) return;
        var el = document.createElement('div');
        el.className = 'toast align-items-center border-0 shadow ' + (isError ? 'toast-error' : 'toast-success');
        el.setAttribute('role', 'status');
        el.innerHTML =
            '<div class="d-flex">' +
            '  <div class="toast-body"><i class="bi ' + (isError ? 'bi-exclamation-circle' : 'bi-check-circle') + ' me-2"></i>' +
            '  ' + message + '</div>' +
            '  <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';
        container.appendChild(el);
        showToastElement(el);
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
    };
})();
