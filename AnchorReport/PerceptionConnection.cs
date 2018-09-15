using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Security.Credentials;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Web;

namespace AnchorReport
{
    class PerceptionConnection : IDisposable
    {
        private MessageWebSocket messageWebSocket;
        private bool isConnected;

        private string address;
        private string user;
        private string password;
        private Action<string> logger;
        private Action<AnchorCollection> anchorHandler;
        private CoreDispatcher callerDispatcher;
        DispatcherTimer heartbeatTimer;
        Task heartbeatTask;
        DateTime nextAnchorRequestTime;
        TimeSpan anchorRequestDelta = TimeSpan.FromSeconds(5.0);

        public PerceptionConnection(string address, string user, string password)
        {
            // Send back all notifications to this dispatcher
            this.callerDispatcher = Window.Current.Dispatcher;

            this.address = address;
            this.user = user;
            this.password = password;
        }

        /// <summary>
        /// Sets a function that receives all logging information
        /// </summary>
        public void SetLogger(Action<string> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Sets a function that receives the sets of received anchors
        /// </summary>
        public void SetAnchorHandler(Action<AnchorCollection> anchorHandler)
        {
            this.anchorHandler = anchorHandler;
        }

        /// <summary>
        /// Starts the connection to the HoloLens.  Doesn't retry if it fails.
        /// </summary>
        public async Task ConnectAsync(bool ignoreHttpsErrors)
        {
            if(this.messageWebSocket != null)
            {
                throw new InvalidOperationException("ConnectAsync: failed - already connected");
            }

            var uri = new Uri($"wss://{this.address}/ext/perception/client?clientmode=active");
            var origin = $"Http://{this.address}";

            this.messageWebSocket = new MessageWebSocket();
            if (ignoreHttpsErrors)
            {
                this.messageWebSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
                this.messageWebSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                this.messageWebSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            }
            this.messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
            this.messageWebSocket.Control.ServerCredential = new PasswordCredential()
            {
                UserName = this.user,
                Password = this.password
            };
            this.messageWebSocket.SetRequestHeader("Origin", origin);

            this.messageWebSocket.MessageReceived += this.WebSocket_MessageReceived;
            this.messageWebSocket.Closed += this.WebSocket_Closed;

            try
            {
                this.Log("Connecting");
                await this.messageWebSocket.ConnectAsync(uri);
                this.isConnected = true;
                this.Log("Connecteed");
            }
            catch (Exception e)
            {
                WebErrorStatus webErrorStatus = WebSocketError.GetStatus(e.GetBaseException().HResult);

                this.Log("Connection Failure:");
                this.Log($"    {e.Message}");
                this.Log($"    WebErrorStatus.{webErrorStatus}");
                this.ClearConnection();
                throw;
            }

            await this.HeartbeatAsync(); // do the first heartbeat without waiting

            this.nextAnchorRequestTime = DateTime.UtcNow;
            heartbeatTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(10.0) };
            heartbeatTimer.Tick += HeartbeatTimerTick;
            heartbeatTimer.Start();
        }

        private void HeartbeatTimerTick(object sender, object e)
        {
            if (this.heartbeatTask == null || this.heartbeatTask.IsCompleted)
            {
                this.heartbeatTask = Task.Run(() => this.HeartbeatAsync());
            }
        }

        private async Task HeartbeatAsync()
        {
            if (this.isConnected && DateTime.UtcNow > this.nextAnchorRequestTime)
            {
                await this.SendMessageUsingMessageWebSocketAsync("getspatialAnchors");
                this.nextAnchorRequestTime = DateTime.UtcNow + this.anchorRequestDelta;
            }
        }

        private async Task SendMessageUsingMessageWebSocketAsync(string message)
        {
            try
            {
                using (var dataWriter = new DataWriter(this.messageWebSocket.OutputStream))
                {
                    dataWriter.WriteString(message);
                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                }
                this.Log("Sent message: " + message);
            }
            catch(Exception e)
            {
                this.Log($"Send message failed {message}  {e}");
            }
        }

        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader dataReader = args.GetDataReader())
                {
                    dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    string message = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                    
                    if (message.StartsWith("{\"Anchors\"")) // Hack to just get the message we want
                    {
                        // this.Log("Message received from MessageWebSocket: " + message);

                        var anchors = new AnchorCollection(message);

                        if(this.anchorHandler != null)
                        {
                            var t = this.callerDispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () => { this.anchorHandler.Invoke(anchors); });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WebErrorStatus webErrorStatus = WebSocketError.GetStatus(e.GetBaseException().HResult);

                this.Log($"Failure:\r\n\t{e.Message}\r\n\tWebErrorStatus.{webErrorStatus}");
            }
        }

        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this.Log("WebSocket_Closed; Code: " + args.Code + ", Reason: \"" + args.Reason + "\"");
            this.isConnected = false;
        }

        public void Dispose()
        {
            if (this.heartbeatTimer != null)
            {
                this.heartbeatTimer.Stop();
            }
            if (this.heartbeatTask != null)
            {
                this.heartbeatTask.Wait();
            }

            this.ClearConnection();
        }

        private void ClearConnection()
        {
            if(this.messageWebSocket != null)
            {
                this.messageWebSocket.Dispose();
                this.messageWebSocket = null;
            }

            this.isConnected = false;
        }

        private void Log(string message)
        {
            if (this.logger != null)
            {
                var t = this.callerDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.logger.Invoke(message); });
            }
        }
    }
}
