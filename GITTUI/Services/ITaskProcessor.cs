using System;
using System.Threading.Tasks;

namespace GITTUI.Services
{
    internal interface ITaskProcessor
    {
        Task ProcessAsync(Func<Task> taskFunc);
    }
}
