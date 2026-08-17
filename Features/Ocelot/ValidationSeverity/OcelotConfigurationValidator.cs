using System.Text.Json;
using OcelotAdmin.Domain.Ocelot;

namespace OcelotAdmin.Features.Ocelot.Validation;

public sealed class OcelotConfigurationValidator
{
	public OcelotValidationResult Validate(
		OcelotConfiguration configuration)
	{
		var result = new OcelotValidationResult();

		for (var i = 0; i < configuration.Routes.Count; i++)
		{
			ValidateRoute(
				configuration.Routes[i],
				i,
				result);
		}

		ValidateDuplicateRoutes(
			configuration,
			result);

		return result;
	}

	private static void ValidateRoute(
		OcelotRoute route,
		int routeIndex,
		OcelotValidationResult result)
	{
		ValidatePathTemplates(
			route,
			routeIndex,
			result);

		ValidateDownstream(
			route,
			routeIndex,
			result);

		ValidateHttpMethods(
			route,
			routeIndex,
			result);

		ValidateLoadBalancer(
			route,
			routeIndex,
			result);
	}
	
	private static void ValidatePathTemplates(
		OcelotRoute route,
		int routeIndex,
		OcelotValidationResult result)
	{
		if (string.IsNullOrWhiteSpace(
				route.UpstreamPathTemplate))
		{
			AddError(
				result,
				routeIndex,
				nameof(route.UpstreamPathTemplate),
				"Upstream path template is required.");
		}

		if (string.IsNullOrWhiteSpace(
				route.DownstreamPathTemplate))
		{
			AddError(
				result,
				routeIndex,
				nameof(route.DownstreamPathTemplate),
				"Downstream path template is required.");
		}

		if (!string.IsNullOrWhiteSpace(
				route.UpstreamPathTemplate) &&
			!route.UpstreamPathTemplate.StartsWith('/'))
		{
			AddWarning(
				result,
				routeIndex,
				nameof(route.UpstreamPathTemplate),
				"Upstream path template normally starts with '/'.");
		}

		if (!string.IsNullOrWhiteSpace(
				route.DownstreamPathTemplate) &&
			!route.DownstreamPathTemplate.StartsWith('/'))
		{
			AddWarning(
				result,
				routeIndex,
				nameof(route.DownstreamPathTemplate),
				"Downstream path template normally starts with '/'.");
		}
	}
	
	private static void ValidateDownstream(
		OcelotRoute route,
		int routeIndex,
		OcelotValidationResult result)
	{
		if (string.IsNullOrWhiteSpace(
				route.DownstreamScheme))
		{
			AddError(
				result,
				routeIndex,
				nameof(route.DownstreamScheme),
				"Downstream scheme is required.");
		}

		var hasServiceName =
			!string.IsNullOrWhiteSpace(route.ServiceName);

		var hasDownstreamHosts =
			HasExtensionProperty(
				route,
				"DownstreamHostAndPorts");

		if (!hasServiceName &&
			!hasDownstreamHosts)
		{
			AddError(
				result,
				routeIndex,
				"Destination",
				"Route must define either ServiceName or DownstreamHostAndPorts.");
		}
	}
	
	private static bool HasExtensionProperty(
		OcelotRoute route,
		string propertyName)
	{
		if (route.ExtensionData is null)
		{
			return false;
		}

		return route.ExtensionData.Keys.Any(
			x => string.Equals(
				x,
				propertyName,
				StringComparison.OrdinalIgnoreCase));
	}
	
	private static readonly HashSet<string> KnownHttpMethods =
		new(
			[
				"GET",
				"POST",
				"PUT",
				"PATCH",
				"DELETE",
				"OPTIONS",
				"HEAD",
				"TRACE",
				"CONNECT"
			],
			StringComparer.OrdinalIgnoreCase);
	
	private static void ValidateHttpMethods(
		OcelotRoute route,
		int routeIndex,
		OcelotValidationResult result)
	{
		foreach (var method in route.UpstreamHttpMethod)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				AddError(
					result,
					routeIndex,
					nameof(route.UpstreamHttpMethod),
					"HTTP method cannot be empty.");

				continue;
			}

			if (!KnownHttpMethods.Contains(method))
			{
				AddWarning(
					result,
					routeIndex,
					nameof(route.UpstreamHttpMethod),
					$"'{method}' is not a standard HTTP method.");
			}
		}
	}
	
	private static void ValidateLoadBalancer(
		OcelotRoute route,
		int routeIndex,
		OcelotValidationResult result)
	{
		if (route.LoadBalancerOptions is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(
				route.LoadBalancerOptions.Type))
		{
			AddError(
				result,
				routeIndex,
				"LoadBalancerOptions.Type",
				"Load balancer type is required when LoadBalancerOptions is configured.");
		}
	}
	
	private static void ValidateDuplicateRoutes(
		OcelotConfiguration configuration,
		OcelotValidationResult result)
	{
		var seen = new Dictionary<string, int>(
			StringComparer.OrdinalIgnoreCase);

		for (var i = 0; i < configuration.Routes.Count; i++)
		{
			var route = configuration.Routes[i];

			var path =
				route.UpstreamPathTemplate?.Trim()
				?? string.Empty;

			var methods = route.UpstreamHttpMethod
							   .OrderBy(x => x)
							   .Select(x => x.ToUpperInvariant());

			var key =
				$"{path}|{string.Join(",", methods)}";

			if (seen.TryGetValue(
					key,
					out var previousIndex))
			{
				AddWarning(
					result,
					i,
					nameof(route.UpstreamPathTemplate),
					$"This route has the same upstream path and HTTP methods as route #{previousIndex + 1}.");
			}
			else
			{
				seen[key] = i;
			}
		}
	}
	
	private static void AddError(
		OcelotValidationResult result,
		int? routeIndex,
		string? field,
		string message)
	{
		result.Issues.Add(
			new OcelotValidationIssue
			{
				Severity = ValidationSeverity.Error,
				RouteIndex = routeIndex,
				Field = field,
				Message = message
			});
	}

	private static void AddWarning(
		OcelotValidationResult result,
		int? routeIndex,
		string? field,
		string message)
	{
		result.Issues.Add(
			new OcelotValidationIssue
			{
				Severity = ValidationSeverity.Warning,
				RouteIndex = routeIndex,
				Field = field,
				Message = message
			});
	}
}