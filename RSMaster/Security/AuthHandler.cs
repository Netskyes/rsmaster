using System;
using System.Text;
using System.Threading.Tasks;

namespace RSMaster.Security
{
    using UI;
    using Utility;
    using Network;
    using Cryptography;

    internal class AuthHandler
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string LicenseKey { get; set; }

        public ConnectErrorCallback OnConnectError;
        public delegate void ConnectErrorCallback();

        public StatusCodeCallback OnStatusCode;
        public delegate void StatusCodeCallback(byte code);
        
        private readonly NetworkManager NetworkManager = MainWindow.NetworkManager;

        public AuthHandler()
        {
            NetworkManager.Client.ClientRecv += Client_ClientRecv;
        }

        public void SetAuth(string username, string password, string licenseKey)
        {
            Username = username;
            Password = password;
            LicenseKey = licenseKey;
        }

        private async Task TrySend(byte[] bytes)
        {
            var connected = NetworkManager.Connected();
            if (!connected)
            {
                connected = await NetworkManager.Connect(2000);
            }

            if (connected)
            {
                NetworkManager.Client.Send(bytes);
            }
            else
            {
                OnConnectError();
            }
        }

        private byte[] GetRegisterBytes()
        {
            var userBytes = AES.Encrypt(Encoding.UTF8.GetBytes(Username));
            var passBytes = AES.Encrypt(Encoding.UTF8.GetBytes(Password));

            int writeOffset = 3;
            int arrayLen = (userBytes.Length + 1) + (passBytes.Length + 1);

            byte[] array = new byte[3 + arrayLen];
            Array.Copy(BitConverter.GetBytes(array.Length - 2), 0, array, 0, 2);
            array[2] = 2; // Opcode

            array[writeOffset] = (byte)userBytes.Length;
            Array.Copy(userBytes, 0, array, writeOffset + 1, userBytes.Length);
            writeOffset += (userBytes.Length + 1);

            array[writeOffset] = (byte)passBytes.Length;
            Array.Copy(passBytes, 0, array, writeOffset + 1, passBytes.Length);
            writeOffset += (passBytes.Length + 1);

            return array;
        }

        private byte[] GetAuthenticationBytes()
        {
            var userBytes = AES.Encrypt(Encoding.UTF8.GetBytes(Username));
            var passBytes = AES.Encrypt(Encoding.UTF8.GetBytes(Password));
            var licenseBytes = AES.Encrypt(Encoding.UTF8.GetBytes(LicenseKey));
            var hwidBytes = AES.Encrypt(Encoding.UTF8.GetBytes(Util.GetHWID()));

            int writeOffset = 3;
            int arrayLen = (userBytes.Length + 1) 
                + (passBytes.Length + 1) + (licenseBytes.Length + 1) + (hwidBytes.Length + 1);

            byte[] array = new byte[3 + arrayLen];
            Array.Copy(BitConverter.GetBytes(array.Length - 2), 0, array, 0, 2);
            array[2] = 1; // Opcode

            array[writeOffset] = (byte)userBytes.Length;
            Array.Copy(userBytes, 0, array, writeOffset + 1, userBytes.Length);
            writeOffset += (userBytes.Length + 1);

            array[writeOffset] = (byte)passBytes.Length;
            Array.Copy(passBytes, 0, array, writeOffset + 1, passBytes.Length);
            writeOffset += (passBytes.Length + 1);

            array[writeOffset] = (byte)licenseBytes.Length;
            Array.Copy(licenseBytes, 0, array, writeOffset + 1, licenseBytes.Length);
            writeOffset += (licenseBytes.Length + 1);

            array[writeOffset] = (byte)hwidBytes.Length;
            Array.Copy(hwidBytes, 0, array, writeOffset + 1, hwidBytes.Length);

            return array;
        }

        public async Task RequestAuth()
        {
            await TrySend(GetAuthenticationBytes());
        }

        public async Task RequestRegister()
        {
            await TrySend(GetRegisterBytes());
        }

        private void Client_ClientRecv(ClientBase client, Packet packet)
        {
            if (packet is null 
                || packet.Buffer is null 
                || packet.Buffer.Length < 1)
                return;

            var code = packet.Buffer[0];
            switch (code)
            {
                case 1:
                    NetworkManager.StartHeartBeat();
                    NetworkManager.Client.ClientRecv -= Client_ClientRecv;
                    AeonGuard.Begin();
                    break;
            }

            OnStatusCode?.Invoke(code);
        }
    }
}
