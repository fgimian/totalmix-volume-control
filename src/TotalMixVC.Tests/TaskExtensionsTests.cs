using Xunit;

namespace TotalMixVC.Tests;

public class TaskExtensionsTests
{
    [Fact]
    public async Task TimeoutAfter_Completes_DoesNotThrowException_Async()
    {
        var completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1);
            completed = true;
        };

        await Task.Run(task).TimeoutAfter(1000);

        Assert.True(completed);
    }

    [Fact]
    public async Task TimeoutAfter_TimesOut_ThrowsException_Async()
    {
        var completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
        };

        Func<Task> timeoutTask = () => Task.Run(task).TimeoutAfter(1);

        await Assert.ThrowsAsync<TimeoutException>(timeoutTask);
        Assert.False(completed);
    }

    [Fact]
    public async Task TimeoutAfter_Cancellation_ThrowsException_Async()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
        };

        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            await cancellationTokenSource.CancelAsync();
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Task.Run(task).TimeoutAfter(1000, cancellationTokenSource)
        );
        Assert.False(completed);
        await cancelTask;
    }

    [Fact]
    public async Task TimeoutAfter_CompletesWithReturn_ReturnsValue_Async()
    {
        var completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1);
            completed = true;
            return "Hello";
        };

        var result = await Task.Run(task).TimeoutAfter(1000);

        Assert.True(completed);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task TimeoutAfter_TimesOutWithReturn_ThrowsException_Async()
    {
        var completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
            return "Hello";
        };

        Func<Task<string>> timeoutTask = () => Task.Run(task).TimeoutAfter(1);

        await Assert.ThrowsAsync<TimeoutException>(timeoutTask);
        Assert.False(completed);
    }

    [Fact]
    public async Task TimeoutAfter_CancellationWithReturn_ThrowsException_Async()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
            return "Hello";
        };

        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            await cancellationTokenSource.CancelAsync();
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Task.Run(task).TimeoutAfter(1000, cancellationTokenSource)
        );
        Assert.False(completed);
        await cancelTask;
    }
}
