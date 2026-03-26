using System;
using System.Threading;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal class LightweightTaskProcessor : ITaskProcessor
    {
        public async Task ProcessAsync(Func<Task> taskFunc)
        {
            await Task.Run(taskFunc);
        }

        public async Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken)
        {
            await Task.Run(() => taskFunc(cancellationToken), cancellationToken);
        }
    }
}
