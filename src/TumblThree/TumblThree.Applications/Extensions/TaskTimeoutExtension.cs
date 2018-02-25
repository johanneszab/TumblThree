using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Extensions
{
    static class TaskTimeoutExtension
    {
        public async static Task<T> TimeoutAfter<T>(this Task<T> task, int delay)
        {
            await Task.WhenAny(task, Task.Delay(delay * 1000));

            if (!task.IsCompleted)
                throw new TimeoutException();

            return await task;
        }

        public async static Task TimeoutAfter(this Task task, int delay)
        {
            await Task.WhenAny(task, Task.Delay(delay * 1000));

            if (!task.IsCompleted)
                throw new TimeoutException();

            await task;
        }
    }
}
