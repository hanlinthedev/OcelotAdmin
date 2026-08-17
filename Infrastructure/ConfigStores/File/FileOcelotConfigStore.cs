using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores.File;

public sealed class FileOcelotConfigStore : IOcelotConfigStore
{
	public ConfigStoreType Type => ConfigStoreType.File;

	public async Task<ConfigStoreReadResult> ReadAsync(
		Gateway gateway,
		CancellationToken cancellationToken = default)
	{
		var settings = gateway.FileSettings
					   ?? throw new OcelotConfigStoreException(
						   "File configuration settings are missing.");

		var path =
			settings.ConfigurationPath;

		if (!System.IO.File.Exists(path))
		{
			throw new OcelotConfigStoreException(
				$"Configuration file '{path}' does not exist.");
		}

		try
		{
			var json =
				await System.IO.File.ReadAllTextAsync(
					path,
					cancellationToken);

			return new ConfigStoreReadResult
			{
				ConfigurationJson = json,

				// We'll add proper File concurrency later.
				Version = null
			};
		}
		catch (Exception ex)
			when (ex is IOException or UnauthorizedAccessException)
		{
			throw new OcelotConfigStoreException(
				$"Failed to read configuration file '{path}'.",
				ex);
		}
	}

	public async Task PublishAsync(Gateway gateway, string configurationJson, string? expectedVersion = null,CancellationToken cancellationToken = default)
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
	
	public async Task<ConfigStoreHealthResult> CheckHealthAsync(
		Gateway gateway,
		CancellationToken cancellationToken = default)
	{
		var settings = gateway.FileSettings
					   ?? throw new OcelotConfigStoreException(
						   "File configuration settings are missing.");

		var path =
			settings.ConfigurationPath;

		if (!System.IO.File.Exists(path))
		{
			return new ConfigStoreHealthResult
			{
				IsReachable = true,
				KeyExists = false,
				Message =
					$"Configuration file '{path}' does not exist."
			};
		}

		try
		{
			var content =
				await System.IO.File.ReadAllTextAsync(
					path,
					cancellationToken);

			var validJson = IsJson(content);

			return new ConfigStoreHealthResult
			{
				IsReachable = true,
				KeyExists = true,
				IsReadable = true,
				ConfigurationIsValid = validJson,
				Message = validJson
					? "Configuration file is accessible."
					: "Configuration file does not contain valid JSON."
			};
		}
		catch (UnauthorizedAccessException)
		{
			return new ConfigStoreHealthResult
			{
				IsReachable = true,
				KeyExists = true,
				Message =
					"The configuration file exists but cannot be read."
			};
		}
		catch (IOException ex)
		{
			return new ConfigStoreHealthResult
			{
				IsReachable = true,
				KeyExists = true,
				Message = ex.Message
			};
		}
	}
	
	private static bool IsJson(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		try
		{
			using var document =
				System.Text.Json.JsonDocument.Parse(value);

			return true;
		}
		catch (System.Text.Json.JsonException)
		{
			return false;
		}
	}
}