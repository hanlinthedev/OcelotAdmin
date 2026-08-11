using Microsoft.EntityFrameworkCore;
using OcelotAdmin.Domain;

namespace OcelotAdmin.Data;

public sealed class OcelotAdminDbContext : DbContext
{
    public OcelotAdminDbContext(DbContextOptions<OcelotAdminDbContext> options) : base (options){}
    
    public DbSet<Gateway> Gateways => Set<Gateway>();
    
    public DbSet<FileGatewaySettings> FileGatewaySettings => Set<FileGatewaySettings>();
    
    public DbSet<ConsulGatewaySettings>  ConsulGatewaySettings => Set<ConsulGatewaySettings>();
    
    public DbSet<GatewayDraft>  GatewayDrafts => Set<GatewayDraft>();
    
    public DbSet<ConfigurationHistory>  ConfigurationHistory => Set<ConfigurationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureGateway(modelBuilder);
    }

    private static void ConfigureGateway(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gateway>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.ConfigStoreType)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasOne(x => x.FileSettings)
                .WithOne(x => x.Gateway)
                .HasForeignKey<FileGatewaySettings>(x => x.GatewayId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ConsulSettings)
                .WithOne(x => x.Gateway)
                .HasForeignKey<ConsulGatewaySettings>(x => x.GatewayId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Draft)
                .WithOne(x => x.Gateway)
                .HasForeignKey<GatewayDraft>(x => x.GatewayId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.ConfigurationHistory)
                .WithOne(x => x.Gateway)
                .HasForeignKey(x => x.GatewayId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<FileGatewaySettings>(entity =>
        {
            entity.HasKey(x => x.GatewayId);

            entity.Property(x => x.ConfigurationPath)
                .IsRequired()
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<ConsulGatewaySettings>(entity =>
        {
            entity.HasKey(x => x.GatewayId);

            entity.Property(x => x.Address)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.ConfigurationKey)
                .IsRequired()
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<GatewayDraft>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.GatewayId)
                .IsUnique();

            entity.Property(x => x.ConfigurationJson)
                .IsRequired();
        });

        modelBuilder.Entity<ConfigurationHistory>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.GatewayId,
                x.PublishedAt
            });
        });
    }
}