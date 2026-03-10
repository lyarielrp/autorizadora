using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using autorizadora_producer.services.interfaces;
using DotNetEnv;
using Newtonsoft.Json;

namespace autorizadora_producer.services
{
    public class SocketServer : ISocketServer, IDisposable
    {
        private Socket _listenerSocket;
        private Socket _clientSocket;
        private readonly int _port;
        private readonly string _ip;
        private bool _isRunning;
        private const int BUFFER_SIZE = 4096;

        private readonly IRabbitMQ_Producer _producer;
        private readonly ILogger<SocketServer> _logger;

        public SocketServer(int port, string ip, IRabbitMQ_Producer producer, ILogger<SocketServer> logger)
        {
            Console.WriteLine(port);
            _port = port;
            _ip = ip;
            _isRunning = true;
            _producer = producer;
            _logger = logger;
        }

        private void InitializeListener()
        {
            try
            {
                IPAddress hostIPAddress = IPAddress.Parse(_ip);
                var ipEndPoint = new IPEndPoint(hostIPAddress, _port);

                _listenerSocket = new Socket(AddressFamily.InterNetwork,
                                           SocketType.Stream,
                                           ProtocolType.Tcp);

                _listenerSocket.SetSocketOption(SocketOptionLevel.Socket,
                                              SocketOptionName.ReuseAddress,
                                              true);

                _listenerSocket.Bind(ipEndPoint);
                _listenerSocket.Listen(10);

                Console.WriteLine($"{DateTime.Now} Servidor listo y escuchando en {_ip}:{_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} Error inicializando listener: {ex.Message}");
                throw;
            }
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            InitializeListener();
            _isRunning = true;
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"{DateTime.Now} Esperando conexiones...");

                    // Aceptar nueva conexión
                    _clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);
                    Console.WriteLine($"{DateTime.Now} Cliente conectado: {_clientSocket.RemoteEndPoint}");


                new Thread(() =>{
                    Thread.CurrentThread.IsBackground = true;
                    List<string> queueNames = ["q.responses.authorice_messages.BB06", "q.responses.personalization_messages.BB06"];
                    _producer.Consume(queueNames, _clientSocket, async (string message, string queue) => {
                        Console.WriteLine("Mensaje de respuesta" + message);
                        
                        if(queue == "q.responses.personalization_messages.BB06"){
                            await SendAsync(message,cancellationToken);
                        }
                        else{
                            var iso8583Async = new Iso8583Async(message);
                            var iso = iso8583Async.GetIsoFromMessage();
                            await SendAsync(iso, cancellationToken);
                        }
                        });
                }).Start();

