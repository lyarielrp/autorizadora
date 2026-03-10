using System.Text;
using System.Net;
using System.Net.Sockets;

namespace autorizadora_producer.services;
public class SocketServerViejo
{
    private Socket clientSocket;
    private Socket listenSocket;
    private int puerto;
    private string ipEntrada;

    public SocketServerViejo(int port, string ip)
    {
        puerto = port;
        ipEntrada = ip;

        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress hostIPAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(hostIPAddress, port);
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        Console.WriteLine(DateTime.Now + " parseo de datos " + ip + " - " + port.ToString());
        listenSocket.Bind(ipEndPoint);
        listenSocket.Listen(5);

        Console.WriteLine("Servidor listo y escuchando en {0}:{1}", ip, port);
    }

    bool SocketConectadoOK(Socket s)
    {
        
        bool conexionEstablecidaOK = s.Poll(1000, SelectMode.SelectRead);
        
        
        if (conexionEstablecidaOK)
            return true;
        return false;
    }

    public Socket getListenSocket()
    {
        return listenSocket;
    }

    public Socket getClientSocket()
    {
        return clientSocket;
    }

    public async Task<Socket> AcceptConnection(CancellationToken stoppingToken)
    {
        try
        {
            Console.WriteLine("AcceptConnection");
            if (!SocketConectadoOK(listenSocket))
                conectar(ipEntrada, puerto, stoppingToken);

            clientSocket = await listenSocket.AcceptAsync().ConfigureAwait(false);

            return clientSocket;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An Exception ocurred accept connection {0}", ex.Message);
            throw;
        }
    }

    public async void conectar(string ip, int port,CancellationToken stoppingToken)
    {
        Console.WriteLine(DateTime.Now + " parseo de datos " + ipEntrada + " - " + puerto.ToString());
        IPEndPoint conexion = new IPEndPoint(IPAddress.Parse(ipEntrada), puerto);

        Console.WriteLine("creando conexion...");
        bool primerIntento = true;

        while (!SocketConectadoOK(listenSocket) && !stoppingToken.IsCancellationRequested)
        {
            
            listenSocket.Close();
            listenSocket.Dispose();
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            if(primerIntento){
              Console.WriteLine(DateTime.Now + " Estableciendo conexion");
              primerIntento = false;  
            }
            else {
                Console.WriteLine(DateTime.Now + " Reintentando establecer conexion");
            }
            try
            {                
                listenSocket.Bind(conexion);                
                listenSocket.Listen(2);
                Thread.Sleep(3000);
                
            }
            // catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            // {
            //     Console.WriteLine(DateTime.Now + " Puerto en uso, reintentando...");
            //     Thread.Sleep(1000);
            // }
            catch (SocketException e)
            {
                Console.WriteLine($"{DateTime.Now} Error de Socket: { e.SocketErrorCode.ToString()} || message: {e.Message} ");
                Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " conectandose a REDSA: " + "Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + "*****ToString - " + e.ToString());
                Thread.Sleep(1000);
            }
        }
        Console.WriteLine(DateTime.Now +" Conexion exitosa");
    }


    public async Task<string> ReceiveAsync(CancellationToken stoppingToken)
    {
        string data = "";
        try
        {
            if (!SocketConectadoOK(listenSocket))
            {
                Console.WriteLine(DateTime.Now + " Verificando socket");
                conectar(ipEntrada, puerto,stoppingToken);
            }

            Console.WriteLine(DateTime.Now + " escucha");
            const int BufferSize = 64 * 1024;

            byte[] recibir_info = new byte[BufferSize];
            Console.WriteLine(DateTime.Now + " recibir_info " + recibir_info[0].ToString());

            int array_size = 0;
            array_size = await clientSocket.ReceiveAsync(recibir_info,stoppingToken).ConfigureAwait(false);
            Console.WriteLine(DateTime.Now + " array_size " + array_size.ToString());

            Array.Resize(ref recibir_info, array_size);

            if (array_size == 0)
            {
                Console.WriteLine("Error reading message from client, no data was received ");
                data = Encoding.ASCII.GetString(recibir_info, 0, recibir_info.Length);
                Console.WriteLine("GetString " + data);
            }

            data = Encoding.Default.GetString(recibir_info);
            Console.WriteLine("Encoding.Default " + data);

            Console.WriteLine("respuesta completa " + data);

            /*escribir todos los mensajes separados*/
            // while (data.Length > 0)
            // {
            //     int len = Int16.Parse(data.Substring(0, 4)) - 4;
            //     Console.WriteLine(len);
            //     string menIso = data.Substring(4, len);

            //     Console.WriteLine("mensaje individual " + menIso);

            //     data = data.Substring(len + 4, data.Length - (len + 4));
            // }

            string message = Encoding.ASCII.GetString(recibir_info, 0, array_size);
            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
            throw;
        }
        return data;
    }

