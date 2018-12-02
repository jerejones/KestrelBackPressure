using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Client
    {
        private int                     _bytesPerSecond;
        private CancellationTokenSource _cts;
        private string                  _url;

        public Client(string url, int bytesPerSecond)
        {
            _cts            = new CancellationTokenSource();
            _url            = url;
            _bytesPerSecond = bytesPerSecond;
        }

        public async Task Download()
        {
            int _bytesRx = 0;
            var _sw = new Stopwatch();
            
            try
            {
                var buf    = new byte[256];
                var client = (HttpWebRequest) WebRequest.Create(_url);

                using (var resp = await client.GetResponseAsync())
                using (var stream = resp.GetResponseStream())
                {
                    _sw.Restart();
                    _bytesRx  = 0;
                    var throttleSw = new Stopwatch();
                    while (!_cts.IsCancellationRequested)
                    {
                        throttleSw.Restart();
                        int bytesRead = await stream.ReadAsync(buf, 0, buf.Length, _cts.Token);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("Zero bytes read. Must be disconnected.");
                            _cts.Cancel();
                        }
                        if (bytesRead > 0)
                        {
                            _bytesRx += bytesRead;
                        }

                        if (_bytesPerSecond <= 0)
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Downloaded {_bytesRx} bytes ({bytesRead} this cycle)");
                            continue;
                        }

                        double expectedMillisecondsUsed = bytesRead * 1000.0 / _bytesPerSecond;
                        double actualMillisecondsUsed   = throttleSw.ElapsedMilliseconds;
                        int    delayMilliseconds        = (int) (expectedMillisecondsUsed - actualMillisecondsUsed);
                        if (delayMilliseconds > 0)
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Downloaded {_bytesRx} bytes ({bytesRead} this cycle). Throttling for {delayMilliseconds}ms");
                            await Task.Delay(delayMilliseconds);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred after {_sw.Elapsed}: {ex.Message}");
            }

            Console.WriteLine($"{_bytesRx} bytes read in {_sw.Elapsed}");
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
