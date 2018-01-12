using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Extensions
{
	internal static class TaskTimeoutExtension
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int delay)
        {
            await Task.WhenAny(task, Task.Delay(delay * 1000));

            if (!task.IsCompleted)
            {
	            throw new TimeoutException();
            }

	        return await task;
        }

        public static async Task TimeoutAfter(this Task task, int delay)
        {
            await Task.WhenAny(task, Task.Delay(delay * 1000));

            if (!task.IsCompleted)
            {
	            throw new TimeoutException();
            }

	        await task;
        }
    }
}
