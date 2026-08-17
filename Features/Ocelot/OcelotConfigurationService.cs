using System.Text.Json;
using OcelotAdmin.Data;
using OcelotAdmin.Domain.Ocelot;
using OcelotAdmin.Features;
using OcelotAdmin.Infrastructure.ConfigStores;
using OcelotAdmin.Services;
using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Domain;
using OcelotAdmin.Features.Ocelot.Diff;
using OcelotAdmin.Features.Ocelot.Publish;
using OcelotAdmin.Features.Ocelot.Validation;

namespace OcelotAdmin.Features.Ocelot;

public sealed class OcelotConfigurationService
{
    private readonly OcelotAdminDbContext _dbContext;
    private readonly OcelotConfigStoreResolver _storeResolver;
    private readonly OcelotConfigurationSerializer _serializer;
    private readonly OcelotConfigurationValidator _validator;
    private readonly OcelotConfigurationDiffService _diffService;

    public OcelotConfigurationService(
        OcelotAdminDbContext dbContext,
        OcelotConfigStoreResolver storeResolver,
        OcelotConfigurationSerializer serializer,
        OcelotConfigurationValidator validator,
        OcelotConfigurationDiffService diffService)
    {
        _dbContext = dbContext;
        _storeResolver = storeResolver;
        _serializer = serializer;
        _validator = validator;
        _diffService = diffService;
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

            var readResult =
                await store.ReadAsync(
                    gateway,
                    cancellationToken);

            var configuration =
                _serializer.Deserialize(
                    readResult.ConfigurationJson);
            
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
    
    private async Task<Result<LiveOcelotConfiguration>>
        GetLiveWithVersionAsync(
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
            return Result<LiveOcelotConfiguration>.Failure(
                "Gateway was not found.");
        }

        try
        {
            var store =
                _storeResolver.Resolve(
                    gateway.ConfigStoreType);

            var readResult =
                await store.ReadAsync(
                    gateway,
                    cancellationToken);

            var configuration =
                _serializer.Deserialize(
                    readResult.ConfigurationJson);

            return Result<LiveOcelotConfiguration>.Success(
                new LiveOcelotConfiguration
                {
                    Configuration = configuration,
                    Version = readResult.Version
                });
        }
        catch (OcelotConfigStoreException ex)
        {
            return Result<LiveOcelotConfiguration>.Failure(
                ex.Message);
        }
        catch (JsonException ex)
        {
            return Result<LiveOcelotConfiguration>.Failure(
                $"Invalid JSON: {ex.Message}");
        }
    }
    
    public async Task<Result<OcelotConfiguration>> GetDraftAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.GatewayDrafts
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(
                                        x => x.GatewayId == gatewayId,
                                        cancellationToken);

        if (draft is null)
        {
            return Result<OcelotConfiguration>.Failure(
                "No draft exists for this gateway.");
        }

