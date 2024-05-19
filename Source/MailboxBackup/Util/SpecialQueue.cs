using System;
using System.Collections.Generic;

namespace MailboxBackup
{
    class SpecialQueue<T>
    {
        private readonly List<T> inner;

        public SpecialQueue(IEnumerable<T> initial)
        {
            this.inner = new List<T>(initial);
        }

        public int Count
        {
            get => inner.Count;
        }

        private T UnsafeDequeue()
        {
            var result = inner[0];
            inner.RemoveAt(0);
            return result;
        }

        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException();

            return UnsafeDequeue();
        }

        public bool TryDequeue(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }
            value = UnsafeDequeue();
            return true;
        }

        public void PutFront(T value)
        {
            inner.Insert(0, value);
        }

        public void PutFront(params T[] value)
        {
            inner.InsertRange(0, value);
        }

        public void PutFront(IEnumerable<T> values)
        {
            inner.InsertRange(0, values);
        }

    }
}