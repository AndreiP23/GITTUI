using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal class ConcurrentTaskProcessor : ITaskProcessor, IAsyncDisposable
    {
        private readonly Channel<Func<Task>> _taskChannel = Channel.CreateUnbounded<Func<Task>>();
        private readonly Task _workerTask;

        public ConcurrentTaskProcessor()
        {
            _workerTask = Task.Run(ProcessTasksAsync);
        }

        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await _taskChannel.Writer.WriteAsync(taskFunc);
        }

        public async Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            await _taskChannel.Writer.WriteAsync(() => taskFunc(cancellationToken));
        }

        public async ValueTask DisposeAsync()
        {
            _taskChannel.Writer.Complete();
            await _workerTask;
        }

        private async Task ProcessTasksAsync()
        {
            await foreach (var taskFunc in _taskChannel.Reader.ReadAllAsync())
            {
                try
                {
                    await taskFunc();
                }
                catch (OperationCanceledException)
                {
                    // Task was cancelled, skip silently
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Task failed: {ex.Message}");
                }
            }
        }
    }
}
