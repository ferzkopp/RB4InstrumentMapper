using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Implementation of a fixed-size concurrent queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizeConcurrentQueue<T> : ConcurrentQueue<T>
    {
        /// <summary>
        /// Sync root of the queue for locking.
        /// </summary>
        private readonly object privateLockObject = new object();

        /// <summary>
        /// Size of the queue.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Create a new instance of the FixedSizeConcurrentQueue.
        /// </summary>
        /// <param name="size">Size of the queue.</param>
        public FixedSizeConcurrentQueue(int size)
        {
            Size = size;
        }

        /// <summary>
        /// Enqueue an object into the queue. Maintains size limit of queue.
        /// </summary>
        /// <param name="obj">Object to enqueue</param>
        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (privateLockObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }
}