        try
        {
            var configuration =
                _serializer.Deserialize(draft.ConfigurationJson);

            return Result<OcelotConfiguration>.Success(configuration);
        }
        catch (JsonException ex)
        {
            return Result<OcelotConfiguration>.Failure(
                $"The draft contains invalid JSON: {ex.Message}");
        }
    }
    
    public async Task<Result<OcelotConfiguration>> GetOrCreateDraftAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var existingDraft = await _dbContext.GatewayDrafts
                                            .FirstOrDefaultAsync(
                                                x => x.GatewayId == gatewayId,
                                                cancellationToken);

        if (existingDraft is not null)
        {
            try
            {
                var configuration =
                    _serializer.Deserialize(
                        existingDraft.ConfigurationJson);

                return Result<OcelotConfiguration>.Success(
                    configuration);
            }
            catch (JsonException ex)
            {
                return Result<OcelotConfiguration>.Failure(
                    $"The existing draft contains invalid JSON: {ex.Message}");
            }
        }

        var liveResult =   await GetLiveWithVersionAsync(
            gatewayId,
            cancellationToken);

        if (!liveResult.IsSuccess ||
            liveResult.Value is null)
        {
            return Result<OcelotConfiguration>.Failure(
                liveResult.Error ??
                "Failed to load live configuration.");
        }

        var live =
            liveResult.Value;

        var now =
            DateTime.UtcNow;

        var draft =
            new GatewayDraft
            {
                Id = Guid.NewGuid(),

                GatewayId = gatewayId,

                ConfigurationJson =
                    _serializer.Serialize(
                        live.Configuration),

                SourceVersion =
                    live.Version,

                CreatedAt = now,
                UpdatedAt = now
            };

        _dbContext.GatewayDrafts.Add(draft);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Result<OcelotConfiguration>.Success(
            live.Configuration);
    }
    
    public async Task<Result<OcelotConfiguration>> SaveDraftAsync(
        Guid gatewayId,
        OcelotConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.GatewayDrafts
                                    .FirstOrDefaultAsync(
                                        x => x.GatewayId == gatewayId,
                                        cancellationToken);

        if (draft is null)
        {
            return Result<OcelotConfiguration>.Failure(
                "No draft exists for this gateway.");
        }

        draft.ConfigurationJson =
            _serializer.Serialize(configuration);

        draft.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<OcelotConfiguration>.Success(configuration);
    }
    
    public async Task<Result<bool>> DiscardDraftAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.GatewayDrafts
                                    .FirstOrDefaultAsync(
                                        x => x.GatewayId == gatewayId,
                                        cancellationToken);

        if (draft is null)
        {
            return Result<bool>.Failure(
                "No draft exists for this gateway.");
        }

        _dbContext.GatewayDrafts.Remove(draft);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
    
    public Task<bool> HasDraftAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.GatewayDrafts
                         .AnyAsync(
                             x => x.GatewayId == gatewayId,
                             cancellationToken);
    }
    
    public async Task<Result<int>> AddRouteAsync(
        Guid gatewayId,
        OcelotRoute route,
        CancellationToken cancellationToken = default)
    {
        var draftResult = await GetOrCreateDraftAsync(
            gatewayId,
            cancellationToken);

        if (!draftResult.IsSuccess || draftResult.Value is null)
        {
            return Result<int>.Failure(
                draftResult.Error ?? "Failed to load gateway draft.");
        }

        var configuration = draftResult.Value;

        configuration.Routes.Add(route);

        var saveResult = await SaveDraftAsync(
            gatewayId,
            configuration,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return Result<int>.Failure(
                saveResult.Error ?? "Failed to save gateway draft.");
        }

        return Result<int>.Success(
            configuration.Routes.Count - 1);
    }
    
    public async Task<Result<bool>> DeleteRouteAsync(
        Guid gatewayId,
        int routeIndex,
        CancellationToken cancellationToken = default)
    {
        var draftResult = await GetOrCreateDraftAsync(
            gatewayId,
            cancellationToken);

        if (!draftResult.IsSuccess || draftResult.Value is null)
        {
            return Result<bool>.Failure(
                draftResult.Error ?? "Failed to load gateway draft.");
        }

        var configuration = draftResult.Value;

        if (routeIndex < 0 ||
            routeIndex >= configuration.Routes.Count)
        {
            return Result<bool>.Failure(
                "Route was not found.");
        }

        configuration.Routes.RemoveAt(routeIndex);

        var saveResult = await SaveDraftAsync(
            gatewayId,
            configuration,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(
                saveResult.Error ?? "Failed to save gateway draft.");
        }

        return Result<bool>.Success(true);
    }
    
    public async Task<Result<int>> DuplicateRouteAsync(
        Guid gatewayId,
        int routeIndex,
        CancellationToken cancellationToken = default)
    {
        var draftResult = await GetOrCreateDraftAsync(
            gatewayId,
            cancellationToken);

        if (!draftResult.IsSuccess || draftResult.Value is null)
        {
            return Result<int>.Failure(
                draftResult.Error ?? "Failed to load gateway draft.");
        }

        var configuration = draftResult.Value;

        if (routeIndex < 0 ||
            routeIndex >= configuration.Routes.Count)
        {
            return Result<int>.Failure(
                "Route was not found.");
        }

        var source = configuration.Routes[routeIndex];

        var json = _serializer.SerializeRoute(source);
        var duplicate = _serializer.DeserializeRoute(json);

        configuration.Routes.Insert(
            routeIndex + 1,
            duplicate);

        var saveResult = await SaveDraftAsync(
            gatewayId,
            configuration,
            cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return Result<int>.Failure(
                saveResult.Error ?? "Failed to save gateway draft.");
        }

        return Result<int>.Success(routeIndex + 1);
    }
    
    public async Task<Result<string>> GetDraftJsonAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.GatewayDrafts
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(
                                        x => x.GatewayId == gatewayId,
                                        cancellationToken);

        if (draft is null)
        {
            return Result<string>.Failure(
                "No draft exists for this gateway.");
        }

        return Result<string>.Success(
            draft.ConfigurationJson);
    }
    
    public async Task<Result<bool>> SaveDraftJsonAsync(
        Guid gatewayId,
        string configurationJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return Result<bool>.Failure(
                "Configuration JSON cannot be empty.");
        }

        try
        {
            _serializer.Deserialize(configurationJson);
        }
        catch (JsonException ex)
        {
            return Result<bool>.Failure(
                $"Invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        var draft = await _dbContext.GatewayDrafts
                                    .FirstOrDefaultAsync(
                                        x => x.GatewayId == gatewayId,
                                        cancellationToken);

        if (draft is null)
        {
            return Result<bool>.Failure(
                "No draft exists for this gateway.");
        }

        draft.ConfigurationJson = configurationJson;
        draft.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Result<bool>.Success(true);
    }
    
    public Result<string> FormatJson(string json)
    {
        try
        {
            return Result<string>.Success(
                _serializer.Format(json));
        }
        catch (JsonException ex)
        {
            return Result<string>.Failure(
                $"Invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return Result<string>.Failure(
                ex.Message);
        }
    }
    
    public async Task<Result<OcelotValidationResult>> ValidateDraftAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        var draftResult = await GetDraftAsync(
            gatewayId,
            cancellationToken);

        if (!draftResult.IsSuccess ||
            draftResult.Value is null)
        {
            return Result<OcelotValidationResult>.Failure(
                draftResult.Error ??
                "Failed to load gateway draft.");
        }

        var validation =
            _validator.Validate(draftResult.Value);

        return Result<OcelotValidationResult>.Success(
            validation);
    }
    
    public async Task<Result<OcelotPublishPreview>> GetPublishPreviewAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
{
    var draft = await _dbContext.GatewayDrafts
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.GatewayId == gatewayId,
            cancellationToken);

    if (draft is null)
    {
        return Result<OcelotPublishPreview>.Failure(
            "No draft exists for this gateway.");
    }

    OcelotConfiguration draftConfiguration;

    try
    {
        draftConfiguration =
            _serializer.Deserialize(
                draft.ConfigurationJson);
    }
    catch (Exception ex)
        when (ex is JsonException or InvalidOperationException)
    {
        return Result<OcelotPublishPreview>.Failure(
            $"Draft configuration is invalid: {ex.Message}");
    }

    var liveResult = await GetLiveWithVersionAsync(
        gatewayId,
        cancellationToken);

    if (!liveResult.IsSuccess ||
        liveResult.Value is null)
    {
        return Result<OcelotPublishPreview>.Failure(
            liveResult.Error ??
            "Failed to load live configuration.");
    }

    var live = liveResult.Value;

    var validation =
        _validator.Validate(
            draftConfiguration);

    var diff =
        _diffService.Compare(
            live.Configuration,
            draftConfiguration);

    var hasConcurrencyConflict =
        !string.IsNullOrWhiteSpace(draft.SourceVersion) &&
        !string.IsNullOrWhiteSpace(live.Version) &&
        !string.Equals(
            draft.SourceVersion,
            live.Version,
            StringComparison.Ordinal);

    return Result<OcelotPublishPreview>.Success(
        new OcelotPublishPreview
        {
            Validation = validation,
            Diff = diff,

            HasConcurrencyConflict =
                hasConcurrencyConflict,

            DraftSourceVersion =
                draft.SourceVersion,

            CurrentVersion =
                live.Version
        });
}
    
    public async Task<Result<bool>> PublishDraftAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
{
    var gateway = await _dbContext.Gateways
        .Include(x => x.FileSettings)
        .Include(x => x.ConsulSettings)
        .FirstOrDefaultAsync(
            x => x.Id == gatewayId,
            cancellationToken);

    if (gateway is null)
    {
        return Result<bool>.Failure(
            "Gateway was not found.");
    }

    var draft = await _dbContext.GatewayDrafts
        .FirstOrDefaultAsync(
            x => x.GatewayId == gatewayId,
            cancellationToken);

    if (draft is null)
    {
        return Result<bool>.Failure(
            "No draft exists for this gateway.");
    }

    OcelotConfiguration draftConfiguration;

    try
    {
        draftConfiguration =
            _serializer.Deserialize(
                draft.ConfigurationJson);
    }
    catch (Exception ex)
        when (ex is JsonException or InvalidOperationException)
    {
        return Result<bool>.Failure(
            $"Draft configuration is invalid: {ex.Message}");
    }

    var validation =
        _validator.Validate(
            draftConfiguration);

    if (!validation.IsValid)
    {
        return Result<bool>.Failure(
            $"Draft contains {validation.ErrorCount} validation error(s).");
    }

    var store =
        _storeResolver.Resolve(
            gateway.ConfigStoreType);

    ConfigStoreReadResult liveRead;

    try
    {
        liveRead = await store.ReadAsync(
            gateway,
            cancellationToken);
    }
    catch (OcelotConfigStoreException ex)
    {
        return Result<bool>.Failure(
            ex.Message);
    }

    if (!string.IsNullOrWhiteSpace(draft.SourceVersion) &&
        !string.IsNullOrWhiteSpace(liveRead.Version) &&
        !string.Equals(
            draft.SourceVersion,
            liveRead.Version,
            StringComparison.Ordinal))
    {
        return Result<bool>.Failure(
            "The live gateway configuration changed after this draft " +
            "was created. Review the latest configuration before publishing.");
    }

    OcelotConfiguration liveConfiguration;

    try
    {
        liveConfiguration =
            _serializer.Deserialize(
                liveRead.ConfigurationJson);
    }
    catch (Exception ex)
        when (ex is JsonException or InvalidOperationException)
    {
        return Result<bool>.Failure(
            $"Live configuration is invalid: {ex.Message}");
    }

    var diff =
        _diffService.Compare(
            liveConfiguration,
            draftConfiguration);

    if (!diff.HasChanges)
    {
        return Result<bool>.Failure(
            "Draft contains no changes.");
    }

    var history = new ConfigurationHistory
    {
        Id = Guid.NewGuid(),
        GatewayId = gatewayId,

        ConfigurationJson =
            liveRead.ConfigurationJson,

        PublishedAt =
            DateTime.UtcNow
    };

    _dbContext.ConfigurationHistory.Add(
        history);

    try
    {
        await store.PublishAsync(
            gateway,
            draft.ConfigurationJson,
            draft.SourceVersion,
            cancellationToken);
    }
    catch (OcelotConfigConflictException)
    {
        return Result<bool>.Failure(
            "The live gateway configuration changed while publishing. " +
            "The newer configuration was not overwritten.");
    }
    catch (OcelotConfigStoreException ex)
    {
        return Result<bool>.Failure(
            ex.Message);
    }
    
    ConfigStoreReadResult publishedRead;

    try
    {
        publishedRead = await store.ReadAsync(
            gateway,
            cancellationToken);
    }
    catch (OcelotConfigStoreException ex)
    {
        return Result<bool>.Failure(
            "Configuration was published, but verification failed: " +
            ex.Message);
    }

    OcelotConfiguration publishedConfiguration;

    try
    {
        publishedConfiguration =
            _serializer.Deserialize(
                publishedRead.ConfigurationJson);
    }
    catch (Exception ex)
        when (ex is JsonException or InvalidOperationException)
    {
        return Result<bool>.Failure(
            "Configuration was published, but the stored result " +
            $"could not be parsed: {ex.Message}");
    }

    var verificationDiff =
        _diffService.Compare(
            draftConfiguration,
            publishedConfiguration);

    if (verificationDiff.HasChanges)
    {
        return Result<bool>.Failure(
            "Configuration was published, but verification detected " +
            "a difference between the draft and stored configuration.");
    }
    
    _dbContext.GatewayDrafts.Remove(
        draft);

    await _dbContext.SaveChangesAsync(
        cancellationToken);

    return Result<bool>.Success(true);
}
    
    public async Task<List<ConfigurationHistory>> GetHistoryAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConfigurationHistory
                               .AsNoTracking()
                               .Where(x => x.GatewayId == gatewayId)
                               .OrderByDescending(x => x.PublishedAt)
                               .ToListAsync(cancellationToken);
    }
    
    public async Task<ConfigurationHistory?> GetHistoryByIdAsync(
        Guid gatewayId,
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConfigurationHistory
                               .AsNoTracking()
                               .FirstOrDefaultAsync(
                                   x =>
                                       x.Id == historyId &&
                                       x.GatewayId == gatewayId,
                                   cancellationToken);
    }
    
    public async Task<Result<bool>> RestoreHistoryAsDraftAsync(
    Guid gatewayId,
    Guid historyId,
    CancellationToken cancellationToken = default)
{
    var gatewayExists =
        await _dbContext.Gateways
            .AnyAsync(
                x => x.Id == gatewayId,
                cancellationToken);

    if (!gatewayExists)
    {
        return Result<bool>.Failure(
            "Gateway was not found.");
    }

    var history =
        await _dbContext.ConfigurationHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id == historyId &&
                    x.GatewayId == gatewayId,
                cancellationToken);

    if (history is null)
    {
        return Result<bool>.Failure(
            "Configuration history was not found.");
    }

    try
    {
        _serializer.Deserialize(
            history.ConfigurationJson);
    }
    catch (Exception ex)
        when (ex is JsonException or InvalidOperationException)
    {
        return Result<bool>.Failure(
            $"Historical configuration is invalid: {ex.Message}");
    }
    
    var liveResult =
        await GetLiveWithVersionAsync(
            gatewayId,
            cancellationToken);

    if (!liveResult.IsSuccess ||
        liveResult.Value is null)
    {
        return Result<bool>.Failure(
            liveResult.Error ??
            "Failed to read the current live configuration.");
    }

    var currentSourceVersion =
        liveResult.Value.Version;

    var existingDraft =
        await _dbContext.GatewayDrafts
            .FirstOrDefaultAsync(
                x => x.GatewayId == gatewayId,
                cancellationToken);

    var now =
        DateTime.UtcNow;

    if (existingDraft is null)
    {
        existingDraft =
            new GatewayDraft
            {
                Id =
                    Guid.NewGuid(),

                GatewayId =
                    gatewayId,

                ConfigurationJson =
                    history.ConfigurationJson,

                SourceVersion =
                    currentSourceVersion,

                CreatedAt =
                    now,

                UpdatedAt =
                    now
            };

        _dbContext.GatewayDrafts.Add(
            existingDraft);
    }
    else
    {
        existingDraft.ConfigurationJson =
            history.ConfigurationJson;

        existingDraft.SourceVersion =
            currentSourceVersion;

        existingDraft.UpdatedAt =
            now;
    }

    await _dbContext.SaveChangesAsync(
        cancellationToken);

    return Result<bool>.Success(true);
}
}