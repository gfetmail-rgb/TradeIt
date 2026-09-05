using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradeIt.Services
{
    /// <summary>
    /// Owns Auto Scroll timing and sequencing. Rendering remains in the UI layer;
    /// callbacks are marshalled back to the context that started the controller.
    /// </summary>
    internal sealed class AutoScrollController : IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private System.Threading.Timer? _timer;
        private Func<Task>? _tickAction;
        private SynchronizationContext? _context;
        private int _index;
        private int _count;
        private bool _running;

        public bool IsRunning => _running;
        public int CurrentIndex => _index;

        public bool Start(int count, int initialIndex, int intervalMilliseconds, Func<Task> tickAction)
        {
            if (count <= 0 || intervalMilliseconds <= 0 || tickAction == null)
                return false;

            Stop();
            _count = count;
            _index = Math.Clamp(initialIndex, 0, count - 1);
            _tickAction = tickAction;
            _context = SynchronizationContext.Current;
            _running = true;
            _timer = new System.Threading.Timer(OnTimer, null, intervalMilliseconds, intervalMilliseconds);
            return true;
        }

        public int MoveNext()
        {
            if (_count <= 0) return -1;
            _index++;
            if (_index >= _count) _index = 0;
            return _index;
        }

        public void Stop()
        {
            _running = false;
            _timer?.Dispose();
            _timer = null;
            _tickAction = null;
            _context = null;
            _count = 0;
            _index = -1;
        }

        private void OnTimer(object? state)
        {
            if (!_running || _tickAction == null || _context == null)
                return;

            _context.Post(async _ => await ExecuteTickAsync(), null);
        }

        private async Task ExecuteTickAsync()
        {
            if (!_running || _tickAction == null || !await _gate.WaitAsync(0))
                return;

            try
            {
                if (!_running || _tickAction == null) return;
                MoveNext();
                await _tickAction();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            Stop();
            _gate.Dispose();
        }
    }
}