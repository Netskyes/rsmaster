using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RSMaster.Security
{
    using UI;

    internal static class AeonGuard
    {
        private static bool isRunning;
        private static CancellationTokenSource cancellationTokenSource;
        private static CancellationToken cancellationToken;

        public static void Begin()
        {
            if (MainWindow.LoginDialog is null 
                || MainWindow.LoginDialog.Username is null
                || MainWindow.LoginDialog.Password is null)
            {
                throw new Exception();
            }

            if (isRunning)
                return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            Task.Factory.StartNew(() => Loop(),
                cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static void Loop()
        {
            while (IsTokenAlive())
            {
                if (!MainWindow.NetworkManager.IsHeartBeating())
                {
                    MainWindow.ShutdownHandler();
                    break;
                }

                if (MainWindow.LoginDialog is null
                    || MainWindow.LoginDialog.Username is null
                    || MainWindow.LoginDialog.Password is null)
                {
                    MainWindow.ShutdownHandler();
                    break;
                }

                Thread.Sleep(1000);
            }
        }

        private static bool IsTokenAlive()
        {
            return !cancellationToken.IsCancellationRequested;
        }
    }
}
