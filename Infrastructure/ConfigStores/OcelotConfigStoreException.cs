namespace OcelotAdmin.Infrastructure.ConfigStores;

public sealed class OcelotConfigStoreException : Exception
{
	public OcelotConfigStoreException(string message) : base(message)
	{
	}
	
	public OcelotConfigStoreException(string message, Exception innerException) : base(message, innerException)
	{
	}	
}