    public async Task SendAsync(char[] data)
    {
        try
        {
            var dataForSend = Encoding.ASCII.GetBytes(data);
            await clientSocket.SendAsync(dataForSend).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An Exception ocurred during send {0}", ex.Message);
            throw;
        }
    }
}

//CLEIDY_CODIGO public class SocketServer
// public class SocketServer
// {	
//     private Socket listenSocket;
//     int puerto;
//     string ipEntrada;

//     public SocketServer(int port, string ip)
//     {   
//         try
//         {
//             conectar(ip, port);
//             Console.WriteLine("Servidor listo y escuchando en {0}:{1}", ip, port);
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine(DateTime.Now + " conectándose a REDSA: " + "Excepción - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + "*****ToString - " + e.ToString());
//         }
//         finally
//         {
//             puerto = port;
//             ipEntrada = ip;
//         }
//     }

//     public void conectar(string ip, int port)
//     {
//         listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//         Console.WriteLine(DateTime.Now + " parseo de datos "+ip + " - "+port.ToString());
//         IPEndPoint conexion = new IPEndPoint(IPAddress.Parse(ip), port);

// 		Console.WriteLine("creando conexion...");

//         while (!SocketConectadoOK(listenSocket))
//         {
//             try
//             {
//                 //listenSocket.Connect(ipEndPoint);
//                 listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                 listenSocket.Bind(conexion);
//                 Console.WriteLine(DateTime.Now + " haciendo BIND a la conexion");
//                 listenSocket.Listen(10);
//                 Console.WriteLine(DateTime.Now + " haciendo LISTEN. conectandose a REDSA: ");
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(DateTime.Now + " conectandose a REDSA: " + "Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + "*****ToString - " + e.ToString());
//                 Thread.Sleep(1000);
//             }
//         }
//     }

//     bool SocketConectadoOK(Socket s)
//     {
//         bool conexionEstablecidaOK = s.Poll(1000, SelectMode.SelectRead);
//         bool hayDatosRecibidos = (s.Available != 0);
//         if (conexionEstablecidaOK)
//             return true;
//         return false;
//     }

// 	public async Task<string> ReceiveAsync(Socket clientSocket)
//     {
//         Console.WriteLine(DateTime.Now + " ReceiveAsync");
//         string message = "";
//         try
//         {
//             if (!SocketConectadoOK(listenSocket))
//             {
//                 Console.WriteLine(DateTime.Now + " Verificando socket");
//                 conectar(ipEntrada, puerto);
//             }
//             while (SocketConectadoOK(listenSocket) && (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)))
//             {
//                 Console.WriteLine(DateTime.Now + " recibiendo info");
//                 message = await escucha(clientSocket);
//                 if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
//                 {   
// 					Console.WriteLine("No hay mensaje recibido ");
//                     ReceiveAsync(clientSocket);
//                 }
//                 Console.WriteLine("mensaje recibido "+message);
//             }
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
//         }
//         return message;
// 	}

//     public async Task<string> escucha(Socket clientSocket)
//     {
//         Console.WriteLine(DateTime.Now + " escucha");
//         string data = "";
//         try
//         {
//             byte[] recibir_info = new byte[clientSocket.ReceiveBufferSize];
//             Console.WriteLine(DateTime.Now + " recibir_info " + recibir_info[0].ToString());

//             int array_size = 0;
//             array_size = clientSocket.Receive(recibir_info, 0, recibir_info.Length,0);
//             Console.WriteLine(DateTime.Now + " array_size " + array_size.ToString());

//             Array.Resize(ref recibir_info, array_size);
//             data = Encoding.Default.GetString(recibir_info);
//             Console.WriteLine("Encoding.Default "+data);

//             if (array_size == 0)
//             {
//                  Console.WriteLine("Error reading message from client, no data was received ");
//                  data = Encoding.ASCII.GetString(recibir_info, 0, recibir_info.Length);
//                  Console.WriteLine("GetString "+data);
//             }

//             Console.WriteLine("respuesta completa " + data);

//             /*escribir todos los mensajes separados*/
//             while (data.Length > 0)
//             {
//                 int len = Int16.Parse(data.Substring(0, 4)) - 4;
//                 string menIso = data.Substring(4, len);

//                 Console.WriteLine("mensaje individual " + menIso);

//                 data = data.Substring(len + 4, data.Length - (len + 4));
//             }
//         }
//         catch (SocketException e)
//         {
//             if (e.SocketErrorCode == SocketError.TimedOut)
//             {
//                 Console.WriteLine("SocketExecption => Timeout");

//                 Console.WriteLine(DateTime.Now + " -- TIEMOUT RECIBIENDO -- " + e.Message);

//                 Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
//             }
//             else
//             {
//                 Console.WriteLine("SocketExecption => " + e.ToString());

//                 Console.WriteLine(DateTime.Now + " " + e.Message);

//                 Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
//             }
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
//         }
//         listenSocket.Close();
//         listenSocket.Dispose();
//         return data;
//     }

//     public async Task SendAsync(char[] mensajeParaEnviar, Socket clientSocket)
// 	{
//         while (!SocketConectadoOK(clientSocket))
//         {
//             conectar(ipEntrada, puerto);
//         }
//         byte[] enviar_info = new byte[100];
//         string isoEncriptado = (mensajeParaEnviar.Length + 4).ToString("D" + 4) + mensajeParaEnviar;
//         enviar_info = Encoding.Default.GetBytes(isoEncriptado);

//         Console.WriteLine("Enviando... " + isoEncriptado);
//         try
//         {
//             await clientSocket.SendAsync(enviar_info);
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine(DateTime.Now + " " + e.Message);
//             Console.WriteLine("Excepcion - " + e.Message + " ***Source - " + e.Source + " ***Target - " + e.TargetSite + " *****ToString - " + e.ToString());
//         }
// 	}

//     public void EscribeTraza(string mensaje)
//     {
//         try
//         {
//             string direccionTraza = @"C:\\AC_sockets" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
//             if (System.IO.File.Exists(direccionTraza))
//             {
//                 StreamWriter WriteReportFile = System.IO.File.AppendText(direccionTraza);
//                 WriteReportFile.Write(DateTime.Now.ToString() + "   " + mensaje + Environment.NewLine);
//                 WriteReportFile.Close();
//             }
//             else
//             {
//                 System.IO.File.Create(direccionTraza);
//                 System.IO.StreamWriter file = new System.IO.StreamWriter(direccionTraza);
//                 file.WriteLine(DateTime.Now.ToString() + "  " + mensaje + Environment.NewLine);
//                 file.Close();
//             }
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine("Error al salvar trazas " + e.Message);
//         }
//     }

//     public async Task<Socket> AcceptConnection()
// 	{
// 		try
// 		{
// 			return await listenSocket.AcceptAsync().ConfigureAwait(false);			 
// 		}
// 		catch (Exception ex)
// 		{
// 			Console.WriteLine("AcceptConnection. An Exception ocurred accept connection {0}", ex.Message);
//             throw;
// 		}
// 	}
//      public Socket getListenSocket()
// 	{ 
//         return listenSocket;
// 	}
// }
