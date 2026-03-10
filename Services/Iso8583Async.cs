using autorizadora_producer.Helpers;
using System.Text.Json;
using System.Text;
using autorizadora_producer.Entity;
using autorizadora_producer.Services;

namespace autorizadora_producer.services;

public class Iso8583Async
{
    private string JsonMessage;
    private string BinaryMapForm;
    private string HexMapForm;
    private int Total;
    private int Pos;

    public Iso8583Async(string jsonMessage)
    {
        JsonMessage = jsonMessage;
    }

    private string GetBitmapBinForm()
    {
        StandarManager standarManager = new StandarManager();
        string[] bitArray = new string[128];
        Array.Fill<string>(bitArray, "0");

        bool doubleMap = false;
        var reader = JsonDocument.Parse(JsonMessage);
        var root = reader.RootElement;
        foreach (var jsonProp in root.EnumerateObject())
        {
            int index = standarManager.getIndexByBitVariable(jsonProp.Name);
            bitArray[index] = "1";
            if (index > 64)
            {
                doubleMap = true;
            }
        }
        string bitMap = "";
        int length = doubleMap ? 128 : 64;
        for (int i = 0; i < length; i++)
        {
            bitMap += bitArray[i];
        }
        return bitMap;
    }

    public string GetIsoFromMessage()
    {
        StandarManager standarManager = new StandarManager();
        string message = "";
        var reader = JsonDocument.Parse(JsonMessage);
        var root = reader.RootElement;
        foreach (var jsonProp in root.EnumerateObject())
        {
            if ("X_BITMATPR" == jsonProp.Name)
            {
                Console.WriteLine(jsonProp.Value);
                message = jsonProp.Value.ToString();
            }
        }
        message += Utils.BinToHex(GetBitmapBinForm());

        foreach (var jsonProp in root.EnumerateObject())
        {
            if ("X_BITMATPR" == jsonProp.Name) continue;
            Standar standar = standarManager.getStandarByBitVariable(jsonProp.Name);
            if (standar.BitFormat == 5)
            {
                string length = "00" + jsonProp.Value.ToString().Length;
                message += length.Substring(length.Length - 2, 2);
            }
            if (standar.BitFormat == 6)
            {
                string length = "000" + jsonProp.Value.ToString().Length;
                message += length.Substring(length.Length - 3, 3);
            }
            message += jsonProp.Value.ToString();
        }
        return message;
    }

}