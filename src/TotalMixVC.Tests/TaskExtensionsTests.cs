﻿using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace TotalMixVC.Tests;

[SuppressMessage(
    "Usage",
    "MA0040:Forward the CancellationToken parameter to methods that take one",
    Justification = "Forwarding the cancellation token would invalidate these tests."
)]
public class TaskExtensionsTests
{
    [Fact]
    public async Task TimeoutAfter_Completes_DoesNotThrowException_Async()
    {
        // Arrange
        bool completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1);
            completed = true;
        };

        // Act
        await Task.Run(task).TimeoutAfter(1000);

        // Assert
        Assert.True(completed);
    }

    [Fact]
    public async Task TimeoutAfter_TimesOut_ThrowsException_Async()
    {
        // Arrange
        bool completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
        };

        // Act
        Func<Task> timeoutTask = () => Task.Run(task).TimeoutAfter(1);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(timeoutTask);
        Assert.False(completed);
    }

    [Fact]
    public async Task TimeoutAfter_Cancellation_ThrowsException_Async()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();

        bool completed = false;
        Func<Task> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
        };

        // Act
        Task cancelTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            await cancellationTokenSource.CancelAsync();
        });

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Task.Run(task).TimeoutAfter(1000, cancellationTokenSource)
        );
        Assert.False(completed);
        await cancelTask;
    }

    [Fact]
    public async Task TimeoutAfter_CompletesWithReturn_ReturnsValue_Async()
    {
        // Arrange
        bool completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1);
            completed = true;
            return "Hello";
        };

        // Act
        string result = await Task.Run(task).TimeoutAfter(1000);

        // Assert
        Assert.True(completed);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task TimeoutAfter_TimesOutWithReturn_ThrowsException_Async()
    {
        // Arrange
        bool completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
            return "Hello";
        };

        // Act
        Func<Task<string>> timeoutTask = () => Task.Run(task).TimeoutAfter(1);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(timeoutTask);
        Assert.False(completed);
    }

    [Fact]
    public async Task TimeoutAfter_CancellationWithReturn_ThrowsException_Async()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();

        bool completed = false;
        Func<Task<string>> task = async () =>
        {
            await Task.Delay(1000);
            completed = true;
            return "Hello";
        };

        // Act
        Task cancelTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            await cancellationTokenSource.CancelAsync();
        });

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Task.Run(task).TimeoutAfter(1000, cancellationTokenSource)
        );
        Assert.False(completed);
        await cancelTask;
    }
}
