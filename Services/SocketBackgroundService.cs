using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using autorizadora_producer.services.interfaces;
using autorizadora_producer.Services;
using DotNetEnv;

namespace autorizadora_producer.services
{
    public class SocketBackgroundService : BackgroundService
{
    private readonly SocketServer _socketServer;
    private readonly ILogger<SocketBackgroundService> _logger;
    private readonly RabbitMQ_Producer _producer;

        public SocketBackgroundService(IRabbitMQ_Producer producer, ILogger<SocketBackgroundService> logger, SocketServer socketServer)
        {
            _logger = logger;
            _socketServer = socketServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        _logger.LogInformation("El servicio de socket se esta iniciando");

            //while (!stoppingToken.IsCancellationRequested)
            // {
            await Task.Yield();
           
                try
                {
                    while(!stoppingToken.IsCancellationRequested){
                        // var clientSocket =  await _socketServer.AcceptConnection(stoppingToken);
                         
                        await _socketServer.StartListeningAsync(stoppingToken);

                        


                        // Aquí podrías manejar la conexión del cliente en un hilo/tarea separado
                        //_ = Task.Run(()=>HandleClientAsync(clientSocket, stoppingToken),stoppingToken);
                        //_ = ReceiveMessageAsync(stoppingToken);
                    }
                    
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Servicio de socket cancelado");
                    await Task.Delay(1000, stoppingToken); // Espera antes de reintentar
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en la comunicacion de socket");
                    await Task.Delay(1000, stoppingToken); // Espera antes de reintentar
                }
            // }
            
            // await Task.Delay(1000, stoppingToken);
            //  _logger.LogInformation("Socket background service is stopping.");
        }

    // private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    // {
    //     try
    //     {
            
    //             var data = await _socketServer.ReceiveAsync(cancellationToken);
    //             Iso8583AsyncToJson iso8583AsyncToJson = new Iso8583AsyncToJson(data);
    //              Console.WriteLine("Json: "+ iso8583AsyncToJson.GetJsonFromMessage());
    //              Console.WriteLine("Data: "+ data);

    //             await  _producer.Publish(iso8583AsyncToJson.GetJsonFromMessage());
            
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error handling client connection");
    //     }
    //     finally
    //     {
    //         clientSocket.Close();
    //     }
    // }
    // private async Task ReceiveMessageAsync(CancellationToken cancellationToken)
    // {
    //     try
    //     {
    //         while (!cancellationToken.IsCancellationRequested)
    //         {
    //             string responseFromAuthorize = _producer.ReceiveMessage("q.responses.authorize_messages.BB06");
    //             Iso8583Async iso8583Async = new Iso8583Async(responseFromAuthorize);                
    //             string isoToSend = iso8583Async.GetIsoFromMessage();
    //             Console.WriteLine(isoToSend);

    //             //Sending Iso to Redsa
    //             await _socketServer.SendAsync(isoToSend.ToArray());
                
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error Receiving Message");
    //     }
    // }
}
}