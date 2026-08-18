using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Components;
using OcelotAdmin.Data;
using OcelotAdmin.Features.Gateways;
using OcelotAdmin.Features.Ocelot;
using OcelotAdmin.Features.Ocelot.Diff;
using OcelotAdmin.Features.Ocelot.Validation;
using OcelotAdmin.Infrastructure.ConfigStores;
using OcelotAdmin.Infrastructure.ConfigStores.Consul;
using OcelotAdmin.Infrastructure.ConfigStores.File;
using OcelotAdmin.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var keysFolder = builder.Environment.IsProduction() 
    ? new DirectoryInfo("/app/volume/keys")
    : new DirectoryInfo(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "OcelotAdmin", 
        "keys"));

builder.Services
       .AddDataProtection()
       .PersistKeysToFileSystem(keysFolder)
       .SetApplicationName("OcelotAdmin");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<OcelotAdminDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<GatewayService>();
builder.Services.AddHttpClient<ConsulOcelotConfigStore>();
builder.Services.AddScoped<IOcelotConfigStore, FileOcelotConfigStore>();
builder.Services.AddScoped<IOcelotConfigStore>(provider => provider.GetRequiredService<ConsulOcelotConfigStore>());
builder.Services.AddScoped<OcelotConfigStoreResolver>();
builder.Services.AddSingleton<OcelotConfigurationSerializer>();
builder.Services.AddScoped<OcelotConfigurationService>();
builder.Services.AddSingleton<OcelotConfigurationValidator>();
builder.Services.AddSingleton<OcelotConfigurationDiffService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();


var volumePath = Path.Combine(
    app.Environment.ContentRootPath,
    "volume");

Directory.CreateDirectory(volumePath);

using (var scope = app.Services.CreateScope())
{
    var logger =
        scope.ServiceProvider
             .GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext =
            scope.ServiceProvider
                 .GetRequiredService<OcelotAdminDbContext>();

        logger.LogInformation(
            "Applying database migrations.");

        await dbContext.Database.MigrateAsync();

        logger.LogInformation(
            "Database migrations completed.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "Database migration failed.");

        throw;
    }
}

app.Run();
