using CompVault.Frontend.Common.Models;

namespace CompVault.Frontend.Common.Services;

public class AuthStateService
{
    private const int MaxAttempts = 5;

    public LoginAttemptState LoginState { get; private set; } = new();

    public event Action? OnChange;

    public bool CanAttemptLogin()
    {
        if (LoginState.IsLocked)
        {
            if (LoginState.LockedUntil.HasValue &&
                LoginState.LockedUntil.Value <= DateTime.UtcNow)
            {
                LoginState.Reset();
                Notify();
                return true;
            }

            return false;
        }

        return true;
    }

    public void RegisterFailedLogin()
    {
        LoginState.RegisterFail(MaxAttempts);
        Notify();
    }

    public void RegisterSuccess()
    {
        LoginState.Reset();
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}