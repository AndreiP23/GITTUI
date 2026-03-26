using System;
using System.Threading;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal interface ITaskProcessor
    {
        Task ProcessAsync(Func<Task> taskFunc);
        Task ProcessAsync(Func<CancellationToken, Task> taskFunc, CancellationToken cancellationToken);
    }
}
