using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal class ConcurrentTaskProcessor : ITaskProcessor, IAsyncDisposable
    {
        private readonly Channel<QueuedTask> _taskChannel = Channel.CreateBounded<QueuedTask>(
            new BoundedChannelOptions(64) { FullMode = BoundedChannelFullMode.Wait });
        private readonly Task _workerTask;

        public ConcurrentTaskProcessor()
        {
            _workerTask = Task.Run(ProcessTasksAsync);
        }

        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await _taskChannel.Writer.WriteAsync(new QueuedTask(
                ExecuteAsync: _ => taskFunc(),
                CancellationToken: CancellationToken.None,
                Completion: completion));
            await completion.Task;
        }

        public async Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await _taskChannel.Writer.WriteAsync(new QueuedTask(taskFunc, cancellationToken, completion), cancellationToken);
            await completion.Task.WaitAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _taskChannel.Writer.Complete();
            await _workerTask;
        }

        private async Task ProcessTasksAsync()
        {
            await foreach (var queuedTask in _taskChannel.Reader.ReadAllAsync())
            {
                try
                {
                    queuedTask.CancellationToken.ThrowIfCancellationRequested();
                    await queuedTask.ExecuteAsync(queuedTask.CancellationToken);
                    queuedTask.Completion.SetResult();
                }
                catch (OperationCanceledException)
                {
                    queuedTask.Completion.SetCanceled();
                }
                catch (Exception ex)
                {
                    queuedTask.Completion.SetException(ex);
                }
            }
        }

        private sealed record QueuedTask(
            Func<CancellationToken, Task> ExecuteAsync,
            CancellationToken CancellationToken,
            TaskCompletionSource Completion);
    }
}
