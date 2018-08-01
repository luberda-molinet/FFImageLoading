using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace FFImageLoading
{
    public class ThreadSafeCollection<T> : ICollection<T>
    {
        readonly List<T> _list = new List<T>();
        readonly object _lock = new object();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IList<T>)_list).IsReadOnly;
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                return _list.ToList().GetEnumerator();
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        public void RemoveAll(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                foreach (var item in _list.ToList())
                {
                    if (predicate.Invoke(item))
                        _list.Remove(item);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
