using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Infrastructure;

public class WarehouseDbContext(DbContextOptions options, IDataProtectionProvider dataProtection) : DbContext(options)
{
    private readonly IDataProtector protector = dataProtection.CreateProtector("Warehouse.Credentials");

    public DbSet<MarketCredentials> MarketCredentials { get; set; }

    public DbSet<MarketDetails> MarketDetails { get; set; }

    public DbSet<WorkerDetails> WorkerDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var encryptedConverter = new EncryptedStringConverter(protector);
        modelBuilder.Entity<MarketCredentials>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd().IsRequired();
            entity.Property(x => x.ApiKey).IsRequired().HasMaxLength(500).HasConversion(encryptedConverter);
            entity.Property(x => x.SecretKey).IsRequired().HasMaxLength(500).HasConversion(encryptedConverter);
            entity.Property(x => x.Passphrase).HasMaxLength(500).HasConversion(encryptedConverter);
        });

        modelBuilder.Entity<MarketDetails>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).IsRequired();
            entity.HasIndex(x => x.Type).IsUnique();
            entity.HasOne<MarketCredentials>(x => x.Credentials)
                .WithOne(x => x.MarketDetails)
                .HasForeignKey<MarketCredentials>(x => x.MarketId);
        });

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        base.OnModelCreating(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty prop in entityType.GetProperties())
            {
                if (prop.ClrType == typeof(DateTime) || prop.ClrType == typeof(DateTime?))
                {
                    prop.SetValueConverter(dateTimeConverter);
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        OnBeforeSaving();
        int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        OnBeforeSaving();
        int result = await base.SaveChangesAsync(ct);
        return result;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSaving();
        int result = base.SaveChanges(acceptAllChangesOnSuccess);
        return result;
    }

    public override int SaveChanges()
    {
        OnBeforeSaving();
        int result = base.SaveChanges();
        return result;
    }

    private void OnBeforeSaving()
    {
        DateTime utcNow = DateTime.UtcNow;
        IEnumerable<EntityEntry> entries = ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified);
        foreach (EntityEntry entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    ProcessModified(entry, utcNow);
                    break;
                case EntityState.Added:
                    ProcessAdded(entry, utcNow);
                    break;
            }
        }

        return;

        static void ProcessModified(EntityEntry entry, DateTime dateTime)
        {
            if (entry.Entity is AuditEntity modifiedEntity)
            {
                modifiedEntity.UpdatedAt = dateTime;
            }
        }

        static void ProcessAdded(EntityEntry entry, DateTime dateTime)
        {
            if (entry.Entity is AuditEntity addedEntity)
            {
                if (addedEntity.UpdatedAt == default)
                {
                    addedEntity.UpdatedAt = dateTime;
                }

                if (addedEntity.CreatedAt == default)
                {
                    addedEntity.CreatedAt = dateTime;
                }
            }
        }
    }
}

public class EncryptedStringConverter(IDataProtector protector) : ValueConverter<string, string>(
    plainText => protector.Protect(plainText),
    cipherText => protector.Unprotect(cipherText));
