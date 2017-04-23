using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AndroidHouseMaster
{
    public class AsyncTcpClient : IDisposable
    {
        private TcpClient tcpClient;
        private Stream stream;

        private bool disposed = false;

        public bool IsReceiving
        { get; set; }

        public bool IsConnected
        {
            get
            {
                return tcpClient != null && tcpClient.Connected;
            }
        }

        public event EventHandler<byte[]> OnDataReceived;
        public event EventHandler OnDisconnected;

        public void Connect(string host, int port, bool ssl = false, CancellationToken token = default(CancellationToken))
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(host, port);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task ReceiveAsync(CancellationToken token = default(CancellationToken))
        {
            IsReceiving = true;

            byte[] dataSize = new byte[4];
            while (this.IsConnected)
            {
                token.ThrowIfCancellationRequested();
                stream = tcpClient.GetStream();
                int k = await stream.ReadAsync(dataSize, 0, sizeof(uint), token);  // 3rd entry in dataSize hold how many bytes to read

                byte[] data = new byte[dataSize[3]];
                int bytesRead = await stream.ReadAsync(data, 0, dataSize[3], token);

                var onDataReceived = this.OnDataReceived;
                if (OnDataReceived != null)
                {
                    onDataReceived(this, data);
                }
            }
        }

        public async Task SendAsync(byte[] data, CancellationToken token = default(CancellationToken))
        {
            try
            {
                stream = tcpClient.GetStream();
                await this.stream.WriteAsync(data, 0, data.Length, token);
                await this.stream.FlushAsync(token);
            }
            catch (IOException ex)
            {
                var onDisconnected = this.OnDisconnected;
                if (ex.InnerException != null && ex.InnerException is ObjectDisposedException)
                {
                    //Console.WriteLine("innocous SSL stream error);
                }
                else if (onDisconnected != null)
                {
                    onDisconnected(this, EventArgs.Empty);
                }
            }
        }

        #region closing and cleanup

        public async Task CloseAsync()
        {
            await Task.Yield();
            //Close();
            CloseMe();
        }

        private void CloseMe()
        {
            if (tcpClient != null)
            {
                tcpClient = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }

            GC.SuppressFinalize(this);
        }

        private void Close()
        {
            if (tcpClient != null)
            {
                Dispose();
                tcpClient = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        private async Task CloseIfCanceled(CancellationToken token, Action onClosed = null)
        {
            if (token.IsCancellationRequested)
            {
                await CloseAsync();
                if (onClosed != null)
                {
                    onClosed();
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    this.Close();
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}