using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Components;
using OcelotAdmin.Data;
using OcelotAdmin.Features.Gateways;
using OcelotAdmin.Features.Ocelot;
using OcelotAdmin.Infrastructure.ConfigStores;
using OcelotAdmin.Infrastructure.ConfigStores.Consul;
using OcelotAdmin.Infrastructure.ConfigStores.File;
using OcelotAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
