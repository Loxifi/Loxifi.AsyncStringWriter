using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Loxifi
{
    /// <summary>
    /// An asyncronous string writer class. Accepts text into a queue, and uses a background thread to flush the text to a provided func for processing
    /// </summary>
    public class AsyncStringWriter : IDisposable
    {
        private readonly Action<string> _action;
        private readonly ConcurrentQueue<string> _queue = new();
        private readonly BackgroundWorker _worker = new();
        private readonly AutoResetEvent _queueGate = new(false);
        private readonly AutoResetEvent _disposeGate = new(false);
        internal readonly ManualResetEvent FlushGate = new(true);

        private bool _disposedValue;

        /// <summary>
        /// Creates a new instance of the string writer class
        /// </summary>
        /// <param name="action">A function that should be the target of the processing background thread.</param>
        public AsyncStringWriter(Action<string> action)
        {
            _action = action;

            _worker.DoWork += (se, e) => LoopProcess();

            _worker.RunWorkerAsync();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds a new line of text to the internal queue, to be flushed by the background thread
        /// </summary>
        /// <param name="toEnque">The string to Enqueue</param>
        public void Enqueue(string toEnque)
        {
            //Add the line to print
            _queue.Enqueue(toEnque);

            _ = _queueGate.Set();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;

                _ = _queueGate.Set();

                _ = _disposeGate.WaitOne();
            }
            else
            {
                Debug.WriteLine($"Attempting to dispose of already disposed {typeof(AsyncStringWriter)}");
            }
        }

        private void LoopProcess()
        {
            StringBuilder toLog = new();

            void Flush()
            {
                _action(toLog.ToString());

                _ = toLog.Clear();
            }

            while (_queueGate.WaitOne() && !_disposedValue)
            {
                _ = FlushGate.Reset();

                bool flush = false;

                while (_queue.TryDequeue(out string line))
                {
                    flush = true;

                    if (toLog.MaxCapacity <= toLog.Length + line.Length + Environment.NewLine.Length)
                    {
                        Flush();
                    }
                    else if (toLog.Length > 0)
                    {
                        _ = toLog.Append(Environment.NewLine);
                    }

                    _ = toLog.Append(line);
                }

                if (flush)
                {
                    Flush();
                }

                _ = FlushGate.Set();
            }

            _ = FlushGate.Set();
            _ = _disposeGate.Set();
        }
    }
}
