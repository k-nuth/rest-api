using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using bitprim.insight.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace bitprim.insight.Middlewares
{
    internal class ForwarderMiddleware
    {
        private readonly RequestDelegate next_;
        private readonly ILogger<ForwarderMiddleware> logger_;
        private static readonly HttpClient client = new HttpClient();
        private readonly Policy retryPolicy_;
        private readonly NodeConfig nodeConfig_;

        public ForwarderMiddleware(RequestDelegate next, ILogger<ForwarderMiddleware> logger, IOptions<NodeConfig> config)
        {
            next_ = next ?? throw new ArgumentNullException(nameof(next));
            logger_ = logger;
            nodeConfig_ = config.Value;
            client.BaseAddress = new Uri(config.Value.ForwardUrl);
            client.Timeout = TimeSpan.FromSeconds(nodeConfig_.HttpClientTimeoutInSeconds);
            retryPolicy_ = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(RetryUtils.DecorrelatedJitter
                (
                    nodeConfig_.ForwarderMaxRetries,
                    TimeSpan.FromMilliseconds(nodeConfig_.ForwarderFirstRetryDelayInMillis),
                    TimeSpan.FromSeconds(nodeConfig_.ForwarderMaxRetryDelayInSeconds)
                ));
        }

        public async Task Invoke(HttpContext context)
        {
            logger_.LogInformation("Invoking request " + context.Request.Path);
            
            var method = new HttpMethod(context.Request.Method);

            StringContent httpContent;
            using (var sr = new StreamReader(context.Request.Body))
            {
                var content = await sr.ReadToEndAsync();
                httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            }

            var ret = await retryPolicy_.ExecuteAsync(() =>
            {
                var message = new HttpRequestMessage(method,(context.Request.Path.Value ?? "") + (context.Request.QueryString.Value ?? ""))
                {
                    Content = httpContent
                };

                return client.SendAsync(message);
            });

            context.Response.StatusCode = (int)ret.StatusCode;
            context.Response.ContentType = ret.Content.Headers.ContentType?.ToString();
            await context.Response.WriteAsync(await ret.Content.ReadAsStringAsync());
        }

    }

    internal static class ForwarderMiddlewareExtensions
    {
        public static IApplicationBuilder UseForwarderMiddleware(this IApplicationBuilder builder)
        {
            builder.Map("/forwarderhealth", applicationBuilder =>
            {
                applicationBuilder.Run(async context =>
                {
                    await context.Response.WriteAsync("OK");
                });
            });


            builder.Map("/forwardermemstats", applicationBuilder =>
            {
                applicationBuilder.Run(async context =>
                {
                    var stats = new MemoryStatsDto();
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(stats));
                });
            });

            return builder.UseMiddleware<ForwarderMiddleware>();
        }
    }
}