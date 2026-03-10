using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DotNetEnv;
using autorizadora_producer.services.interfaces;
using System.Net.Sockets;

namespace autorizadora_producer.services;
public class RabbitMQ_Producer : IRabbitMQ_Producer
{
    private IConnection connection;
    private string exchangeName;
    private string queueName;
    //private IModel channel;
    private IConfiguration _conf;
    private readonly ILogger<RabbitMQ_Producer> _logger;
    private readonly IModel _channel;
    private readonly List<EventingBasicConsumer> _consumers;

    //private string vhost;
    //private string foincode;

    public RabbitMQ_Producer(IConfiguration conf, ILogger<RabbitMQ_Producer> logger)
    {
        _logger = logger;
        this._conf = conf;
        this.exchangeName = Env.GetString("EXCHANGE_NAME");

        ConnectionFactory factory = new ConnectionFactory();
        factory.Uri = new Uri(Env.GetString("RABBITMQ") + Env.GetString("RABBITMQ_VIRTUALHOST"));
        factory.RequestedConnectionTimeout = TimeSpan.FromMinutes(60);

        //Make the connection
        connection = factory.CreateConnection();

        //Make channell
        _channel = connection.CreateModel();
    }
    public async Task Publish(string data, string queue_name)
    {
        _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
        _channel.QueueDeclare(queue_name, true, false, false, null);
        _channel.QueueBind(queue_name, exchangeName, queue_name, null);
        var body = Encoding.UTF8.GetBytes(data);
        _channel.BasicPublish(exchangeName, queue_name, null, body);

            
        await Task.CompletedTask;
    }
    public async void Consume(List<string> queueNames,Socket _clientSocket, Action<string,string> callback)
    {
        bool result = true;
        try
        {
            
            
                 if (_channel.MessageCount(queueNames[0]) != 0)
                {
                    var listofconsumerNamesforTag = new Dictionary<string,string>();
                var consumer = new EventingBasicConsumer(_channel);
                  
                    consumer.Received += (model, ea) =>
                        {
                            Console.WriteLine("count" +_channel.ConsumerCount("q.responses.personalization_messages.BB06"));
                            var body = ea.Body.Span.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            Console.WriteLine("modelo: " + json);
                            if(_clientSocket.Connected){
                                listofconsumerNamesforTag.TryGetValue(ea.ConsumerTag,out string value);
                            callback(json, value);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            }
                            else{
                            //_channel.BasicNack(ea.DeliveryTag,false,true);
                            _channel.BasicCancel(ea.ConsumerTag);    
                            }   
                            Thread.Sleep(1000);
                            
                        };
                
                foreach(var queue in queueNames){
                    var consumertag = _channel.BasicConsume(queue, false, consumer);
                    listofconsumerNamesforTag.Add(consumertag, queue);
                }
                
                }
                else{
                  Thread.Sleep(2000);  
                }
                // foreach (var queue_name in queueNames){
                //         channel.QueueDeclare(queue_name, true, false, false, null);
                //         if(_clientSocket.Connected)
                //         channel.BasicConsume(queue_name, false, consumer);
                //     }    
                

                
                 //channel?.Dispose();
                 //connection?.Dispose();
            
            
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: " + ex);
        }
    }
   

}