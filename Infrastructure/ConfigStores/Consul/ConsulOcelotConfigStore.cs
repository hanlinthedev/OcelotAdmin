using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using OcelotAdmin.Domain;

namespace OcelotAdmin.Infrastructure.ConfigStores.Consul;

public sealed class ConsulOcelotConfigStore : IOcelotConfigStore
{
    private readonly HttpClient _httpClient;

    public ConsulOcelotConfigStore(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public ConfigStoreType Type =>
        ConfigStoreType.Consul;


    public async Task<ConfigStoreReadResult> ReadAsync(
    Gateway gateway,
    CancellationToken cancellationToken = default)
{
    var settings = GetSettings(gateway);

    using var request = new HttpRequestMessage(
        HttpMethod.Get,
        BuildKeyUrl(settings, false));

    AddToken(request, settings.Token);

    try
    {
        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OcelotConfigStoreException(
                $"Consul key '{settings.ConfigurationKey}' was not found.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new OcelotConfigStoreException(
                "Consul denied access to the configuration key. " +
                "Check the ACL token and key:read permission.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new OcelotConfigStoreException(
                $"Consul returned HTTP {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}) while reading " +
                $"'{settings.ConfigurationKey}'.");
        }

        var entries =
            await response.Content
                .ReadFromJsonAsync<List<ConsulKvEntry>>(
                    cancellationToken);

        var entry = entries?.SingleOrDefault();

        if (entry is null)
        {
            throw new OcelotConfigStoreException(
                $"Consul key '{settings.ConfigurationKey}' returned no value.");
        }

        if (string.IsNullOrWhiteSpace(entry.Value))
        {
            throw new OcelotConfigStoreException(
                $"Consul key '{settings.ConfigurationKey}' is empty.");
        }

        string json;

        try
        {
            var bytes =
                Convert.FromBase64String(entry.Value);

            json = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new OcelotConfigStoreException(
                $"Consul key '{settings.ConfigurationKey}' " +
                "contains an invalid encoded value.",
                ex);
        }

        return new ConfigStoreReadResult
        {
            ConfigurationJson = json,
            Version = entry.ModifyIndex.ToString()
        };
    }
    catch (OcelotConfigStoreException)
    {
        throw;
    }
    catch (TaskCanceledException ex)
        when (!cancellationToken.IsCancellationRequested)
    {
        throw new OcelotConfigStoreException(
            $"Connection to Consul at '{settings.Address}' timed out.",
            ex);
    }
    catch (HttpRequestException ex)
    {
        throw new OcelotConfigStoreException(
            $"Failed to connect to Consul at '{settings.Address}'.",
            ex);
    }
}

public async Task PublishAsync(
    Gateway gateway,
    string configurationJson,
    string? expectedVersion = null,
    CancellationToken cancellationToken = default)
{
    var settings = GetSettings(gateway);

    var url =
        BuildKeyUrl(settings,false);

    if (!string.IsNullOrWhiteSpace(expectedVersion))
    {
        if (!ulong.TryParse(
                expectedVersion,
                out var modifyIndex))
        {
            throw new OcelotConfigStoreException(
                "The expected Consul configuration version is invalid.");
        }

        url =
            $"{url}?cas={modifyIndex}";
    }

    using var request = new HttpRequestMessage(
        HttpMethod.Put,
        url)
    {
        Content = new StringContent(
            configurationJson,
            Encoding.UTF8,
            "application/json")
    };

    AddToken(request, settings.Token);

    try
    {
        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new OcelotConfigStoreException(
                "Consul denied write access to the configuration key. " +
                "Check the ACL token and key:write permission.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new OcelotConfigStoreException(
                $"Consul returned HTTP {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}) while publishing " +
                $"'{settings.ConfigurationKey}'.");
        }

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!bool.TryParse(
                responseBody,
                out var succeeded))
        {
            throw new OcelotConfigStoreException(
                "Consul returned an unexpected response " +
                "while publishing configuration.");
        }

        if (!succeeded)
        {
            throw new OcelotConfigConflictException(
                "The live Consul configuration changed after " +
                "this draft was created.");
        }
    }
    catch (OcelotConfigStoreException)
    {
        throw;
    }
    catch (TaskCanceledException ex)
        when (!cancellationToken.IsCancellationRequested)
    {
        throw new OcelotConfigStoreException(
            $"Connection to Consul at '{settings.Address}' timed out.",
            ex);
    }
    catch (HttpRequestException ex)
    {
        throw new OcelotConfigStoreException(
            $"Failed to connect to Consul at '{settings.Address}'.",
            ex);
    }
}
  

    public async Task<ConfigStoreHealthResult> CheckHealthAsync(
        Gateway gateway,
        CancellationToken cancellationToken = default)
    {
        var settings = GetSettings(gateway);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildKeyUrl(settings, raw: true));

        AddToken(request, settings.Token);

        try
        {
            using var response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ConfigStoreHealthResult
                {
                    IsReachable = true,
                    KeyExists = false,
                    IsReadable = false,
                    ConfigurationIsValid = false,
                    Message =
                        $"Consul is reachable, but key " +
                        $"'{settings.ConfigurationKey}' does not exist."
                };
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ConfigStoreHealthResult
                {
                    IsReachable = true,
                    KeyExists = false,
                    IsReadable = false,
                    ConfigurationIsValid = false,
                    Message =
                        "Consul is reachable, but access was denied. " +
                        "Check the ACL token."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ConfigStoreHealthResult
                {
                    IsReachable = true,
                    Message =
                        $"Consul responded with HTTP " +
                        $"{(int)response.StatusCode} " +
                        $"({response.ReasonPhrase})."
                };
            }

            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            var validJson = IsJson(json);

            return new ConfigStoreHealthResult
            {
                IsReachable = true,
                KeyExists = true,
                IsReadable = true,
                ConfigurationIsValid = validJson,
                Message = validJson
                    ? "Consul configuration is accessible."
                    : "The Consul key exists but does not contain valid JSON."
            };
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return new ConfigStoreHealthResult
            {
                Message =
                    $"Connection to Consul at " +
                    $"'{settings.Address}' timed out."
            };
        }
        catch (HttpRequestException ex)
        {
            return new ConfigStoreHealthResult
            {
                Message =
                    $"Unable to connect to Consul: {ex.Message}"
            };
        }
    }


    private static ConsulGatewaySettings GetSettings(
        Gateway gateway)
    {
        return gateway.ConsulSettings
            ?? throw new OcelotConfigStoreException(
                "Consul configuration settings are missing.");
    }


    private static string BuildKeyUrl(
        ConsulGatewaySettings settings,
        bool raw)
    {
        var address =
            settings.Address.TrimEnd('/');

        var key = string.Join(
            "/",
            settings.ConfigurationKey
                .Trim('/')
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

        var url =
            $"{address}/v1/kv/{key}";

        return raw
            ? $"{url}?raw=true"
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

        request.Headers.Add(
            "X-Consul-Token",
            token);
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