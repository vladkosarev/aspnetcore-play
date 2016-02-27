using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;

namespace aspnetcoreplay
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseIISPlatformHandler();

            var _sockets = new ConcurrentDictionary<string, WebSocket>();

            app.UseWebSockets();
            app.Use(async (http, next) =>
            {
                if (http.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await http.WebSockets.AcceptWebSocketAsync();

                    _sockets.TryAdd(webSocket.GetHashCode().ToString(), webSocket);
                    if (webSocket != null && webSocket.State == WebSocketState.Open)
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var token = CancellationToken.None;
                            var buffer = new ArraySegment<Byte>(new Byte[4096]);

                            // Below will wait for a request message.
                            var received = await webSocket.ReceiveAsync(buffer, token);

                            switch (received.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    var request = Encoding.UTF8.GetString(buffer.Array,
                                                                          buffer.Offset,
                                                                          buffer.Count);
                                    // Handle request here.
                                    var type = WebSocketMessageType.Text;
                                    var data = Encoding.UTF8.GetBytes(request + " Response");
                                    var sendBuffer = new ArraySegment<Byte>(data);
                                    await webSocket.SendAsync(sendBuffer, type, true, token);

                                    break;
                            }
                        }
                        WebSocket _;
                        _sockets.TryRemove(webSocket.GetHashCode().ToString(), out _);
                    }
                }
                else
                {
                    // Nothing to do here, pass downstream.  
                    await next();
                }
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
        

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
