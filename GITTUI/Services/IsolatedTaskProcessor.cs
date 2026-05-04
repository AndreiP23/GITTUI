using System;
using System.Threading;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    /// <summary>
    /// Runs each task on a dedicated thread pool thread, isolated from the caller's context.
    /// Limits concurrent long-running threads to avoid resource exhaustion.
    /// </summary>
    internal class IsolatedTaskProcessor : ITaskProcessor
    {
        private readonly SemaphoreSlim _throttle = new(maxCount: 4, initialCount: 4);

        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await _throttle.WaitAsync();
            try
            {
                await Task.Factory.StartNew(
                    taskFunc,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                ).Unwrap();
            }
            finally
            {
                _throttle.Release();
            }
        }

        public async Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            await _throttle.WaitAsync(cancellationToken);
            try
            {
                await Task.Factory.StartNew(
                    () => taskFunc(cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                ).Unwrap();
            }
            finally
            {
                _throttle.Release();
            }
        }
    }
}
