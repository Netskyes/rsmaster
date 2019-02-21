using System;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace RSMaster.Network
{
    using Enums;

    public abstract class ClientBase
    {
        public Socket Socket { get => handle; }
        public IPEndPoint EndPoint { get; private set; }
        public bool Connected { get; private set; }
        public DateTime ConnectedTime { get; private set; }

        public int BUFFER_SIZE { get; set; }
        public int HEADER_SIZE { get; set; }

        #region Fields

        private Socket handle;

        private bool appendHeader;

        private int dataLen;
        private int readOffset;
        private int writeOffset;
        private int tempHeaderOffset;
        private int payloadLen;

        private byte[] tempHeader;
        private byte[] payloadBuffer;
        private byte[] readBuffer;

        private ReceiveType receiveType = ReceiveType.Header;

        #endregion

        #region Buffers

        private Queue<Packet> sendBuffers = new Queue<Packet>();
        private readonly object sendingPacketsLock = new object();
        private bool sendingPackets;

        private Queue<Packet> readBuffers = new Queue<Packet>();
        private readonly object readingPacketsLock = new object();
        private bool readingPackets;

        #endregion

        #region Event Handlers

        public event ClientStateEventHandler ClientState;
        public delegate void ClientStateEventHandler(ClientBase client, bool isConnected);

        protected virtual void OnClientState(bool isConnected)
        {
            if (Connected == isConnected)
                return;

            Connected = isConnected;
            ClientState?.Invoke(this, isConnected);
        }

        public event ClientRecvEventHandler ClientRecv;
        public delegate void ClientRecvEventHandler(ClientBase client, Packet packet);

        protected virtual void OnClientRecv(Packet packet, ClientBase client = null)
        {
            if (packet is null)
                return;

            ClientRecv?.Invoke(client ?? (this), packet);
        }

        public event ClientSendEventHandler ClientSend;
        public delegate void ClientSendEventHandler(ClientBase client, Packet packet);

        protected virtual void OnClientSend(Packet packet, ClientBase client = null)
        {
            if (packet is null)
                return;

            ClientSend?.Invoke(client ?? (this), packet);
        }

        #endregion


        public ClientBase()
        {
        }

        public void Connect(IPEndPoint endPoint)
            => Connect(endPoint.Address.ToString(), (ushort)endPoint.Port);

        public void Connect(string host, ushort port)
        {
            try
            {
                if (handle != null)
                    DisposeHandle();

                handle = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                handle.Connect(host, port);

                if (handle.Connected)
                {
                    ConnectedTime = DateTime.Now;
                    EndPoint = (IPEndPoint)handle.RemoteEndPoint;

                    OnClientState(true);

                    tempHeader = new byte[HEADER_SIZE];
                    readBuffer = new byte[BUFFER_SIZE];

                    handle.BeginReceive(readBuffer, 0, readBuffer.Length, SocketFlags.None, AsyncReceive, null);
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Send(byte[] bytes)
            => Send(new Packet { Buffer = bytes });

        public void Send(byte[] bytes, IPEndPoint endPoint)
            => Send(new Packet { Buffer = bytes, EndPoint = endPoint, UdpPacket = true });

        public void Send(Packet packet)
        {
            if (!Connected || packet == null)
                return;


            OnClientSend(packet);

            lock (sendBuffers)
            {
                sendBuffers.Enqueue(packet);

                lock (sendingPacketsLock)
                {
                    if (sendingPackets)
                        return;

                    sendingPackets = true;
                }

                ThreadPool.QueueUserWorkItem(SendProcess);
            }
        }

        public void Disconnect()
        {
            if (handle != null) DisposeHandle();
            
            OnClientState(false);
        }


        private void AsyncUdpReceive(IAsyncResult result)
        {
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            int bytesTransfered;

            try
            {
                bytesTransfered = handle.EndReceiveFrom(result, ref remote);

                if (bytesTransfered <= 0)
                {
                    throw new Exception("No bytes transfered!");
                }
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }


            byte[] received = new byte[bytesTransfered];

            try
            {
                Array.Copy(readBuffer, received, received.Length);
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }

            var packet = new Packet { Buffer = received, EndPoint = (IPEndPoint)remote, UdpPacket = true };

            lock (readBuffers)
            {
                readBuffers.Enqueue(packet);
            }

            lock (readingPacketsLock)
            {
                if (!readingPackets)
                {
                    readingPackets = true;
                    ThreadPool.QueueUserWorkItem(AsyncRecvProcess);
                }
            }


            try
            {
                handle.BeginReceiveFrom(readBuffer, 0, readBuffer.Length, SocketFlags.None, ref remote, AsyncUdpReceive, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void AsyncReceive(IAsyncResult result)
        {
            int bytesTransfered;

            try
            {
                bytesTransfered = handle.EndReceive(result);

                if (bytesTransfered <= 0)
                {
                    throw new Exception("No bytes transfered!");
                }
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }


            byte[] received = new byte[bytesTransfered];

            try
            {
                Array.Copy(readBuffer, received, received.Length);
            }
            catch (Exception)
            {
                Disconnect();
                return;
            }

            var packet = new Packet { Buffer = received };

            lock (readBuffers)
            {
                readBuffers.Enqueue(packet);
            }

            lock (readingPacketsLock)
            {
                if (!readingPackets)
                {
                    readingPackets = true;
                    ThreadPool.QueueUserWorkItem(AsyncRecvProcess);
                }
            }


            try
            {
                handle.BeginReceive(readBuffer, 0, readBuffer.Length, SocketFlags.None, AsyncReceive, null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void AsyncRecvProcess(object state)
        {
            while (true)
            {
                Packet packet;

                lock (readBuffers)
                {
                    if (readBuffers.Count == 0)
                    {
                        lock (readingPacketsLock)
                        {
                            readingPackets = false;
                        }

                        return;
                    }

                    packet = readBuffers.Dequeue();
                }

                var buffer = packet.Buffer;
                var bufferLen = buffer.Length;
                dataLen += bufferLen;

                while (true)
                {
                    if (receiveType == ReceiveType.Header)
                    {
                        if (dataLen < HEADER_SIZE)
                        {
                            try
                            {
                                Array.Copy(readBuffer, 0, tempHeader, tempHeaderOffset, bufferLen);
                            }
                            catch (Exception)
                            {
                                Disconnect();
                                break;
                            }

                            tempHeaderOffset += bufferLen;
                            appendHeader = true;

                            break;
                        }
                        else
                        {
                            int headerLen = (appendHeader)
                                ? (HEADER_SIZE - tempHeaderOffset) : HEADER_SIZE;

                            try
                            {
                                if (appendHeader)
                                {
                                    Array.Copy(readBuffer, 0, tempHeader, tempHeaderOffset, headerLen);

                                    payloadLen = BitConverter.ToInt16(tempHeader, 0);
                                    tempHeaderOffset = 0;
                                    appendHeader = false;
                                }
                                else
                                {
                                    payloadLen = BitConverter.ToInt16(readBuffer, 0);
                                }

                                readOffset += headerLen;
                                dataLen -= HEADER_SIZE;
                                receiveType = ReceiveType.Payload;
                            }
                            catch (Exception)
                            {
                                Disconnect();
                                break;
                            }
                        }
                    }
                    else if (receiveType == ReceiveType.Payload)
                    {
                        if (payloadBuffer is null || payloadBuffer.Length != payloadLen)
                        {
                            payloadBuffer = new byte[payloadLen];
                        }

                        int length = ((writeOffset + dataLen) >= payloadLen)
                            ? payloadLen - writeOffset : dataLen;

                        try
                        {
                            Array.Copy(readBuffer, readOffset, payloadBuffer, writeOffset, length);
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        writeOffset += length;
                        readOffset += length;
                        dataLen -= length;

                        if (writeOffset == payloadLen)
                        {
                            if (payloadBuffer.Length > 0)
                            {
                                var payload = new Packet { Buffer = payloadBuffer };

                                OnClientRecv(payload, this);
                                AsyncRecvProcess(payload);
                            }

                            receiveType = ReceiveType.Header;
                            payloadBuffer = null;
                            payloadLen = 0;
                            writeOffset = 0;

                            break;
                        }

                        if (dataLen == 0)
                            break;
                    }
                }

                if (receiveType == ReceiveType.Header)
                {
                    writeOffset = 0;
                }

                dataLen = 0;
                readOffset = 0;
            }
        }

        protected virtual void AsyncRecvProcess(Packet packet)
        {
        }

        private void SendProcess(object state)
        {
            while (true)
            {
                if (!Connected)
                {
                    SendCleanup(false);
                    return;
                }

                Packet packet;

                lock (sendBuffers)
                {
                    if (sendBuffers.Count == 0)
                    {
                        SendCleanup(false);
                        return;
                    }

                    packet = sendBuffers.Dequeue();
                }

                if (packet == null)
                    continue;

                try
                {
                    SendProcess(packet);
                }
                catch (Exception)
                {
                    Disconnect();
                    SendCleanup(true);
                    return;
                }
            }
        }

        protected virtual void SendProcess(Packet packet)
        {
            if (!packet.UdpPacket)
            {
                handle.Send(packet.Buffer);
            }
            else
            {
                handle.SendTo(packet.Buffer, packet.EndPoint);
            }
        }

        private void SendCleanup(bool clear)
        {
            lock (sendingPacketsLock)
            {
                sendingPackets = false;
            }

            if (clear)
            {
                lock (sendBuffers)
                {
                    sendBuffers.Clear();
                }
            }
        }

        private void DisposeHandle()
        {
            handle.Close();
            handle = null;
        }
    }
}
