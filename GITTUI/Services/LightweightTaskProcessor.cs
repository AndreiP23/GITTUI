using System;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal class LightweightTaskProcessor : ITaskProcessor
    {
        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await Task.Run(taskFunc);
        }
    }
}