                    // Manejar la conexión del cliente
                    _ = Task.Run(() => HandleClientConnectionAsync(_clientSocket, cancellationToken), cancellationToken);



                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Operacion cancelada por solicitud");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogInformation("Socket disposed durante la operacion");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} Error aceptando conexión: {ex.Message}");
                    await Task.Delay(1000); // Esperar antes de reintentar
                }
            }
        }

        private async Task HandleClientConnectionAsync(Socket clientSocket, CancellationToken cancellationToken)
        {
            string clientInfo = clientSocket.RemoteEndPoint?.ToString() ?? "Cliente desconocido";
            byte[] buffer = new byte[BUFFER_SIZE];

            try
            {
                while (_isRunning && clientSocket.Connected && !cancellationToken.IsCancellationRequested)
                {

                    // Recibir datos
                    var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);


                    if (bytesReceived == 0)
                    {
                        // Conexión cerrada gracefulmente por el cliente
                        Console.WriteLine($"{DateTime.Now} Cliente {clientInfo} cerró la conexión");
                        break;
                    }

                    // Procesar mensaje
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine($"{DateTime.Now} Mensaje de {clientInfo}: {message}");

                    // Procesar mensajes ISO (tu lógica actual)
                    ProcessIsoMessages(message);

                    // Opcional: enviar respuesta
                    string response = $"ACK: {DateTime.Now:HH:mm:ss}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await clientSocket.SendAsync(responseData, SocketFlags.None);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"{DateTime.Now} Error de socket con {clientInfo}: {ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} Error manejando cliente {clientInfo}: {ex.Message}");
            }
            finally
            {
                // Cerrar conexión solo cuando haya error o se desconecte
                SafeCloseSocket(clientSocket);
                Console.WriteLine($"{DateTime.Now} Conexión con {clientInfo} finalizada");
            }
        }

        // private void ProcessIsoMessages(string data)
        // {
        //     try
        //     {
        //         /*escribir todos los mensajes separados*/
        //         string remainingData = data;

        //         while (remainingData.Length >= 4)
        //         {
        //             // Leer longitud del mensaje (primeros 4 dígitos)
        //             if (int.TryParse(remainingData.Substring(0, 4), out int messageLength))
        //             {
        //                 if (messageLength <= remainingData.Length)
        //                 {
        //                     string isoMessage = remainingData.Substring(4, messageLength - 4);
        //                     Console.WriteLine($"{DateTime.Now} Mensaje ISO individual: {isoMessage}");

        //                     // Remover mensaje procesado
        //                     remainingData = remainingData.Substring(messageLength);
        //                 }
        //                 else
        //                 {
        //                     // Mensaje incompleto, esperar más datos
        //                     break;
        //                 }
        //             }
        //             else
        //             {
        //                 Console.WriteLine($"{DateTime.Now} Error parsing message length");
        //                 break;
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"{DateTime.Now} Error procesando mensajes ISO: {ex.Message}");
        //     }
        // }

        private void ProcessIsoMessages(string data)
        {
            List<string> mess_financiero = ["0100", "0110", "0120", "0130", "0200", "0210", "0220", "0230", "0400", "0410", "0420", "0430"];
            List<string> mess_personalizacion = ["0302", "0312 ", "0800", "0810"];
            try
            {

                Iso8583AsyncToJson iso8583AsyncToJson = new Iso8583AsyncToJson(data);
                string messagejson = string.Empty;
                Console.WriteLine("Data: " + data);
                
                messagejson = iso8583AsyncToJson.GetJsonFromMessage();
              
                string queue = mess_financiero.Contains(iso8583AsyncToJson.GetMessageType()) ?
                                                "q.requests.authorice_messages.BB06" :
                                                mess_personalizacion.Contains(iso8583AsyncToJson.GetMessageType()) ?
                                                "q.requests.personalization_messages.BB06" :
                                                "q.requests.mensajes_desconocidos";
                // Llamar a mi clase que chequea aqui 
                var classifier = new MessageClassifier(Env.GetString("socket_authorize_ip_consumer"), Env.GetInt("socket_authorize_port_consumer"));
                bool resultado = classifier.ProcessMessage(messagejson, data);
                if (resultado)
                {
                    _producer.Publish(messagejson, queue);
                }
                else
                {
                    _producer.Publish(messagejson, "q.requests.mensajes_desconocidos");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client connection");
                _logger.LogError(ex.Message);
            }
        }


        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            if (_clientSocket == null || !_clientSocket.Connected)
            {
                Console.WriteLine($"{DateTime.Now} No hay cliente conectado para enviar mensaje");
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _clientSocket.SendAsync(data, SocketFlags.None, cancellationToken);
                Console.WriteLine($"{DateTime.Now} Mensaje enviado: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} Error enviando mensaje: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            SafeCloseSocket(_clientSocket);
            SafeCloseSocket(_listenerSocket);
            Console.WriteLine($"{DateTime.Now} Servidor detenido");
        }

        private void SafeCloseSocket(Socket socket)
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch
            {
                // Ignorar errores al cerrar
            }
        }

        // Métodos de utilidad (opcionales)

        public string GetClientInfo()
        {
            return _clientSocket?.RemoteEndPoint?.ToString() ?? "No client connected";
        }

        public bool isClientConnected()
        {
            return _clientSocket != null && _clientSocket.Connected;
        }

        public void Dispose()
        {
            Stop();
            _listenerSocket?.Dispose();
            _clientSocket?.Dispose();

        }
    }
}