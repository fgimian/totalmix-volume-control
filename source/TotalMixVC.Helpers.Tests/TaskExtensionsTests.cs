using System;
using System.Threading;
using System.Threading.Tasks;
using TotalMixVC.Helpers;
using Xunit;

namespace TotalMixVC.Helpers.Tests
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task TimeoutAfter_TaskCompletes_NoExceptionIsRaisedAsync()
        {
            // Arrange
            bool completed = false;
            Func<Task> task = new(async () =>
            {
                await Task.Delay(1).ConfigureAwait(false);
                completed = true;
            });

            // Act
            await Task.Run(task).TimeoutAfter(1000).ConfigureAwait(false);

            // Assert
            Assert.True(completed);
        }

        [Fact]
        public async Task TimeoutAfter_OnTimeout_ThrowsExceptionAsync()
        {
            // Arrange
            bool completed = false;
            Func<Task> task = new(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                completed = true;
            });

            // Act & Assert
            await Assert
                .ThrowsAsync<TimeoutException>(async () =>
                    await Task.Run(task).TimeoutAfter(1).ConfigureAwait(false))
                .ConfigureAwait(false);

            // Assert
            Assert.False(completed);
        }

        [Fact]
        public async Task TimeoutAfter_OnCancellation_ThrowsExceptionAsync()
        {
            // Arrange
            using CancellationTokenSource cancellationTokenSource = new();

            bool completed = false;
            Func<Task> task = new(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                completed = true;
            });

            // Act
            Task cancelTask = Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                cancellationTokenSource.Cancel();
            });

            // Assert
            await Assert
                .ThrowsAsync<OperationCanceledException>(async () =>
                    await Task
                        .Run(task)
                        .TimeoutAfter(1000, cancellationTokenSource)
                        .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.False(completed);
            await cancelTask.ConfigureAwait(false);
        }



        [Fact]
        public async Task TimeoutAfter_OnTaskWithReturn_ReturnsValueAsync()
        {
            // Arrange
            bool completed = false;
            Func<Task<string>> task = new(async () =>
            {
                await Task.Delay(1).ConfigureAwait(false);
                completed = true;
                return "Hello";
            });

            // Act
            string result = await Task.Run(task).TimeoutAfter(1000).ConfigureAwait(false);

            // Assert
            Assert.True(completed);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public async Task TimeoutAfter_OnTimeoutWithReturn_ThrowsExceptionAsync()
        {
            // Arrange
            bool completed = false;
            Func<Task<string>> task = new(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                completed = true;
                return "Hello";
            });

            // Act & Assert
            await Assert
                .ThrowsAsync<TimeoutException>(async () =>
                    await Task.Run(task).TimeoutAfter(1).ConfigureAwait(false))
                .ConfigureAwait(false);

            // Assert
            Assert.False(completed);
        }

        [Fact]
        public async Task TimeoutAfter_OnCancellationWithReturn_ThrowsExceptionAsync()
        {
            // Arrange
            using CancellationTokenSource cancellationTokenSource = new();

            bool completed = false;
            Func<Task<string>> task = new(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                completed = true;
                return "Hello";
            });

            // Act
            Task cancelTask = Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                cancellationTokenSource.Cancel();
            });

            // Assert
            await Assert
                .ThrowsAsync<OperationCanceledException>(async () =>
                    await Task
                        .Run(task)
                        .TimeoutAfter(1000, cancellationTokenSource)
                        .ConfigureAwait(false))
                .ConfigureAwait(false);
            Assert.False(completed);
            await cancelTask.ConfigureAwait(false);
        }
    }
}
