using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UDPService
{
    /// <summary>
    /// Provides UDP communication services, allowing connection to a remote endpoint,
    /// sending and receiving data, and handling connection events.
    /// </summary>
    public class UDPService : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private bool _isConnected;
        private CancellationTokenSource _cts;
        private int _timeoutMilliseconds;
        private DateTime _lastReceivedTime = DateTime.UtcNow;
        private DateTime _lastInvokeTime = DateTime.UtcNow;
        private int _packetReceiveRate = 0;

        /// <summary>
        /// Occurs when a connection is successfully established.
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// Occurs when the connection is terminated.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Occurs when data is received from the remote endpoint.
        /// </summary>
        public event Action<byte[]> DataReceived;

        /// <summary>
        /// Occurs when data is sent to the remote endpoint.
        /// </summary>
        public event Action<byte[]> DataSended;

        /// <summary>
        /// Initializes a new instance of the <see cref="UDPService"/> class with specified timeout and packet receive rate.
        /// </summary>
        /// <param name="timeoutMilliseconds">The timeout duration in milliseconds.</param>
        /// <param name="packetReceiveRate">The rate at which packets are received, in milliseconds.</param>
        public UDPService(int timeoutMilliseconds = 500, int packetReceiveRate = 0)
        {
            _udpClient = new UdpClient();
            _timeoutMilliseconds = timeoutMilliseconds;
            _packetReceiveRate = packetReceiveRate;
        }

        /// <summary>
        /// Asynchronously connects to the specified IP address and port.
        /// </summary>
        /// <param name="ip">The IP address of the remote endpoint.</param>
        /// <param name="port">The port number of the remote endpoint.</param>
        public async Task ConnectAsync(string ip, int port)
        {
            if (_isConnected) return;

            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Connect(_remoteEndPoint);
            _isConnected = true;
            _lastReceivedTime = DateTime.UtcNow;

            Connected?.Invoke();

            _cts = new CancellationTokenSource();
            _ = StartListeningAsync(_cts.Token);
        }

        /// <summary>
        /// Disconnects from the remote endpoint and releases resources.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            _cts?.Cancel();
            _udpClient.Close();
            _udpClient.Dispose();

            Disconnected?.Invoke();
        }

        /// <summary>
        /// Asynchronously sends data to the connected remote endpoint.
        /// </summary>
        /// <param name="data">The data to send.</param>
        public async Task SendAsync(byte[] data)
        {
            if (!_isConnected) return;

            await _udpClient.SendAsync(data, data.Length);
            DataSended?.Invoke(data);
        }

        /// <summary>
        /// Starts listening for incoming data asynchronously.
        /// </summary>
        /// <param name="token">A cancellation token to observe while waiting for a task to complete.</param>
        private async Task StartListeningAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var receiveTask = _udpClient.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(_timeoutMilliseconds, token)) == receiveTask)
                    {
                        _lastReceivedTime = DateTime.UtcNow;
                        var elapsed = (DateTime.UtcNow - _lastInvokeTime).TotalMilliseconds;

                        if (elapsed > _packetReceiveRate)
                        {
                            DataReceived?.Invoke(receiveTask.Result.Buffer);
                            _lastInvokeTime = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        CheckServerStatus();
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Checks the server status and disconnects if a timeout occurs.
        /// </summary>
        private void CheckServerStatus()
        {
            if ((DateTime.UtcNow - _lastReceivedTime).TotalMilliseconds > _timeoutMilliseconds)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Sets the rate at which packets are received.
        /// </summary>
        /// <param name="rate">The new packet receive rate in milliseconds.</param>
        public void SetPacketReceiveRate(int rate)
        {
            _packetReceiveRate = rate;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="UDPService"/> class.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }
}
