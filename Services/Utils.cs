namespace autorizadora_producer.Helpers;

public class Utils
{
	public static string HexToBin(string hexData)
	{
		return String.Join(String.Empty, hexData.Select(
			c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
		));
	}	

	public static string BinToHex(string binData)
	{
		string hexMap = "";
		for (int i = 0; i < binData.Length; i += 4) 
		{
			string hex = Convert.ToString(Convert.ToInt32(binData.Substring(i, 4), 2), 16);
        	hexMap += hex;
		}
		return hexMap;
	}

	public static string GetBetween(string strSource, string strStart, string strEnd)
	{
		if(strSource.Contains(strStart) && strSource.Contains(strEnd))
		{
			int Start, End;
			Start = strSource.IndexOf(strStart, 0) + strStart.Length;
			End = strSource.IndexOf(strEnd, Start);
			return strSource.Substring(Start, End - Start);
		}
		return "";
	}	
}