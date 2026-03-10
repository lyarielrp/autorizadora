using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
namespace autorizadora_producer.services.interfaces;

    public interface ISocketServer
    {
        Task StartListeningAsync(CancellationToken cancellationToken);
        Task SendAsync(string message, CancellationToken cancellationToken);
        void Stop();
        bool isClientConnected();
        string GetClientInfo();
    }
