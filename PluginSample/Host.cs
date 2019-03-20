using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RSMaster.Api;

namespace PluginSample
{
    public class Host : Core
    {
        public void PluginRun()
        {
            Log("Plugin: Bridge Controller Loaded");
            OnPacketReceive += PacketReceive;
        }

        private void PacketReceive(byte[] buffer)
        {
            if (buffer.Length < 3)
                return;

            var args = Encoding.UTF8.GetString(buffer)?.Split(':');
            if (args is null)
                return;

            var email = args[2];
            var username = args[1];

            byte.TryParse(args[0], out byte opcode);
            switch(opcode)
            {
                case OpCodes.Heartbeat:
                    // We're alive, maybe register something.
                    break;

                case OpCodes.LoginStatus:
                    if (args[3] == "4")
                    {
                        var account = GetAccountByUsername(username);
                        if (account != null)
                        {
                            Log("Banned: " + account.Username);
                        }
                    }
                    break;

                case OpCodes.GeneralStatus:
                    if (args[3] == "10")
                    {
                        var account = GetAccountByUsername(username);
                        if (account != null)
                        {
                            Task.Run(() =>
                            {
                                StopAccountById(account.Id);
                                Thread.Sleep(2000);

                                account.Script = "ExplvAIO:default.config";
                                account.GroupId = 77;
                                UpdateAccount(account);
                                LaunchAccountById(account.Id);
                            });
                        }
                    }
                    break;
            }
        }

        public void PluginStop()
        {
            OnPacketReceive -= PacketReceive;
        }
    }
}
