using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Server
{
    internal class Client
    {
        private readonly byte[]                     _buf;
        private readonly int                        _bytesPerSec;
        private readonly TaskCompletionSource<bool> _completed;
        private readonly HttpContext                _ctx;
        private readonly Stopwatch                  _sw;
        private readonly Stopwatch                  _throttleSw;
        private          int                        _bytesTx;

        public Client(HttpContext ctx, int bytesPerSec)
        {
            Id = Guid.NewGuid().ToString("N");

            _buf         = new byte[64 * 1024];
            _bytesPerSec = bytesPerSec;
            _completed   = new TaskCompletionSource<bool>();
            _ctx         = ctx;
            _sw          = new Stopwatch();
            _throttleSw  = new Stopwatch();
            _throttleSw.Start();
        }

        public Task<bool> Completed => _completed.Task;

        public bool Finished => _completed.Task.IsCompleted || _ctx.RequestAborted.IsCancellationRequested;

        public string Id { get; }

        public bool IsTransmitting { get; set; }

        public void Disconnect()
        {
            _completed.SetResult(false);
        }

        public async Task Transmit()
        {
            _sw.Restart();
            int numBytesToSend = 0;

            try
            {
                double expectedBytesTx = _bytesPerSec * _throttleSw.ElapsedMilliseconds / 1000.0;
                numBytesToSend = (int) Math.Min(expectedBytesTx - _bytesTx, _buf.Length);

                if (numBytesToSend > 0)
                {
                    IsTransmitting = true;
                    await _ctx.Response.Body.WriteAsync(_buf, 0, numBytesToSend);
                    await _ctx.Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Exception transmitting: {ex.Message}. Disconnecting.");
                _completed.SetResult(true);
            }
            finally
            {
                _bytesTx       += numBytesToSend;
                IsTransmitting =  false;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Transmitted {_bytesTx} (Last transmit {numBytesToSend} bytes in {_sw.Elapsed}");
            }
        }
    }
}
