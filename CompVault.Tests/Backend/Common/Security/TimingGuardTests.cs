using System.Diagnostics;
using CompVault.Backend.Common.Security;
using FluentAssertions;

namespace CompVault.Tests.Backend.Common.Security;

public class TimingGuardTests
{
    /// <summary>
    /// Tester at metoden delayer til mer enn 500 ms
    /// </summary>
    [Fact]
    public async Task TimingGuard_MustDelay()
    {
        // Arrange - starter 2 stk StopWatch-objekter. 1 for å ta total tid, og en til metoden
        var operationSw = Stopwatch.StartNew();
        var totalSw = Stopwatch.StartNew();
        
        // Act
        await TimingGuard.EnforceMinimumTimeAsync(operationSw, 500, CancellationToken.None);
        totalSw.Stop();
        
        // Assert
        totalSw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(500);
    }
}