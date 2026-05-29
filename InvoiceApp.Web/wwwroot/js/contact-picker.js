// Device Contact Picker wiring.
//
// Buttons with class "contact-pick-btn" stay hidden (.d-none) until this
// script confirms the Web Contact Picker API is available — only Chrome /
// Edge / Samsung Internet on Android over HTTPS support it today.
//
// Each button declares its target inputs via data attributes:
//   data-pick-phone="<name attribute of the phone input>"
//   data-pick-name ="<name attribute of the linked name input>"  (optional)
// Inputs are looked up inside the button's nearest <form>.
(function () {
    if (!('contacts' in navigator) || !('ContactsManager' in window)) return;

    function init() {
        document.querySelectorAll('.contact-pick-btn').forEach(function (btn) {
            btn.classList.remove('d-none');
            btn.addEventListener('click', async function () {
                try {
                    var contacts = await navigator.contacts.select(['name', 'tel'], { multiple: false });
                    if (!contacts || contacts.length === 0) return;

                    var picked = contacts[0];
                    var form = btn.closest('form');
                    if (!form) return;

                    var phoneAttr = btn.dataset.pickPhone;
                    var nameAttr  = btn.dataset.pickName;
                    var phoneInput = phoneAttr ? form.querySelector('input[name="' + phoneAttr + '"]') : null;
                    var nameInput  = nameAttr  ? form.querySelector('input[name="' + nameAttr  + '"]') : null;

                    if (phoneInput && picked.tel && picked.tel.length > 0) {
                        phoneInput.value = picked.tel[0];
                    }
                    // Only fill name if it's empty, so edit forms don't clobber existing values.
                    if (nameInput && !nameInput.value && picked.name && picked.name.length > 0) {
                        nameInput.value = picked.name[0];
                    }
                } catch (err) {
                    // user cancelled or denied permission — no-op
                }
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
