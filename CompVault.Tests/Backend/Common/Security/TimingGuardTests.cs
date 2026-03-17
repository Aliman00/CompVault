using System.Diagnostics;
using CompVault.Backend.Common.Security;
using FluentAssertions;

namespace CompVault.Tests.Backend.Common.Security;

public class TimingGuardTests
{
    /// <summary>
    /// Tester at metoden delayer til mer enn 500 ms. Metoden gir noen ganger 499 ms, selvom vi
    /// tester 500. Løser det med en slack i Assert-seksjonen
    /// </summary>
    [Fact]
    public async Task TimingGuard_TimeIsLessThanMinimum_ShouldDelayToMinimumTime()
    {
        // Arrange - starter 2 stk StopWatch-objekter. 1 for å ta total tid, og en som simulerer
        // metoden som kaller TimingGuard
        var minimumMs = 500;
        var operationSw = Stopwatch.StartNew();
        var testingStopwatch = Stopwatch.StartNew();

        // Act
        await TimingGuard.EnforceMinimumTimeAsync(operationSw, minimumMs, CancellationToken.None);
        testingStopwatch.Stop();

        // Assert - Tiden må være høyere enn minimumMs. Slack på 10 ms
        testingStopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(minimumMs - 10);
    }

    /// <summary>
    /// Tester at metoden ikke delayer hvis innsendt StopWatch har brukt mer tid enn minimum tiden
    /// </summary>
    [Fact]
    public async Task TimingGuard_TimeIsHigherThanMinimum_ShouldNotDelay()
    {
        // Arrange - Starter metodens stopwatch som vi sikrer er høyere enn minimumtiden med en delay
        var minimumMs = 500;
        var operationSw = Stopwatch.StartNew();
        await Task.Delay(minimumMs + 100); // Sikrer at vi er over minimums tiden

        // Starter StopWatchen som måler selve metoden
        var testingStopwatch = Stopwatch.StartNew();
        var estimatedTestingTimeMs = 200;

        // Act
        await TimingGuard.EnforceMinimumTimeAsync(operationSw, minimumMs, CancellationToken.None);
        testingStopwatch.Stop();

        // Assert - Tiden selve EnforceMinimumTimeAsync brukte uten å delaye skal være lav
        testingStopwatch.ElapsedMilliseconds.Should().BeLessThan(estimatedTestingTimeMs);
    }

    /// <summary>
    /// Tester at metoden stopper StopWatch-objektet
    /// </summary>
    [Fact]
    public async Task TimingGuard_ShouldStopStopWatchObject()
    {
        // Arrange - Starter metodens stopwatch
        var minimumMs = 500;
        var operationSw = Stopwatch.StartNew();

        // Act
        await TimingGuard.EnforceMinimumTimeAsync(operationSw, minimumMs, CancellationToken.None);

        // Assert - Sjekker at den er stoppet
        operationSw.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Tester at CancellationToken stopper metoden og kaster OperationCanceledException
    /// </summary>
    [Fact]
    public async Task TimingGuard_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange - Starter metodens stopwatch
        var minimumMs = 10000;
        var operationSw = Stopwatch.StartNew();

        // Oppretter en CancellationToken som vi avbryter
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var token = cts.Token;

        // Act
        var act = async () =>
            await TimingGuard.EnforceMinimumTimeAsync(operationSw, minimumMs, token);

        // Assert - Sjekker at det ble kastet riktig error
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

}