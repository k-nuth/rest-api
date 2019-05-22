using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace bitprim.insight.Websockets
{
    internal sealed class WebSocketForwarderClient : IDisposable
    {
        private readonly IOptions<NodeConfig> config_;
        private readonly ILogger<WebSocketForwarderClient> logger_;
        private readonly WebSocketHandler webSocketHandler_;
        private const string SUBSCRIPTION_MESSAGE_BLOCKS = "SubscribeToBlocks";
        private const string SUBSCRIPTION_MESSAGE_TXS = "SubscribeToTxs";
        private const int RECEPTION_BUFFER_SIZE = 1024 * 4;

        private ClientWebSocket webSocket_;

        private readonly Policy breakerPolicy_;
        private readonly Policy retryPolicy_;
        private readonly Policy execPolicy_;

        private int active_ = 1;

        public WebSocketForwarderClient(IOptions<NodeConfig> config, ILogger<WebSocketForwarderClient> logger, WebSocketHandler webSocketHandler)
        {
            config_ = config;
            logger_ = logger;
            webSocketHandler_ = webSocketHandler;

            breakerPolicy_ = Policy.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromSeconds(config_.Value.WebsocketsForwarderClientRetryDelay));

            retryPolicy_ = Policy.Handle<Exception>()
                                    .WaitAndRetryForeverAsync(
                                                                retryAttempt =>
                                                                {
                                                                    logger_.LogWarning("Retry attempt " + retryAttempt);
                                                                    return TimeSpan.FromSeconds(config_.Value.WebsocketsForwarderClientRetryDelay);
                                                                });

            execPolicy_ = Policy.WrapAsync(retryPolicy_,breakerPolicy_);
        }

        private async Task ReceiveHandler()
        {
            logger_.LogInformation("Initializing websocket receiver hander");

            var buffer = new byte[RECEPTION_BUFFER_SIZE];
          
            while (Interlocked.CompareExchange(ref active_, 0, 0) > 0)
            {
                try
                {
                    var result = await webSocket_.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (Interlocked.CompareExchange(ref active_, 0, 0) > 0)
                        {
                            await ReInit();
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var content = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        logger_.LogInformation("Message received " + content);
                        
                        var obj = JObject.Parse(content);

                        switch (obj["eventname"].ToString())
                        {
                            case "block":
                                await webSocketHandler_.PublishBlock(content);
                                break;
                            case "tx":
                                await webSocketHandler_.PublishTransaction(content);

                                var txid = obj["txid"].ToString();
                                var addresses = ((JArray)obj["addresses"]).ToObject<List<string>>();
                                var balanceDeltas = obj["balanceDeltas"].ToObject<Dictionary<string, Int64>>();

                                var addressesToPublish = new List<Tuple<string, string>>(addresses.Count);
                                foreach(string addr in addresses)
                                {
                                    var addresstx = new
                                    {
                                        eventname = "addresstx",
                                        txid = txid,
                                        balanceDelta = balanceDeltas[addr]
                                    };
                                    addressesToPublish.Add(new Tuple<string, string>(addr, JsonConvert.SerializeObject(addresstx)));
                                }

                                await webSocketHandler_.PublishTransactionAddresses(addressesToPublish);
                                break;
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    logger_.LogDebug("Status " + webSocket_.State);
                    logger_.LogDebug("Close Status " + webSocket_.CloseStatus);
                    logger_.LogDebug("WebSocketErrorCode " + ex.WebSocketErrorCode);
                    
                    if (Interlocked.CompareExchange(ref active_, 0, 0) > 0)
                    {
                        if (webSocket_.State != WebSocketState.CloseSent &&
                            ex.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                        {
                            logger_.LogWarning(ex,"Error processing ReceiveHandler");
                        }
                        await ReInit();
                    }     
                }
                catch (Exception e)
                {
                    if (Interlocked.CompareExchange(ref active_, 0, 0) > 0)
                    {
                        //Internal WinHttpException not exposed...
                        if (e.HResult != Constants.WIN_HTTP_EXCEPTION_ERR_NUMBER)
                        {
                            logger_.LogWarning(e,"Error processing ReceiveHandler");
                        }

                        await ReInit();
                    }   
                }
            }
        }

        private async Task CreateAndOpen()
        {
            logger_.LogInformation("Initializing connection to websocket");
            Dispose();
            webSocket_ = new ClientWebSocket();
            await webSocket_.ConnectAsync(
                new Uri(config_.Value.ForwardUrl.Replace("http://", "ws://")), CancellationToken.None);
            logger_.LogInformation("Connection to websocket established");
        }

        private async Task SendSubscriptions()
        {
            logger_.LogInformation("Sending Block subscription");

            await webSocket_.SendAsync
            (
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(SUBSCRIPTION_MESSAGE_BLOCKS), 0, SUBSCRIPTION_MESSAGE_BLOCKS.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            logger_.LogInformation("Sending Tx subscription");

            await webSocket_.SendAsync
            (
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(SUBSCRIPTION_MESSAGE_TXS), 0, SUBSCRIPTION_MESSAGE_TXS.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        private async Task ReInit()
        {
            logger_.LogInformation("ReInit websocket forwarder client");
            await execPolicy_.ExecuteAsync(async ()=> await CreateAndOpen());
            await SendSubscriptions();
        }

        public async Task Init()
        {
            logger_.LogInformation("Init websocket forwarder client");
            await execPolicy_.ExecuteAsync(async ()=> await CreateAndOpen());
            _ = ReceiveHandler();
            await SendSubscriptions();
        }

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                     ws.State == WebSocketState.Closed ||
                     ws.State == WebSocketState.CloseSent);
        }

        public async Task Close()
        {
            logger_.LogWarning("Closing websocket client");
            Interlocked.Decrement(ref active_);
            try
            {
                if (WebSocketCanSend(webSocket_))
                {
                    await webSocket_.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                logger_.LogInformation(e,"Error closing websocket connection");
            }
        }

        public void Dispose()
        {
            webSocket_?.Dispose();
        }
    }
}