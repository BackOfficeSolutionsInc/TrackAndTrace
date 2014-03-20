using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Pool<T>
    {
        private ConcurrentBag<T> _objects;
        private Func<T> _objectGenerator;
        private SemaphoreSlim _semaphore;
        private TimeSpan _maxWait;

        private object lck = new object();

        public Pool(int maxSize,TimeSpan maxWait, Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
            _semaphore=new SemaphoreSlim(maxSize);
            _maxWait = maxWait;
        }
        public Pool(Func<T> objectGenerator) : this(int.MaxValue,TimeSpan.FromTicks(0),objectGenerator)
        {
        }

        public async Task<T> GetObject()
        {
            T item;
            await _semaphore.WaitAsync(_maxWait);
            if (_objects.TryTake(out item))
                return item;
            return _objectGenerator();
        }

        public void PutObject(T item)
        {
            _objects.Add(item);
            _semaphore.Release();
        }

        public void DisposeObject(T item)
        {
            if (item is IDisposable)
            {
                ((IDisposable)item).Dispose();
                _semaphore.Release();
            }
            else
            {
                throw new TypeAccessException("Item is not IDisposable.");
            }
        }

        public int Available()
        {
            return _objects.Count();
        }
        public int InUse()
        {
            return _semaphore.CurrentCount;
        }

    }
}