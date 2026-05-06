namespace CompVault.Frontend.Common.Models;

public class LoginAttemptState
{
    public int FailedAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }

    public void RegisterFail(int maxAttempts)
    {
        FailedAttempts++;

        if (FailedAttempts >= maxAttempts)
        {
            IsLocked = true;
            LockedUntil = DateTime.UtcNow.AddMinutes(10);
        }
    }

    public void Reset()
    {
        FailedAttempts = 0;
        IsLocked = false;
        LockedUntil = null;
    }
}