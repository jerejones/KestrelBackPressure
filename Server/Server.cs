using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Server
{
    internal class Server
    {
        private readonly TransmitLoop _txLoop;

        private readonly int _txRate;

        public Server()
        {
            _txRate = 100000;
            _txLoop = new TransmitLoop(TimeSpan.FromSeconds(1));
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime lifetime)
        {
            app.UseRouter(r => { r.MapGet("/", HandleRequest); });
            lifetime.ApplicationStopped.Register(OnShutdown);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public static IWebHost Create()
        {
            return new WebHostBuilder().UseKestrel(options =>
                                        {
                                            options.Limits.MaxResponseBufferSize = 0;
                                            options.Listen(IPAddress.Any, 8080);
                                        })
                                       .UseStartup<Server>()
                                       .Build();
        }

        private async Task HandleRequest(HttpContext context)
        {
            context.Response.StatusCode  = StatusCodes.Status200OK;
            context.Response.ContentType = "application/octet-stream";

            var client = new Client(context, _txRate);

            _txLoop.AddClient(client);
            await client.Completed;
            _txLoop.RemoveClient(client);
        }

        private void OnShutdown()
        {
            _txLoop.Stop();
        }
    }
}
