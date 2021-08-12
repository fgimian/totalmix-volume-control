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
        public async Task TimeoutAfter_Completes_DoesNotThrowException_Async()
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
        public async Task TimeoutAfter_TimesOut_ThrowsException_Async()
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
        public async Task TimeoutAfter_Cancellation_ThrowsException_Async()
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
        public async Task TimeoutAfter_CompletesWithReturn_ReturnsValue_Async()
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
        public async Task TimeoutAfter_TimesOutWithReturn_ThrowsException_Async()
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
        public async Task TimeoutAfter_CancellationWithReturn_ThrowsException_Async()
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
