using System;
using System.Collections.Concurrent;

namespace GITTUI.Services
{
    internal class TaskProcessorFactory
    {
        private readonly ConcurrentDictionary<TaskType, ITaskProcessor> _processors = new();

        public ITaskProcessor GetProcessor(TaskType taskType)
        {
            return _processors.GetOrAdd(taskType, type => type switch
            {
                TaskType.Lightweight => new LightweightTaskProcessor(),
                TaskType.Concurrent => new ConcurrentTaskProcessor(),
                TaskType.Isolated => new IsolatedTaskProcessor(),
                _ => throw new ArgumentException($"Invalid task type: {type}")
            });
        }
    }

    internal enum TaskType
    {
        Lightweight,
        Concurrent,
        Isolated
    }
}
