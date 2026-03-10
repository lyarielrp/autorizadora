using autorizadora_producer.Helpers;
using System.Text.Json;
using System.Text;
using autorizadora_producer.Entity;
using autorizadora_producer.Services;

namespace autorizadora_producer.services;

public class Iso8583ToJson
{
    private string FullMessage;
    private string BinaryMapForm;
    private string HexMapForm;
    private int Total;
    private int Pos;

    public Iso8583ToJson(string fullMessage)
    {
        FullMessage = fullMessage;
    }

    private string CommandInMessage()
    {
        return FullMessage.Substring(0, FullMessage.IndexOf('*'));
    }

    private string Message()
    {
        return FullMessage.Substring(FullMessage.IndexOf('*') + 1);
    }

    private string GetBitmapBinForm()
    {
        string message = Message();
        HexMapForm = message.Substring(4, 32);
        if ((int)message[4] >= 56)
        {
            BinaryMapForm = Utils.HexToBin(message.Substring(4, 32));
            Total = 128;
            Pos = 36;
            return BinaryMapForm;
        }
        else
        {
            BinaryMapForm = Utils.HexToBin(message.Substring(4, 16));
            Total = 64;
            Pos = 20;
            return BinaryMapForm;
        }
    }

    private string GetBitmapHexForm()
    {
        return Message().Substring(0, 4);
    }

    private string GetMessageType()
    {
        return Message().Substring(0, 4);
    }

    public string GetJsonFromMessage()
    {
        
        JsonWriterOptions writerOptions = new JsonWriterOptions { Indented = true };

        MemoryStream stream = new MemoryStream();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, writerOptions);
        writer.WriteStartObject();

        string binaryMap = GetBitmapBinForm();
        StandarManager standarManager = new StandarManager();

        writer.WriteString("X_BITMATPR", GetMessageType());
        for (int i = 1; i < Total; i++)
        {
            if (Convert.ToInt32(binaryMap.Substring(i, 1)) == 1)
            {
                try
                {
                    Standar standar = standarManager.GetStandarByBitFormat(i + 1);
                    if (standar.BitVariable == "X_BITMATPR") continue;
                    if (standar.BitNumber != "")
                    {
                        int length = 0;
                        switch (standar.BitFormat)
                        {
                            case 5:
                                {
                                    length = Convert.ToInt32(Message().Substring(Pos, 2));
                                    Pos += 2;
                                    break;
                                }
                            case 6:
                                {
                                    length = Convert.ToInt32(Message().Substring(Pos, 3));
                                    Pos += 3;
                                    break;
                                }
                            default:
                                {
                                    length = standar.BitLength;
                                    break;
                                }
                        }
                        writer.WriteString(standar.BitVariable, Message().Substring(Pos, length));
                        Pos += length;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex, Message);
                }
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        var jsonBytes = stream.ToArray();
        string json = Encoding.UTF8.GetString(jsonBytes);

        return json;

    }
}