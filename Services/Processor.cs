using System;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using DotNetEnv;
public class MessageClassifier
{
    private readonly JObject _classifierConfig;
    private readonly string _host;
    private readonly int _port;

    private static readonly string _configFilePath=@"./ClasificadorMensajeBITS.json";

    public MessageClassifier(string host, int port)
    {
        // Cargar el archivo de configuración JSON
        string configJson = File.ReadAllText(_configFilePath);
        _classifierConfig = JObject.Parse(configJson);
        _host = host;
        _port = port;
        int _out_port=10071;
    }

    public bool ProcessMessage(string jsonMessage, string iso8583Message)
    {
        try
        {
            Console.WriteLine($"{jsonMessage}");
            // Parsear el mensaje JSON recibido
            JObject messageData = JObject.Parse(jsonMessage);
            
            // Extraer X_BITMATPR y X_PRCODE del mensaje
            string? bitmatpr = messageData["X_BITMATPR"]?.ToString();
            string? prcode = messageData["X_PRCODE"]?.ToString();

            if (string.IsNullOrEmpty(bitmatpr))
            {
                Console.WriteLine("Error: X_BITMATPR no encontrado en el mensaje");
                
            }

            // Buscar en la configuración si existe un mensaje que coincida
            bool found = SearchInClassifierConfig(bitmatpr, prcode);

            if (found)
            {
                Console.WriteLine($"Mensaje encontrado: BITMATPR={bitmatpr}, PRCODE={prcode}");
                
                // Enviar mensaje ISO8583 via socket
                
                
                return true;
            }
            else
            {
                Console.WriteLine($"Mensaje NO encontrado: BITMATPR={bitmatpr}, PRCODE={prcode}");
                _ = Task.Run(()=>SendViaSocket(iso8583Message));
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando mensaje: {ex}");
            return false;
        }
    }

    private bool SearchInClassifierConfig(string bitmatpr, string prcode)
    {
        if (_classifierConfig == null)
            return false;

        // Recorrer todas las propiedades del JSON de configuración
        foreach (var property in _classifierConfig.Properties())
        {
            
            var messageConfig = property.Value;
            
            if (messageConfig.Type == JTokenType.Object)
            {
                JObject child = (JObject)messageConfig;
                string? configBitmatpr = child["X_BITMATPR"]?.ToString();
                string? configPrcode = child["X_PRCODE"]?.ToString();

                // Comparar BITMATPR (siempre requerido)
                bool bitmatprMatch = configBitmatpr == bitmatpr;

                // Comparar PRCODE (puede ser null o "default" en algunos casos)
                bool prcodeMatch = configPrcode == prcode;

                if (bitmatprMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SendViaSocket(string iso8583Message)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                // Conectar al host
                client.Connect(_host, _port);
                
                // Obtener stream de red
                NetworkStream stream = client.GetStream();
                
                // Convertir mensaje a bytes
                byte[] data = Encoding.UTF8.GetBytes(iso8583Message);
                
                // Enviar mensaje
                stream.Write(data, 0, data.Length);
                
                Console.WriteLine($"Mensaje ISO8583 enviado a {_host}:{_port}");
                
                // Opcional: leer respuesta
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Console.WriteLine($"Respuesta recibida: {response}");
                //if (response=="ACK"){
                //    while (true)
                //    {
                //      string respserver = ReceiveMessageFromServer(_host,Env.GetInt("socket_authorize_port_reciver"));
                //      Console.WriteLine(respserver);  
                //      Thread.Sleep(2000);
                //    }
                    
                //}
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando mensaje via socket: {ex.Message}");
            throw;
        }
    }
    public string ReceiveMessageFromServer(string ipAddress, int port)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        
        try
        {
            // Crear cliente TCP y conectar
            client = new TcpClient();
            client.Connect(ipAddress, port);
            
            Console.WriteLine($"Conectado al servidor {ipAddress}:{port}");
            
            // Obtener el stream de red
            stream = client.GetStream();
            
            // Buffer para almacenar los datos recibidos
            byte[] buffer = new byte[1024];
            StringBuilder messageBuilder = new StringBuilder();
            
            // Leer datos mientras haya información disponible
            do
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string partialMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(partialMessage);
                }
            } while (stream.DataAvailable); // Mientras haya datos disponibles
            
            string receivedMessage = messageBuilder.ToString();
            
            // Si no se recibió ningún mensaje
            if (string.IsNullOrEmpty(receivedMessage))
            {
                return "No se recibió ningún mensaje del servidor.";
            }
            
            return receivedMessage;
        }
        catch (SocketException ex)
        {
            return $"Error de socket: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
        finally
        {
            // Cerrar conexiones
            stream?.Close();
            client?.Close();
        }
    }
}

// Método de uso simplificado (versión estática)
public static class MessageProcessor
{
    private static MessageClassifier _classifier;

    static MessageProcessor()
    {
        // Inicializar con configuración por defecto
        _classifier = new MessageClassifier(Env.GetString("socket_authorize_ip_consumer"), Env.GetInt("socket_authorize_port_consumer"));
    }

    public static bool ProcessISOMessage(string jsonMessage, string iso8583Message, string host = null, int port = 0)
    {
        try
        {
            JsonNode messageData = JsonNode.Parse(jsonMessage);
            string bitmatpr = messageData?["X_BITMATPR"]?.GetValue<string>();
            string prcode = messageData?["X_PRCODE"]?.GetValue<string>();

            if (string.IsNullOrEmpty(bitmatpr))
                return false;

            // Configurar host y puerto si se proporcionan
            if (!string.IsNullOrEmpty(host) && port > 0)
            {
                _classifier = new MessageClassifier(host, port);
            }

            return _classifier.ProcessMessage(jsonMessage, iso8583Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en ProcessISOMessage: {ex.Message}");
            return false;
        }
    }
}

// Ejemplo de uso
class Programa
{
    static void Main()
    {
        // Ejemplo de mensaje JSON de entrada
        string jsonMessage = @"{
            ""X_BITMATPR"": ""0200"",
            ""X_PRCODE"": ""930000"",
            ""X_PAN"": ""1234567890123456""
        }";

        string iso8583Message = "ISO8583 message content here...";

        // Usar la versión estática simplificada
        bool result = MessageProcessor.ProcessISOMessage(
            jsonMessage, 
            iso8583Message, 
            "192.168.1.100", 
            12345
        );

        Console.WriteLine($"Procesamiento exitoso: {result}");
    }
}


// Inicializar
//var classifier = new MessageClassifier("ClasificadorMensajeBITS.json", "host", 1234);

// Procesar mensaje
//bool success = classifier.ProcessMessage(jsonMessage, iso8583Message);