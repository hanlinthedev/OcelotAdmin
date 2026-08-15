using System.Text.Json;
using OcelotAdmin.Domain.Ocelot;

namespace OcelotAdmin.Services;

public sealed class OcelotConfigurationSerializer
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = true
	};

	public OcelotConfiguration Deserialize(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new InvalidOperationException(
				"Ocelot configuration is empty.");
		}

		var configuration =
			JsonSerializer.Deserialize<OcelotConfiguration>(
				json,
				SerializerOptions);

		if (configuration is null)
		{
			throw new InvalidOperationException(
				"Failed to deserialize Ocelot configuration.");
		}

		return configuration;
	}

	public string Serialize(OcelotConfiguration configuration)
	{
		return JsonSerializer.Serialize(
			configuration,
			SerializerOptions);
	}
}