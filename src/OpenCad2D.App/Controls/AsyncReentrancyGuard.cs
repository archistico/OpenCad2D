using System;
using System.Threading;

namespace OpenCad2D.App.Controls;

/// <summary>
/// Small non-blocking reentrancy guard for UI operations that may await,
/// such as modal text dialogs opened from canvas pointer input.
/// </summary>
internal sealed class AsyncReentrancyGuard
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryEnter(out IDisposable lease)
    {
        if (!_semaphore.Wait(0))
        {
            lease = EmptyLease.Instance;
            return false;
        }

        lease = new ReentrancyLease(_semaphore);
        return true;
    }

    private sealed class ReentrancyLease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public ReentrancyLease(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _semaphore.Release();
        }
    }

    private sealed class EmptyLease : IDisposable
    {
        public static readonly EmptyLease Instance = new();

        private EmptyLease()
        {
        }

        public void Dispose()
        {
        }
    }
}
