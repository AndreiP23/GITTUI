using System;
using System.Threading;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    /// <summary>
    /// Runs each task on a dedicated thread pool thread, isolated from the caller's context.
    /// </summary>
    internal class IsolatedTaskProcessor : ITaskProcessor
    {
        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await Task.Factory.StartNew(
                taskFunc,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            ).Unwrap();
        }
    }
}
