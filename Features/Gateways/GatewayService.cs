using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Data;
using OcelotAdmin.Domain;
using OcelotAdmin.Infrastructure.ConfigStores;

namespace OcelotAdmin.Features.Gateways;

public sealed class GatewayService
{
    private readonly OcelotAdminDbContext _dbContext;
    private readonly OcelotConfigStoreResolver  _resolver;
    public GatewayService(OcelotAdminDbContext dbContext, OcelotConfigStoreResolver resolver)
    {
        _dbContext = dbContext;
        _resolver = resolver;
    }

    public async Task<Result<Gateway>> CreateAsync(CreateGatewayRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<Gateway>.Failure("Gateway name is required.");
        }
        
        var name =  request.Name.Trim();
        
        var nameExist = await _dbContext.Gateways.AnyAsync(x => x.Name == name,cancellationToken);
        if (nameExist)
        {
            return Result<Gateway>.Failure($"A Gateway with the name '{name}' already exists.");
        }

        var validationResult = ValidateStoreSettings(request);

        if (validationResult is not null)
        {
            return Result<Gateway>.Failure(validationResult);
        }
        
        var now =  DateTime.UtcNow;
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

    public async Task<Result<string>> GetLiveConfigurationAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default
    )
    {
        var gateway = await GetByIdAsync(gatewayId, cancellationToken);
        if (gateway is null)
        {
            return Result<string>.Failure("Gateway not found.");
        }

        try
        {
            var store = _resolver.Resolve(gateway.ConfigStoreType);

            var configuration = await store.ReadAsync(gateway, cancellationToken);
            return Result<string>.Success(configuration);
        }
        catch (OcelotConfigStoreException ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
    
    private static string? ValidateStoreSettings(
        CreateGatewayRequest request)
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
}