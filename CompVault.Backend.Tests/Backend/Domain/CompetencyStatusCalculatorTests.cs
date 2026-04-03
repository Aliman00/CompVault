using CompVault.Backend.Features.Competencies;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Tests.Backend.Domain;

public class CompetencyStatusCalculatorTests
{
    // ======================== Null ExpiryDate ========================

    /// <summary>
    /// Tester at competencies uten utløpsdato alltid er gyldige.
    /// Dette støtter kompetansetyper som ikke krever utløp.
    /// </summary>
    [Fact]
    public void CalculateStatus_NullExpiryDate_ReturnsValid()
    {
        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(null);

        // Assert
        Assert.Equal(CompetencyStatus.Valid, status);
    }

    // ======================== Past ExpiryDate ========================

    /// <summary>
    /// Tester at kompetanser med utløpsdato i fortiden er utløpt.
    /// </summary>
    [Fact]
    public void CalculateStatus_PastExpiryDate_ReturnsExpired()
    {
        // Arrange
        DateTime pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(pastDate);

        // Assert
        Assert.Equal(CompetencyStatus.Expired, status);
    }

    /// <summary>
    /// Tester at kompetanser som akkurat har utløpt (ett sekund siden) er utløpt.
    /// </summary>
    [Fact]
    public void CalculateStatus_OneSecondInPast_ReturnsExpired()
    {
        // Arrange
        DateTime justInPast = DateTime.UtcNow.AddSeconds(-1);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(justInPast);

        // Assert
        Assert.Equal(CompetencyStatus.Expired, status);
    }

    /// <summary>
    /// Tester at kompetanser med utløpsdato akkurat nå regnes som utløpt.
    /// </summary>
    [Fact]
    public void CalculateStatus_ExactlyNow_ReturnsExpired()
    {
        // Arrange
        DateTime exactlyNow = DateTime.UtcNow;

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(exactlyNow);

        // Assert
        Assert.Equal(CompetencyStatus.Expired, status);
    }

    // ======================== ExpiringSoon Threshold ========================

    /// <summary>
    /// Tester at kompetanser som utløper innen 90 dager (threshold) er ExpiringSoon.
    /// </summary>
    [Fact]
    public void CalculateStatus_ExpiringSoon_ReturnsExpiringSoon()
    {
        // Arrange
        DateTime expiringSoon = DateTime.UtcNow.AddDays(30);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(expiringSoon);

        // Assert
        Assert.Equal(CompetencyStatus.ExpiringSoon, status);
    }

    /// <summary>
    /// Tester at kompetanser som akkurat er inne på grensen (akkurat 90 dager) er ExpiringSoon.
    /// </summary>
    [Fact]
    public void CalculateStatus_ExactlyNinetyDaysFromNow_ReturnsExpiringSoon()
    {
        // Arrange
        DateTime exactlyAtThreshold = DateTime.UtcNow.AddDays(CompetencyStatusCalculator.ExpiringSoonThresholdDays);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(exactlyAtThreshold);

        // Assert
        Assert.Equal(CompetencyStatus.ExpiringSoon, status);
    }

    /// <summary>
    /// Tester at kompetanser som akkurat er akkurat én dag over grensen er gyldige.
    /// </summary>
    [Fact]
    public void CalculateStatus_OneDayPastThreshold_ReturnsValid()
    {
        // Arrange
        DateTime justPastThreshold = DateTime.UtcNow.AddDays(CompetencyStatusCalculator.ExpiringSoonThresholdDays + 1);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(justPastThreshold);

        // Assert
        Assert.Equal(CompetencyStatus.Valid, status);
    }

    // ======================== Future ExpiryDate ========================

    /// <summary>
    /// Tester at kompetanser med utløpsdato langt i fremtiden er gyldige.
    /// </summary>
    [Fact]
    public void CalculateStatus_FutureExpiryDate_ReturnsValid()
    {
        // Arrange
        DateTime farFuture = DateTime.UtcNow.AddDays(365);

        // Act
        CompetencyStatus status = CompetencyStatusCalculator.Calculate(farFuture);

        // Assert
        Assert.Equal(CompetencyStatus.Valid, status);
    }
}
