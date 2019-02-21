using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RSMaster.Services
{
    using Interfaces;

    internal abstract class ServiceBase : IService
    {
        public event ServiceErrorEventHandler ServiceError;
        public delegate void ServiceErrorEventHandler(string message);

        public string Name { get; set; }
        public string Description { get; set; }
        public string LastError { get; set; }
        public bool IsRunning { get; set; }

        private Task serviceTask;
        private readonly object runningTasksLock = new object();

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        
        private void Initialize()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        protected CancellationToken GetCancelToken()
            => cancellationToken;

        protected virtual bool ServiceStartup()
            => true;

        protected virtual void RunService()
        {
            // Do nothing by default
        }

        public async Task<bool> Start()
        {
            if (IsRunning)
                return false;

            Initialize();
            return await Task.Run(async () =>
            {
                if (!await Task.Run(() => ServiceStartup()))
                {
                    OnServiceError(LastError ?? "An error occured during startup.");
                    return false;
                }

                serviceTask = Task.Run(() => 
                    RunService(), cancellationToken);
                IsRunning = true;

                return true;
            });
        }

        public void Stop()
        {
            if (IsRunning)
            {
                cancellationTokenSource.Cancel();
                IsRunning = false;
            }
        }

        protected virtual void OnServiceError(string errorMessage)
        {
            LastError = errorMessage;
            ServiceError?.Invoke(errorMessage);
        }

        protected void IsTokenCancelThrow()
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        public bool IsTokenAlive()
        {
            return !cancellationToken.IsCancellationRequested;
        }
    }
}
