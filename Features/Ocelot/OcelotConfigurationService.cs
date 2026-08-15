using System.Text.Json;
using OcelotAdmin.Data;
using OcelotAdmin.Domain.Ocelot;
using OcelotAdmin.Features;
using OcelotAdmin.Infrastructure.ConfigStores;
using OcelotAdmin.Services;
using Microsoft.EntityFrameworkCore;

namespace OcelotAdmin.Features.Ocelot;

public sealed class OcelotConfigurationService
{
    private readonly OcelotAdminDbContext _dbContext;
    private readonly OcelotConfigStoreResolver _storeResolver;
    private readonly OcelotConfigurationSerializer _serializer;

    public OcelotConfigurationService(
        OcelotAdminDbContext dbContext,
        OcelotConfigStoreResolver storeResolver,
        OcelotConfigurationSerializer serializer)
    {
        _dbContext = dbContext;
        _storeResolver = storeResolver;
        _serializer = serializer;
    }

    public async Task<Result<OcelotConfiguration>> GetLiveAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var gateway = await _dbContext.Gateways
            .AsNoTracking()
            .Include(x => x.FileSettings)
            .Include(x => x.ConsulSettings)
            .FirstOrDefaultAsync(
                x => x.Id == gatewayId,
                cancellationToken);

        if (gateway is null)
        {
            return Result<OcelotConfiguration>.Failure(
                "Gateway was not found.");
        }

        try
        {
            var store = _storeResolver.Resolve(
                gateway.ConfigStoreType);

            var json = await store.ReadAsync(
                gateway,
                cancellationToken);

            var configuration =
                _serializer.Deserialize(json);

            return Result<OcelotConfiguration>.Success(
                configuration);
        }
        catch (OcelotConfigStoreException ex)
        {
            return Result<OcelotConfiguration>.Failure(
                ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<OcelotConfiguration>.Failure(
                $"The gateway configuration contains invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return Result<OcelotConfiguration>.Failure(
                ex.Message);
        }
    }
}