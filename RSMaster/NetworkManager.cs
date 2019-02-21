using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RSMaster
{
    using Network;

    internal class NetworkManager
    {
        public Client Client { get; set; }
        public DateTime LastHeartbeat { get; set; }

        private Task heartBeatTask;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        public NetworkManager()
        {
            Client = new Client();
            Client.ClientState += Client_ClientState;
            Client.ClientSend += Client_ClientSend;
            Client.ClientRecv += Client_ClientRecv;

            Connect();
        }

        public bool IsHeartBeating()
        {
            return heartBeatTask != null 
                && heartBeatTask.Status == TaskStatus.Running 
                && (DateTime.Now - LastHeartbeat).TotalMilliseconds < 25 * 1000;
        }

        public bool Connected()
        {
            return Client != null && Client.Connected;
        }

        public Task<bool> Connect(int timeout = 20*1000)
        {
            var beginTime = DateTime.Now;
            return Task.Run(() =>
            {
                while (!Client.Connected 
                    && (DateTime.Now - beginTime).TotalMilliseconds < timeout)
                {
                    Client.Connect("185.5.54.101", 8085); // 185.5.54.101
                    Thread.Sleep(1000);
                }

                return Connected();
            });
        }

        public void StartHeartBeat()
        {
            LastHeartbeat = DateTime.Now;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            heartBeatTask = Task.Factory.StartNew(() => LoopHeartBeat(), 
                cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void LoopHeartBeat()
        {
            while (IsTokenAlive())
            {
                if (Client.Connected)
                {
                    Client.Send(GetHeartbeatBytes());
                }
                else
                {
                    Connect();
                }

                Thread.Sleep(10 * 1000);
            }
        }

        private void Client_ClientRecv(ClientBase client, Packet packet)
        {
            if (packet is null 
                || packet.Buffer is null 
                || packet.Buffer.Length < 1)
                return;

            if (packet.Buffer[0] == 3)
            {
                LastHeartbeat = DateTime.Now;
            }
        }

        private void Client_ClientSend(ClientBase client, Packet packet)
        {
        }

        private void Client_ClientState(ClientBase client, bool isConnected)
        {
        }

        private byte[] GetHeartbeatBytes()
        {
            var array = new byte[3];
            array[2] = 3;
            Array.Copy(BitConverter.GetBytes(1), 0, array, 0, 2);

            return array;
        }

        private bool IsTokenAlive()
        {
            return !cancellationToken.IsCancellationRequested;
        }
    }
}
