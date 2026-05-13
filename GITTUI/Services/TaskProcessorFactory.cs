using System;
using System.Collections.Concurrent;

namespace GITTUI.Services
{
    internal class TaskProcessorFactory : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<TaskType, ITaskProcessor> _processors = new();
        private volatile TaskType _currentTaskType = TaskType.Concurrent;

        public IEnumerable<TaskType> AllTaskTypes => new[]
        {
            TaskType.Sequential,
            TaskType.Lightweight,
            TaskType.Concurrent,
            TaskType.Isolated
        };

        public TaskType CurrentTaskType => _currentTaskType;

        public ITaskProcessor GetCurrentProcessor()
        {
            return GetProcessor(_currentTaskType);
        }

        public void SetCurrentTaskType(TaskType taskType)
        {
            _currentTaskType = taskType;
        }

        public ITaskProcessor GetProcessor(TaskType taskType)
        {
            return _processors.GetOrAdd(taskType, type => type switch
            {
                TaskType.Sequential => new SequentialTaskProcessor(),
                TaskType.Lightweight => new LightweightTaskProcessor(),
                TaskType.Concurrent => new ConcurrentTaskProcessor(),
                TaskType.Isolated => new IsolatedTaskProcessor(),
                _ => throw new ArgumentException($"Invalid task type: {type}")
            });
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var processor in _processors.Values)
            {
                if (processor is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
            }
            _processors.Clear();
        }
    }

    internal enum TaskType
    {
        Sequential,
        Lightweight,
        Concurrent,
        Isolated
    }
}
