using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal class ConcurrentTaskProcessor : ITaskProcessor
    {
        private readonly Channel<Func<Task>> _taskChannel = Channel.CreateUnbounded<Func<Task>>();

        public ConcurrentTaskProcessor()
        {
            // Start the worker to process tasks
            Task.Run(ProcessTasksAsync);
        }

        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await _taskChannel.Writer.WriteAsync(taskFunc);
        }

        private async Task ProcessTasksAsync()
        {
            await foreach (var taskFunc in _taskChannel.Reader.ReadAllAsync())
            {
                try
                {
                    await taskFunc();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Task failed: {ex.Message}");
                }
            }
        }
    }
}
