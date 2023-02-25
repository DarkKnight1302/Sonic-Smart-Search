using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicExplorerLib.Utils
{
    public static class PostBootQueue
    {
        private static Queue<Action> queue = new Queue<Action>();
        private static object _lock = new object();
        private static volatile bool executedStated = false;

        public static void PushToQueue(Action action)
        {
            if (executedStated)
            {
                Task.Run(action);
                return;
            }
            lock (_lock)
            {
                if (executedStated)
                {
                    Task.Run(action);
                    return;
                }
                queue.Enqueue(action);
            }
        }

        public static void ExecuteQueue()
        {
            executedStated = true;
            lock (_lock)
            {
                while (queue.Count > 0)
                {
                    Action action = queue.Dequeue();
                    if (action != null)
                    {
                        Task.Run(action);
                    }
                }
            }
        }
    }
}
