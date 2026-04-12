namespace BioStack.Infrastructure.Persistence;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BioStack.Domain.Entities;

public sealed class BioStackDbContext : DbContext
{
    public BioStackDbContext(DbContextOptions<BioStackDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<PersonProfile> PersonProfiles { get; set; }
    public DbSet<CompoundRecord> CompoundRecords { get; set; }
    public DbSet<CheckIn> CheckIns { get; set; }
    public DbSet<ProtocolPhase> ProtocolPhases { get; set; }
    public DbSet<TimelineEvent> TimelineEvents { get; set; }
    public DbSet<InteractionFlag> InteractionFlags { get; set; }
    public DbSet<KnowledgeEntry> KnowledgeEntries { get; set; }
    public DbSet<LeadCapture> LeadCaptures { get; set; }
    public DbSet<CalculatorResultRecord> CalculatorResultRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.ProviderKey).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Provider).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(u => u.AvatarUrl).HasMaxLength(1024);
            entity.Property(u => u.Role).HasConversion<int>();
            entity.HasIndex(u => new { u.Provider, u.ProviderKey }).IsUnique();
            entity.HasIndex(u => u.Email);
            entity.HasMany(u => u.Profiles)
                .WithOne(p => p.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PersonProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(p => p.Age);
            entity.Property(p => p.DateOfBirth);
            entity.Property(p => p.GoalSummary).HasMaxLength(1000);
            entity.Property(p => p.Notes).HasMaxLength(2000);
            entity.HasMany(p => p.Compounds)
                .WithOne(c => c.PersonProfile)
                .HasForeignKey(c => c.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.CheckIns)
                .WithOne(c => c.PersonProfile)
                .HasForeignKey(c => c.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.ProtocolPhases)
                .WithOne(pp => pp.PersonProfile)
                .HasForeignKey(pp => pp.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.TimelineEvents)
                .WithOne(te => te.PersonProfile)
                .HasForeignKey(te => te.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompoundRecord>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.PersonId).IsRequired();
            entity.Property(c => c.Name).HasMaxLength(255).IsRequired();
            entity.Property(c => c.CanonicalName).HasMaxLength(255);
            entity.Property(c => c.Goal).HasMaxLength(255);
            entity.Property(c => c.Source).HasMaxLength(255);
            entity.Property(c => c.PricePaid).HasPrecision(18, 2);
            entity.Property(c => c.Notes).HasMaxLength(2000);
            entity.HasOne(c => c.PersonProfile)
                .WithMany(p => p.Compounds)
                .HasForeignKey(c => c.PersonId);
            entity.HasOne(c => c.KnowledgeEntry)
                .WithMany()
                .HasForeignKey(c => c.KnowledgeEntryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(c => c.PersonId);
            entity.HasIndex(c => c.KnowledgeEntryId);
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.PersonId).IsRequired();
            entity.Property(c => c.GiSymptoms).HasMaxLength(1000);
            entity.Property(c => c.Mood).HasMaxLength(500);
            entity.Property(c => c.Notes).HasMaxLength(1000);
            entity.HasOne(c => c.PersonProfile)
                .WithMany(p => p.CheckIns)
                .HasForeignKey(c => c.PersonId);
            entity.HasIndex(c => c.PersonId);
            entity.HasIndex(c => c.Date);
        });

        modelBuilder.Entity<ProtocolPhase>(entity =>
        {
            entity.HasKey(pp => pp.Id);
            entity.Property(pp => pp.PersonId).IsRequired();
            entity.Property(pp => pp.Name).HasMaxLength(255).IsRequired();
            entity.Property(pp => pp.Notes).HasMaxLength(2000);
            entity.HasOne(pp => pp.PersonProfile)
                .WithMany(p => p.ProtocolPhases)
                .HasForeignKey(pp => pp.PersonId);
            entity.HasIndex(pp => pp.PersonId);
        });

        modelBuilder.Entity<TimelineEvent>(entity =>
        {
            entity.HasKey(te => te.Id);
            entity.Property(te => te.PersonId).IsRequired();
            entity.Property(te => te.Title).HasMaxLength(255).IsRequired();
            entity.Property(te => te.Description).HasMaxLength(2000);
            entity.Property(te => te.RelatedEntityType).HasMaxLength(100);
            entity.HasOne(te => te.PersonProfile)
                .WithMany(p => p.TimelineEvents)
                .HasForeignKey(te => te.PersonId);
            entity.HasIndex(te => te.PersonId);
            entity.HasIndex(te => te.OccurredAtUtc);
        });

        modelBuilder.Entity<InteractionFlag>(entity =>
        {
            entity.HasKey(ifc => ifc.Id);
            entity.Property(ifc => ifc.PathwayTag).HasMaxLength(255);
            entity.Property(ifc => ifc.Description).HasMaxLength(2000);
            entity.Property(ifc => ifc.EvidenceConfidence).HasMaxLength(100);
            entity.Property(ifc => ifc.CompoundNames)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<KnowledgeEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CanonicalName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Aliases).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.Pathways).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.Benefits).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.PairsWellWith).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.AvoidWith).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.CompatibleBlends).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.WeeklyDosageSchedule).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.IncrementalEscalationSteps).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.DrugInteractions).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.OptimizationSupplements).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.SourceReferences).HasConversion(
                v => string.Join("|", v),
                v => v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.TieredDosing).HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<TieredDosingData>(v, (JsonSerializerOptions?)null));
        });

        modelBuilder.Entity<LeadCapture>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Email).HasMaxLength(255).IsRequired();
            entity.Property(l => l.Source).HasMaxLength(255).IsRequired();
            entity.HasIndex(l => new { l.Email, l.Source }).IsUnique();
        });

        modelBuilder.Entity<CalculatorResultRecord>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.PersonId).IsRequired();
            entity.Property(c => c.CalculatorKind).HasMaxLength(100).IsRequired();
            entity.Property(c => c.InputsJson).HasMaxLength(4000);
            entity.Property(c => c.OutputsJson).HasMaxLength(4000);
            entity.Property(c => c.Unit).HasMaxLength(50);
            entity.Property(c => c.Formula).HasMaxLength(1000);
            entity.Property(c => c.DisplaySummary).HasMaxLength(1000);
            entity.HasOne(c => c.PersonProfile)
                .WithMany()
                .HasForeignKey(c => c.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.CompoundRecord)
                .WithMany()
                .HasForeignKey(c => c.CompoundRecordId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(c => c.PersonId);
            entity.HasIndex(c => c.CompoundRecordId);
        });
    }
}
