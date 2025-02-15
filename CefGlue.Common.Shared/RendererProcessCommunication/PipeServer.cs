using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Xilium.CefGlue.Common.Shared.RendererProcessCommunication
{
    internal class PipeServer : IDisposable
    {
        private const int MaxErrorsAllowed = 5;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public event Action<string> MessageReceived;

        public PipeServer(string pipeName)
        {
            Task.Run(async () =>
            {
                var errorCount = 0;
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        using (var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                        {
                            await serverPipe.WaitForConnectionAsync(_cancellationTokenSource.Token);
                            HandleClientConnected(serverPipe);
                        }
                    }
                    catch
                    {
                        errorCount++;
                        if (errorCount > MaxErrorsAllowed)
                        {
                            break;
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        private void HandleClientConnected(Stream pipe)
        {
            var messageReceivedHandler = MessageReceived;
            if (messageReceivedHandler == null)
            {
                return;
            }

            var stream = new PipeStream(pipe);
            var message = stream.ReadString();
            messageReceivedHandler(message);
        }
    }
}
