using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MOGWAI_CLI
{
    internal class AsyncService
    {
        public delegate void MessageReceivedEventHandler(object sender, ServerMessage serverMessage);       
        
        public event MessageReceivedEventHandler? MessageReceived;

        public IPAddress? IpAddress { get; private set; }
        
        public int Port { get; private set; }

        private StreamReader? reader = null;
        private StreamWriter? writer = null;
        private bool requestStopServer = false;

        public AsyncService(int port)
        {
            Port = port;
        }

        public async Task RunServerAsync()
        {
            var fields = "127.0.0.1".Split('.');

            if (fields.Length == 4)
            {
                var bytes = new byte[4];

                bytes[0] = byte.Parse(fields[0]);
                bytes[1] = byte.Parse(fields[1]);
                bytes[2] = byte.Parse(fields[2]);
                bytes[3] = byte.Parse(fields[3]);

                IpAddress = new IPAddress(bytes);
            }
            else
            {
                return;
            }

            TcpListener listener = new TcpListener(IpAddress!, Port);
            listener.Start(1);
            Console.WriteLine("Waiting STUDIO...");
            Console.WriteLine(IpAddress.ToString());
            Console.WriteLine(Port.ToString());


            try
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                listener.Stop();
                Console.Clear();
                
                await processMessagesAsync(tcpClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }         
        }

        public void StopServer()
        {
            requestStopServer = true;
        }

        private void onMessageReceived(ServerMessage message)
        {
            MessageReceivedEventHandler? handler = MessageReceived;
            handler?.Invoke(this, message);
        }

        private async Task processMessagesAsync(TcpClient tcpClient)
        {
            try
            {
                NetworkStream networkStream = tcpClient!.GetStream();

                reader = new StreamReader(networkStream);

                writer = new StreamWriter(networkStream);
                writer.AutoFlush = true;

                while (!requestStopServer)
                {
                    string? request = await reader.ReadLineAsync();

                    if (request != null)
                    {
                        // Traduction du message json reçu en objet ServerMessage

                        try
                        {
                            var message = JsonConvert.DeserializeObject<ServerMessage>(request);

                            if (message != null)
                            {
                                // Déclenchement événement de message reçu

                                onMessageReceived(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("");
                        }
                    }
                    else
                    {
                        // Déconnexion

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
         
            }

            // On a terminé (on a demandé un arrêt du serveur ou une erreur s'est produite)

            tcpClient.Close();
        }

        public async Task SendToClientAsync(string function, params string[] parameters)
        {
            var message = new ServerMessage("MOGWAI RUNTIME", function, parameters);
            await SendToClientAsync(message);
        }

        private async Task<bool> SendToClientAsync(ServerMessage message)
        {
            var msg = JsonConvert.SerializeObject(message);
            return await SendToClientAsync(msg);
        }

        private async Task<bool> SendToClientAsync(string message)
        {
            if (writer != null)
            {
                try
                {
                    await writer.WriteLineAsync(message);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("");
                }
            }

            return false;
        }
    }
}
