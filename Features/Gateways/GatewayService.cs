using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Data;
using OcelotAdmin.Domain;
using OcelotAdmin.Features.Ocelot.Validation;
using OcelotAdmin.Infrastructure.ConfigStores;
using OcelotAdmin.Services;

namespace OcelotAdmin.Features.Gateways;

public sealed class GatewayService
{
	private readonly OcelotAdminDbContext _dbContext;
	private readonly OcelotConfigStoreResolver _resolver;
	private readonly OcelotConfigStoreResolver _configStoreResolver;
	private readonly OcelotConfigurationSerializer _serializer;
	private readonly OcelotConfigurationValidator _validator;

	public GatewayService(
		OcelotAdminDbContext dbContext,
		OcelotConfigStoreResolver configStoreResolver,
		OcelotConfigurationSerializer serializer,
		OcelotConfigurationValidator validator
	)
	{
		_dbContext = dbContext;
		_configStoreResolver = configStoreResolver;
		_serializer = serializer;
		_validator = validator;
	}

	public async Task<Result<Gateway>> CreateAsync(
		CreateGatewayRequest request,
		CancellationToken cancellationToken = default
	)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
		{
			return Result<Gateway>.Failure("Gateway name is required.");
		}

		var name = request.Name.Trim();

		var nameExist = await _dbContext.Gateways.AnyAsync(x=>x.Name == name, cancellationToken);
		if (nameExist)
		{
			return Result<Gateway>.Failure($"A Gateway with the name '{name}' already exists.");
		}

		var validationResult = ValidateStoreSettings(request);

		if (validationResult is not null)
		{
			return Result<Gateway>.Failure(validationResult);
		}

		var now = DateTime.UtcNow;
		var gateway = new Gateway
		{
			Id = Guid.NewGuid(),
			Name = request.Name,
			Description = request.Description?.Trim(),
			ConfigStoreType = request.ConfigStoreType,
			CreatedAt = now,
			UpdatedAt = now
		};

		switch (request.ConfigStoreType)
		{
			case ConfigStoreType.File:
				gateway.FileSettings = new FileGatewaySettings
				{
					GatewayId = gateway.Id,
					ConfigurationPath =
						request.File!.ConfigurationPath.Trim()
				};
				break;

			case ConfigStoreType.Consul:
				gateway.ConsulSettings = new ConsulGatewaySettings
				{
					GatewayId = gateway.Id,
					Address = request.Consul!.Address.Trim(),
					ConfigurationKey =
						request.Consul.ConfigurationKey.Trim(),
					Token = string.IsNullOrWhiteSpace(request.Consul.Token)
						? null
						: request.Consul.Token.Trim()
				};
				break;
		}

