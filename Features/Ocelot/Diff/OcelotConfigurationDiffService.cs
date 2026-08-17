using System.Text.Json;
using OcelotAdmin.Domain.Ocelot;
using OcelotAdmin.Services;

namespace OcelotAdmin.Features.Ocelot.Diff;

public sealed class OcelotConfigurationDiffService
{
    private readonly OcelotConfigurationSerializer _serializer;

    public OcelotConfigurationDiffService(
        OcelotConfigurationSerializer serializer)
    {
        _serializer = serializer;
    }

    public OcelotConfigurationDiff Compare(
        OcelotConfiguration live,
        OcelotConfiguration draft)
    {
        var liveRoutes = live.Routes
            .Select((route, index) => new
            {
                Index = index,
                Key = BuildRouteKey(route),
                Json = NormalizeRoute(route)
            })
            .ToList();

        var draftRoutes = draft.Routes
            .Select((route, index) => new
            {
                Index = index,
                Key = BuildRouteKey(route),
                Json = NormalizeRoute(route)
            })
            .ToList();

        var liveLookup = liveRoutes
            .GroupBy(x => x.Key)
            .ToDictionary(
                x => x.Key,
                x => x.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var draftLookup = draftRoutes
            .GroupBy(x => x.Key)
            .ToDictionary(
                x => x.Key,
                x => x.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var removed = 0;
        var modified = 0;

        var allKeys = liveLookup.Keys
            .Union(
                draftLookup.Keys,
                StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            liveLookup.TryGetValue(
                key,
                out var liveMatches);

            draftLookup.TryGetValue(
                key,
                out var draftMatches);

            liveMatches ??= [];
            draftMatches ??= [];

            var commonCount = Math.Min(
                liveMatches.Count,
                draftMatches.Count);

            for (var i = 0; i < commonCount; i++)
            {
                if (!string.Equals(
                        liveMatches[i].Json,
                        draftMatches[i].Json,
                        StringComparison.Ordinal))
                {
                    modified++;
                }
            }

            if (draftMatches.Count > liveMatches.Count)
            {
                added +=
                    draftMatches.Count -
                    liveMatches.Count;
            }

            if (liveMatches.Count > draftMatches.Count)
            {
                removed +=
                    liveMatches.Count -
                    draftMatches.Count;
            }
        }

        return new OcelotConfigurationDiff
        {
            AddedRoutes = added,
            RemovedRoutes = removed,
            ModifiedRoutes = modified,
            GlobalConfigurationChanged =
                !JsonEquals(
                    live.GlobalConfiguration,
                    draft.GlobalConfiguration)
        };
    }

    private string NormalizeRoute(
        OcelotRoute route)
    {
        using var document =
            JsonDocument.Parse(
                _serializer.SerializeRoute(route));

        return JsonSerializer.Serialize(
            document.RootElement);
    }

    private static string BuildRouteKey(
        OcelotRoute route)
    {
        var methods = route.UpstreamHttpMethod
            .OrderBy(x => x)
            .Select(x => x.ToUpperInvariant());

        return
            $"{route.UpstreamPathTemplate?.Trim()}|" +
            $"{string.Join(",", methods)}";
    }

    private static bool JsonEquals<T>(
        T? left,
        T? right)
    {
        var leftJson =
            JsonSerializer.Serialize(left);

        var rightJson =
            JsonSerializer.Serialize(right);

        return string.Equals(
            leftJson,
            rightJson,
            StringComparison.Ordinal);
    }
}