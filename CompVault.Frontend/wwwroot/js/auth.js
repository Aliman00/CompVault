/**
 * Lar oss logge inn og setter token
 */

window.loginFormSubmit = function(accessToken, refreshToken, antiforgeryToken) {
    const form = document.getElementById('login-form');
    form.querySelector('#access-token-input').value = accessToken;
    form.querySelector('#refresh-token-input').value = refreshToken;

    let tokenInput = form.querySelector('[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        form.appendChild(tokenInput);
    }
    tokenInput.value = antiforgeryToken;
    form.submit();
};

/**
 * Logger ut brukeren
 */
window.logoutFormSubmit = function() {
    document.getElementById('profile-drawer-logout-form').submit();
};