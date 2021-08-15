using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace TotalMixVC.Helpers
{
    /// <summary>
    /// Provides various useful extensions to async tasks.
    /// </summary>
    [SuppressMessage(
        "Naming",
        "RCS1046:Asynchronous method name should end with 'Async'.",
        Justification = "The Async suffix is not added to conform to similar Task methods.")]
    public static class TaskExtensions
    {
        /// <summary>
        /// Runs a given task but times out after a given period of time if the task does not
        /// complete.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="millisecondsTimeout">The timeout in milliseconds.</param>
        /// <param name="cancellationTokenSource">
        /// A custom cancellation token source which may be cancelled by the caller before the
        /// timeout is exceeded.
        /// </param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="TimeoutException">Thrown if the task times out.</exception>
        public static async Task TimeoutAfter(
            this Task task,
            int millisecondsTimeout,
            CancellationTokenSource cancellationTokenSource = null)
        {
            using CancellationTokenSource timeoutCancellationTokenSource = new();

            List<Task> tasks = new()
            {
                task,
                Task.Delay(millisecondsTimeout, timeoutCancellationTokenSource.Token)
            };

            Task cancellationTask = null;
            if (cancellationTokenSource is not null)
            {
                cancellationTask = Task.Delay(-1, cancellationTokenSource.Token);
                tasks.Add(cancellationTask);
            }

            Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);

            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                await task.ConfigureAwait(false);
                return;
            }

            if (completedTask == cancellationTask)
            {
                timeoutCancellationTokenSource.Cancel();
                throw new OperationCanceledException();
            }

            throw new TimeoutException();
        }

        /// <summary>
        /// Runs a given task but times out after a given period of time if the task does not
        /// complete.
        /// </summary>
        /// <typeparam name="TResult">The type that will be returned by the task.</typeparam>
        /// <param name="task">The task to execute.</param>
        /// <param name="millisecondsTimeout">The timeout in milliseconds.</param>
        /// <param name="cancellationTokenSource">
        /// A custom cancellation token source which may be cancelled by the caller before the
        /// timeout is exceeded.
        /// </param>
        /// <returns>
        /// The task object representing the asynchronous operation which contains the result of
        /// the task.
        /// </returns>
        /// <exception cref="TimeoutException">Thrown if the task times out.</exception>
        public static async Task<TResult> TimeoutAfter<TResult>(
            this Task<TResult> task,
            int millisecondsTimeout,
            CancellationTokenSource cancellationTokenSource = null)
        {
            using CancellationTokenSource timeoutCancellationTokenSource = new();

            List<Task> tasks = new()
            {
                task,
                Task.Delay(millisecondsTimeout, timeoutCancellationTokenSource.Token)
            };

            Task cancellationTask = null;
            if (cancellationTokenSource is not null)
            {
                cancellationTask = Task.Delay(-1, cancellationTokenSource.Token);
                tasks.Add(cancellationTask);
            }

            Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);

            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task.ConfigureAwait(false);
            }

            if (completedTask == cancellationTask)
            {
                timeoutCancellationTokenSource.Cancel();
                throw new OperationCanceledException();
            }

            throw new TimeoutException();
        }
    }
}
