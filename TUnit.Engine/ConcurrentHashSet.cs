namespace TUnit.Engine;

internal class ConcurrentHashSet<T>
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly HashSet<T> _hashSet = [];

    #region Implementation of ICollection<T> ...ish

    public bool Add(T item)
    {
        _lock.EnterWriteLock();

        try
        {
            return _hashSet.Add(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();

        try
        {
            _hashSet.Clear();
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public bool Contains(T item)
    {
        _lock.EnterReadLock();

        try
        {
            return _hashSet.Contains(item);
        }
        finally
        {
            if (_lock.IsReadLockHeld)
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool Remove(T item)
    {
        _lock.EnterWriteLock();

        try
        {
            return _hashSet.Remove(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();

            try
            {
                return _hashSet.Count;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lock.Dispose();
        }
    }

    ~ConcurrentHashSet()
    {
        Dispose(false);
    }

    #endregion
}
