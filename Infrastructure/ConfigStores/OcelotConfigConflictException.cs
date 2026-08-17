namespace OcelotAdmin.Infrastructure.ConfigStores;

public sealed class OcelotConfigConflictException
	: OcelotConfigStoreException
{
	public OcelotConfigConflictException(
		string message)
		: base(message)
	{
	}
}