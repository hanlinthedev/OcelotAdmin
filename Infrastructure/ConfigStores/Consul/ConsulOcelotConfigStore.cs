using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores.Consul;

public sealed class ConsulOcelotConfigStore : IOcelotConfigStore
{
	private readonly HttpClient _client;

	public ConsulOcelotConfigStore(HttpClient client)
	{
		_client = client;
	}
	
	public ConfigStoreType Type =>  ConfigStoreType.Consul;
	
	public async Task<string> ReadAsync(Gateway gateway, CancellationToken cancellationToken = default)
	{
		var settings = gateway.ConsulSettings ?? throw new OcelotConfigStoreException("Consul Configuration settings not found");

		using var request = new HttpRequestMessage(HttpMethod.Get, BuildKeyUrl(settings));
		AddToken(request,settings.Token);

		try
		{
			   using var response = await _client.SendAsync(request, cancellationToken);

			   if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			   {
				   throw new OcelotConfigStoreException($"Consul key '{settings.ConfigurationKey}' was not found.");
			   }
			   
			   if (!response.IsSuccessStatusCode)
			   {
				   throw new OcelotConfigStoreException(
					   $"Consul returned HTTP {(int)response.StatusCode} " +
					   $"while reading '{settings.ConfigurationKey}'.");
			   }
			   
			   return await response.Content.ReadAsStringAsync(
				   cancellationToken);
		}
		catch (OcelotConfigStoreException)
		{
			throw;
		}
		catch (HttpRequestException ex)
		{
			throw new OcelotConfigStoreException($"Failed to connect to Consul at '{settings.Address}'.",
				ex);
		}
	}

	public async Task PublishAsync(
		Gateway gateway,
		string configurationJson,
		CancellationToken cancellationToken = default)
	{
		var settings = gateway.ConsulSettings
					   ?? throw new OcelotConfigStoreException(
						   "Consul configuration settings are missing.");

		using var request = new HttpRequestMessage(
			HttpMethod.Put,
			BuildKeyUrl(settings, raw: false))
		{
			Content = new StringContent(configurationJson)
		};

		AddToken(request, settings.Token);

		try
		{
			using var response = await _client.SendAsync(
				request,
				cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				throw new OcelotConfigStoreException(
					$"Consul returned HTTP {(int)response.StatusCode} " +
					$"while publishing '{settings.ConfigurationKey}'.");
			}

			var result = await response.Content.ReadAsStringAsync(
				cancellationToken);

			if (!bool.TryParse(result, out var success) || !success)
			{
				throw new OcelotConfigStoreException(
					$"Consul rejected configuration key " +
					$"'{settings.ConfigurationKey}'.");
			}
		}
		catch (OcelotConfigStoreException)
		{
			throw;
		}
		catch (HttpRequestException ex)
		{
			throw new OcelotConfigStoreException(
				$"Failed to connect to Consul at '{settings.Address}'.",
				ex);
		}
	}
	
	private static string BuildKeyUrl(
		ConsulGatewaySettings settings,
		bool raw = true)
	{
		var address = settings.Address.TrimEnd('/');

		var key = string.Join(
			"/",
			settings.ConfigurationKey
					.Trim('/')
					.Split('/')
					.Select(Uri.EscapeDataString));

		var url = $"{address}/v1/kv/{key}";

		return raw
			? $"{url}?raw"
			: url;
	}
	
	private static void AddToken(
		HttpRequestMessage request,
		string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return;
		}

		request.Headers.Add("X-Consul-Token", token);
	}
}