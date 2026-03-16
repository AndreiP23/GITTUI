using System;

namespace GITTUI.Services
{
    internal static class TaskProcessorFactory
    {
        public static ITaskProcessor GetProcessor(TaskType taskType)
        {
            return taskType switch
            {
                TaskType.Lightweight => new LightweightTaskProcessor(),
                TaskType.Concurrent => new ConcurrentTaskProcessor(),
                TaskType.Isolated => new IsolatedTaskProcessor(),
                _ => throw new ArgumentException("Invalid task type")
            };
        }
    }

    internal enum TaskType
    {
        Lightweight,
        Concurrent,
        Isolated
    }
}
