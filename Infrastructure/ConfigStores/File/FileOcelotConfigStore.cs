using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores.File;

public sealed class FileOcelotConfigStore : IOcelotConfigStore
{
	public ConfigStoreType Type => ConfigStoreType.File;

	public async Task<string> ReadAsync(Gateway gateway, CancellationToken cancellationToken = default)
	{
		var settings = gateway.FileSettings ?? throw new OcelotConfigStoreException("File Configuration settings are missing.");
		var path = settings.ConfigurationPath;
		if (!System.IO.File.Exists(path))
		{
			throw new OcelotConfigStoreException($"Configuration file at '{path}' does not exist!");
		}

		try
		{
			return await System.IO.File.ReadAllTextAsync(path, cancellationToken);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			throw new OcelotConfigStoreException($"Failed to read configuration file '{path}'.", ex);
		}
	}

	public async Task PublishAsync(Gateway gateway, string configurationJson, CancellationToken cancellationToken = default)
	{
		var settings = gateway.FileSettings ??
					   throw new OcelotConfigStoreException("File configuration settings are missing.");
		var path = settings.ConfigurationPath               ;
		var tempPath = $"{path}.tmp";

		try
		{
			await System.IO.File.WriteAllTextAsync(tempPath, configurationJson, cancellationToken);
			System.IO.File.Move(tempPath, path,overwrite:true);
		}
		catch (Exception e) when(e is IOException or UnauthorizedAccessException)
		{
			if (System.IO.File.Exists(tempPath))
			{
				System.IO.File.Delete(tempPath);
			}

			throw new  OcelotConfigStoreException($"Failed to write configuration file '{path}'.", e);
		}

	}
}