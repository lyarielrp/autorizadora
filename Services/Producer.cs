
// using System.Collections;
// using System.Collections.Concurrent;
// using System.Text;
// using RabbitMQ.Client;
// using RabbitMQ.Client.Events;
// using DotNetEnv;

// namespace autorizadora_producer.services;
// public class Producer 
// {
//     public string? HostName { get; set; }
//     public short Port { get; set; }
//     public string? VirtualHost { get; set; }
//     public string? UserName { get; set; }
//     public string? Password { get; set; }
//     public IConnection? Connection { get; private set; }
//     public IChannel Channel { get; private set; }

//     private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();
//     private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();

//     public Producer()
//     {
//         HostName = Env.GetString("RABBITMQ_HOST");
//         Port =  Convert.ToInt16(Env.GetInt("RABBITMQ_PORT"));
//         VirtualHost = Env.GetString("RABBITMQ_VIRTUALHOST");
//         UserName = Env.GetString("RABBITMQ_USERNAME");
//         Password = Env.GetString("RABBITMQ_PASSWORD");
//         MakeConnection();
        
//     }

//     private async void MakeConnection()
//     {
//         ConnectionFactory factory = new ConnectionFactory { 
//             HostName = HostName, 
//             Port = Port, 
//             VirtualHost = VirtualHost, 
//             Password = Password, 
//             UserName = UserName 
//         };
//         factory.ClientProvidedName = "socket_authorize";
//         Connection = await factory.CreateConnectionAsync();
//     }

//     public IConnection GetConnection() 
//     {
//         return Connection;
//     }

//     public async void MakeQueue(string Name) 
//     {
//         Channel = await Connection.CreateChannelAsync();

//         await Channel.QueueDeclareAsync(queue: Name, durable: true, exclusive: false, autoDelete: false,  arguments: null);
//     }
    
//     public async Task<bool> SendMessage(string message, string exchange, string routingKey) 
//     {
//         MakeConnection();
//         Channel = await Connection.CreateChannelAsync();
//         byte[] body = Encoding.UTF8.GetBytes(message);

//         var properties = new BasicProperties(){
//             Persistent = true,
//             ReplyTo = "q.responses.authorize_messages.BB06"
//         };
//         await Channel.QueueDeclareAsync(queue: "q.responses.authorize_messages.BB06", durable: true, exclusive: false, autoDelete: false,  arguments: null);
       

//         //Make the publish queue
//         await Channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Topic);
//         await Channel.QueueBindAsync(queue: "q.responses.authorize_messages.BB06", exchange: exchange, routingKey: string.Empty);
//         Console.WriteLine("Enviando, conexion"+Channel.IsOpen);
//         await Channel.BasicPublishAsync(exchange: exchange,
//                              routingKey: routingKey,
//                              basicProperties: properties,
//                              body: body,
//                              mandatory: false
//                              );
//         return true;
//     }
//     public string ReceiveMessage(string queue) 
//     {
//         MakeConnection();
//         string message = "";
//         var consumer = new AsyncEventingBasicConsumer(Channel);
//         consumer.ReceivedAsync += (model, ea) => 
//         {
//             var body = ea.Body.ToArray();
//             var messageFromQueue = Encoding.UTF8.GetString(body);

//             //Handle response
//             Console.WriteLine($" [x] Received {messageFromQueue}");
//             Channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
//             respQueue.Add(messageFromQueue);
//             return Task.CompletedTask;
//         };

//         Channel.BasicConsumeAsync(queue: queue,
//                              autoAck: false,
//                              consumer: consumer);
//         return respQueue.Take();
//     }
// }