		_dbContext.Gateways.Add(gateway);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return Result<Gateway>.Success(gateway);
	}

	public async Task<List<Gateway>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var gateways = await _dbContext.Gateways
									   .AsNoTracking()
									   .OrderBy(x=>x.Name)
									   .ToListAsync(cancellationToken);
		return gateways;
	}

	public async Task<Gateway?> GetByIdAsync(Guid gatewayId, CancellationToken cancellationToken = default)
	{
		var gateway = await _dbContext.Gateways
									  .AsNoTracking()
									  .Include(x=>x.FileSettings)
									  .Include(x=>x.ConsulSettings)
									  .FirstOrDefaultAsync(x=>x.Id == gatewayId, cancellationToken);
		return gateway;
	}


	private static string? ValidateStoreSettings(
		CreateGatewayRequest request
	)
	{
		switch (request.ConfigStoreType)
		{
			case ConfigStoreType.File:
				if (request.File is null)
				{
					return "File configuration settings are required.";
				}

				if (string.IsNullOrWhiteSpace(
						request.File.ConfigurationPath))
				{
					return "Configuration file path is required.";
				}

				return null;

			case ConfigStoreType.Consul:
				if (request.Consul is null)
				{
					return "Consul configuration settings are required.";
				}

				if (string.IsNullOrWhiteSpace(request.Consul.Address))
				{
					return "Consul address is required.";
				}

				if (string.IsNullOrWhiteSpace(
						request.Consul.ConfigurationKey))
				{
					return "Consul configuration key is required.";
				}

				return null;

			default:
				return "Unsupported configuration store type.";
		}
	}

	public async Task<Result<GatewayConnectionResult>>
		TestConnectionAsync(
			Guid gatewayId,
			CancellationToken cancellationToken = default
		)
	{
		var gateway = await _dbContext.Gateways
									  .AsNoTracking()
									  .Include(x=>x.FileSettings)
									  .Include(x=>x.ConsulSettings)
									  .FirstOrDefaultAsync(
										  x=>x.Id == gatewayId,
										  cancellationToken);

		if (gateway is null)
		{
			return Result<GatewayConnectionResult>.Failure(
				"Gateway was not found.");
		}

		var store =
			_configStoreResolver.Resolve(
				gateway.ConfigStoreType);

		var health =
			await store.CheckHealthAsync(
				gateway,
				cancellationToken);

		if (!health.IsReadable)
		{
			return Result<GatewayConnectionResult>.Success(
				new GatewayConnectionResult
				{
					IsConnected = health.IsReachable,
					ConfigExists = health.KeyExists,
					ConfigReadable = health.IsReadable,
					ValidJson = health.ConfigurationIsValid,
					Message = health.Message
				});
		}

		try
		{
			var readResult =
				await store.ReadAsync(
					gateway,
					cancellationToken);

			var json =
				readResult.ConfigurationJson;

			var configuration =
				_serializer.Deserialize(json);

			var validation =
				_validator.Validate(configuration);

			return Result<GatewayConnectionResult>.Success(
				new GatewayConnectionResult
				{
					IsConnected = true,
					ConfigExists = true,
					ConfigReadable = true,
					ValidJson = true,
					ValidOcelotConfiguration =
						validation.IsValid,
					RouteCount =
						configuration.Routes.Count,
					Message =
						validation.IsValid
							? "Gateway configuration is accessible and valid."
							: $"Configuration contains " +
							  $"{validation.ErrorCount} validation error(s)."
				});
		}
		catch (Exception ex)
			when (ex is
					  System.Text.Json.JsonException or
					  InvalidOperationException or
					  OcelotConfigStoreException)
		{
			return Result<GatewayConnectionResult>.Success(
				new GatewayConnectionResult
				{
					IsConnected = true,
					ConfigExists = true,
					ConfigReadable = true,
					Message = ex.Message
				});
		}
	}

	public async Task<Result<Gateway>> UpdateAsync(
    Guid gatewayId,
    UpdateGatewayRequest request,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Result<Gateway>.Failure(
            "Gateway name is required.");
    }

    var duplicateNameExists =
        await _dbContext.Gateways
            .AnyAsync(
                x =>
                    x.Id != gatewayId &&
                    x.Name == request.Name.Trim(),
                cancellationToken);

    if (duplicateNameExists)
    {
        return Result<Gateway>.Failure(
            "A gateway with this name already exists.");
    }

    var gateway =
        await _dbContext.Gateways
            .Include(x => x.FileSettings)
            .Include(x => x.ConsulSettings)
            .Include(x => x.Draft)
            .FirstOrDefaultAsync(
                x => x.Id == gatewayId,
                cancellationToken);

    if (gateway is null)
    {
        return Result<Gateway>.Failure(
            "Gateway was not found.");
    }

    var previousStoreType =
        gateway.ConfigStoreType;

    gateway.Name =
        request.Name.Trim();

    gateway.Description =
        string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

    gateway.ConfigStoreType =
        request.ConfigStoreType;

    gateway.UpdatedAt =
        DateTime.UtcNow;

    if (request.ConfigStoreType == ConfigStoreType.File)
    {
        if (request.File is null ||
            string.IsNullOrWhiteSpace(
                request.File.ConfigurationPath))
        {
            return Result<Gateway>.Failure(
                "Configuration path is required.");
        }

        if (gateway.FileSettings is null)
        {
            gateway.FileSettings =
                new FileGatewaySettings
                {
                    GatewayId = gateway.Id,
                    Gateway = gateway
                };
        }

        gateway.FileSettings.ConfigurationPath =
            request.File.ConfigurationPath.Trim();

        if (gateway.ConsulSettings is not null)
        {
            _dbContext.ConsulGatewaySettings.Remove(
                gateway.ConsulSettings);

            gateway.ConsulSettings = null;
        }
    }
    else if (request.ConfigStoreType == ConfigStoreType.Consul)
    {
        if (request.Consul is null)
        {
            return Result<Gateway>.Failure(
                "Consul settings are required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Consul.Address))
        {
            return Result<Gateway>.Failure(
                "Consul address is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Consul.ConfigurationKey))
        {
            return Result<Gateway>.Failure(
                "Consul configuration key is required.");
        }

        if (gateway.ConsulSettings is null)
        {
            gateway.ConsulSettings =
                new ConsulGatewaySettings
                {
                    GatewayId = gateway.Id,
                    Gateway = gateway
                };
        }

        gateway.ConsulSettings.Address =
            request.Consul.Address.Trim();

        gateway.ConsulSettings.ConfigurationKey =
            request.Consul.ConfigurationKey.Trim();

        gateway.ConsulSettings.Token =
            string.IsNullOrWhiteSpace(
                request.Consul.Token)
                ? null
                : request.Consul.Token.Trim();

        if (gateway.FileSettings is not null)
        {
            _dbContext.FileGatewaySettings.Remove(
                gateway.FileSettings);

            gateway.FileSettings = null;
        }
    }
    else
    {
        return Result<Gateway>.Failure(
            "Unsupported configuration store type.");
    }

   
    if (previousStoreType != request.ConfigStoreType &&
        gateway.Draft is not null)
    {
        _dbContext.GatewayDrafts.Remove(
            gateway.Draft);

        gateway.Draft = null;
    }

    await _dbContext.SaveChangesAsync(
        cancellationToken);

    return Result<Gateway>.Success(
        gateway);
}

	public async Task<Result<bool>> DeleteAsync(
		Guid gatewayId,
		CancellationToken cancellationToken = default
	)
	{
		var gateway =
			await _dbContext.Gateways
							.FirstOrDefaultAsync(
								x=>x.Id == gatewayId,
								cancellationToken);

		if (gateway is null)
		{
			return Result<bool>.Failure(
				"Gateway was not found.");
		}

		_dbContext.Gateways.Remove(gateway);

		await _dbContext.SaveChangesAsync(
			cancellationToken);

		return Result<bool>.Success(true);
	}
}