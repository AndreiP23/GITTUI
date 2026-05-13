using System;
using System.Threading;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    /// <summary>
    /// Baseline control-group processor: executes each task synchronously on the
    /// caller's thread with no scheduling, queuing, or thread switching.
    /// Used as the zero-concurrency reference point for benchmark comparisons.
    /// </summary>
    internal sealed class SequentialTaskProcessor : ITaskProcessor
    {
        public Task ProcessAsync(Func<Task> taskFunc)
            => taskFunc();

        public Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return taskFunc(cancellationToken);
        }
    }
}
