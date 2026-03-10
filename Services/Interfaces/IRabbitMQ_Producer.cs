using System.Net.Sockets;
namespace autorizadora_producer.services.interfaces;

    public interface IRabbitMQ_Producer
    {
        Task Publish(string data, string queue_name);
        void Consume(List<string> queueNames,Socket _clientSocket, Action<string, string> callback);
    }
