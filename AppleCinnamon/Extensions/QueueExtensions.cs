using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppleCinnamon.Extensions
{
    public static class QueueExtensions
    {
        public static Queue<T> Enqueue<T>(this Queue<T> queue, T item)
        {
            queue.Enqueue(item);
            return queue;
        }
    }
}
