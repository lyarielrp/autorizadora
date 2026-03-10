namespace autorizadora_producer.Exceptions;

public class NotFoundStandarException: Exception
{
	public NotFoundStandarException()
	{		
	}

	public NotFoundStandarException(string message)
	:base(message)
	{		
	}

	public NotFoundStandarException(string message, Exception inner)
	:base(message, inner)
	{		
	}
}