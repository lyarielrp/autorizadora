using System.Text.Json;
using autorizadora_producer.Entity;
using autorizadora_producer.Exceptions;

namespace autorizadora_producer.Services;
public class StandarManager
{
	private List<Standar> standars;

	public StandarManager()
	{
		FileStream json = File.OpenRead("standars.json");
		standars = JsonSerializer.Deserialize<List<Standar>>(json);
	}

	public List<Standar> GetStandarList()
	{
		return standars;
	}

	public Standar getStandarByBitVariable(string bitVariable)
	{
		foreach(Standar item in standars)
		{
			if(item.BitVariable == bitVariable)
				return item;
		}
		throw new NotFoundStandarException($"Not Found Standar that correspond to {bitVariable}");
	}

	public int getIndexByBitVariable(string bitVariable)
	{
		// int index = 0;
		// foreach(Standar item in standars)
		// {
		// 	if(item.BitVariable == bitVariable)
		// 		return index;
		// 	index++;
		// }

		//Lo mismo de arriba pero con LINQ
		int index = standars.FindIndex(c => c.BitVariable ==  bitVariable);
		return  index >= 0 ? index : 
			throw new NotFoundStandarException($"Not Found Standar that correspond to {bitVariable}");
	}


	public Standar getStandarByBitNumber(string bitNumber)
	{
		foreach(Standar item in standars)
		{
			if(item.BitNumber == bitNumber)
				return item;
		}
		throw new NotFoundStandarException($"Not Found Standar that correspond to {bitNumber}");
	}

	public Standar GetStandarByBitFormat(int formatBit)
	{
		string bitNumber = "";

		if(formatBit >= 10 && formatBit < 100) 
		{
			bitNumber = $"0{formatBit}";
		}     
    	else if(formatBit < 10) 
		{
        	bitNumber = $"00{formatBit}";     
    	} 
    	else if (formatBit >= 100)
		{
        	bitNumber = Convert.ToString(formatBit);
    	}
		return getStandarByBitNumber(bitNumber);	
    }
}