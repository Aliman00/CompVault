/**
 * Lar oss logge inn og setter token
 */

window.loginFormSubmit = function(code) {
    document.getElementById('otp-code-input').value = code;
    document.getElementById('login-form').submit();
};