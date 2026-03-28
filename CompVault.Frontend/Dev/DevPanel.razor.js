export function submitLoginForm(email) {
    const form = document.getElementById('dev-login-form');
    form.querySelector('input[name="OtpRequest.Email"]').value = email;
    form.submit();
}

export function submitLogoutForm() {
    document.getElementById('dev-logout-form').submit();
}
