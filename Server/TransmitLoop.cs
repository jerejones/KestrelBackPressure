using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class TransmitLoop
    {
        private readonly Dictionary<string, Client> _clients;
        private readonly TimeSpan                   _period;
        private readonly Thread                     _thread;
        private          DateTime                   _nextTransmit;
        private          bool                       _stop;

        public TransmitLoop(TimeSpan period)
        {
            _period  = period;
            _clients = new Dictionary<string, Client>();

            _thread = new Thread(ThreadLoop);
            _thread.Start();
        }

        public void AddClient(Client client)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Adding client {client.Id}");
            lock (_clients)
            {
                _clients[client.Id] = client;
            }
        }

        public void RemoveClient(Client client)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Removing client {client.Id}");
            lock (_clients)
            {
                _clients.Remove(client.Id);
            }
        }

        public void Stop()
        {
            List<Client> clients;
            lock (_clients)
            {
                clients = _clients.Select(x => x.Value).ToList();
            }

            foreach (var client in clients)
            {
                client.Disconnect();
            }

            _stop = true;
        }

        private void ThreadLoop()
        {
            _nextTransmit = DateTime.Now;
            try
            {
                while (!_stop)
                {
                    var waitTimeout = ThreadProc();
                    if (waitTimeout > TimeSpan.Zero)
                    {
                        Thread.Sleep(waitTimeout);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} The transmit loop is ending because {0}", e.Message);
            }
            finally
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} The transmit loop is ending!");
            }
        }

        private TimeSpan ThreadProc()
        {
            List<Client> clients;
            lock (_clients)
            {
                clients = _clients.Select(x => x.Value).ToList();
            }

            foreach (var client in clients)
            {
                if (client.Finished)
                {
                    RemoveClient(client);
                    continue;
                }

                if (!client.IsTransmitting)
                {
                    Task.Run(client.Transmit);
                }
            }

            _nextTransmit = _nextTransmit + _period;
            return _nextTransmit - DateTime.Now;
            ;
        }
    }
}
