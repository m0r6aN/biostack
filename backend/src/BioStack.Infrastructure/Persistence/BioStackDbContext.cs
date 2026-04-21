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
    public DbSet<AuthIdentity> AuthIdentities { get; set; }
    public DbSet<AuthChallenge> AuthChallenges { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<PersonProfile> PersonProfiles { get; set; }
    public DbSet<CompoundRecord> CompoundRecords { get; set; }
    public DbSet<CheckIn> CheckIns { get; set; }
    public DbSet<Protocol> Protocols { get; set; }
    public DbSet<ProtocolRun> ProtocolRuns { get; set; }
    public DbSet<ProtocolItem> ProtocolItems { get; set; }
    public DbSet<ProtocolComputationRecord> ProtocolComputationRecords { get; set; }
    public DbSet<ProtocolReviewCompletedEvent> ProtocolReviewCompletedEvents { get; set; }
    public DbSet<ProtocolPhase> ProtocolPhases { get; set; }
    public DbSet<TimelineEvent> TimelineEvents { get; set; }
    public DbSet<InteractionFlag> InteractionFlags { get; set; }
    public DbSet<CompoundInteractionHint> CompoundInteractionHints { get; set; }
    public DbSet<KnowledgeEntry> KnowledgeEntries { get; set; }
    public DbSet<LeadCapture> LeadCaptures { get; set; }

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
            entity.HasMany(u => u.AuthIdentities)
                .WithOne(i => i.User)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthIdentity>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Type).HasMaxLength(50).IsRequired();
            entity.Property(i => i.ValueNormalized).HasMaxLength(255).IsRequired();
            entity.HasIndex(i => new { i.Type, i.ValueNormalized }).IsUnique();
            entity.HasIndex(i => i.UserId);
        });

        modelBuilder.Entity<AuthChallenge>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Channel).HasMaxLength(50).IsRequired();
            entity.Property(c => c.ChallengeType).HasMaxLength(50).IsRequired();
            entity.Property(c => c.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(c => c.IpAddress).HasMaxLength(128);
            entity.Property(c => c.RedirectPath).HasMaxLength(512).IsRequired();
            entity.HasOne(c => c.Identity)
                .WithMany(i => i.Challenges)
                .HasForeignKey(c => c.IdentityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => c.TokenHash).IsUnique();
            entity.HasIndex(c => c.IdentityId);
            entity.HasIndex(c => c.ExpiresAtUtc);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(s => s.IpAddress).HasMaxLength(128);
            entity.Property(s => s.UserAgent).HasMaxLength(512);
            entity.HasIndex(s => s.TokenHash).IsUnique();
            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.ExpiresAtUtc);
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
            entity.HasMany(p => p.Protocols)
                .WithOne(protocol => protocol.PersonProfile)
                .HasForeignKey(protocol => protocol.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.ProtocolRuns)
                .WithOne(run => run.PersonProfile)
                .HasForeignKey(run => run.PersonId)
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
            entity.Property(c => c.Goal).HasMaxLength(255);
            entity.Property(c => c.Source).HasMaxLength(255);
            entity.Property(c => c.PricePaid).HasPrecision(18, 2);
            entity.Property(c => c.Notes).HasMaxLength(2000);
            entity.HasOne(c => c.PersonProfile)
                .WithMany(p => p.Compounds)
                .HasForeignKey(c => c.PersonId);
            entity.HasIndex(c => c.PersonId);
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
            entity.HasOne(c => c.ProtocolRun)
                .WithMany(run => run.CheckIns)
                .HasForeignKey(c => c.ProtocolRunId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(c => c.PersonId);
            entity.HasIndex(c => c.ProtocolRunId);
            entity.HasIndex(c => c.Date);
        });

        modelBuilder.Entity<Protocol>(entity =>
        {
            entity.HasKey(protocol => protocol.Id);
            entity.Property(protocol => protocol.PersonId).IsRequired();
            entity.Property(protocol => protocol.Name).HasMaxLength(255).IsRequired();
            entity.Property(protocol => protocol.Version).HasDefaultValue(1);
            entity.Property(protocol => protocol.EvolutionContext).HasMaxLength(4000);
            entity.HasOne(protocol => protocol.PersonProfile)
                .WithMany(profile => profile.Protocols)
                .HasForeignKey(protocol => protocol.PersonId);
            entity.HasOne(protocol => protocol.ParentProtocol)
                .WithMany(parent => parent.ChildVersions)
                .HasForeignKey(protocol => protocol.ParentProtocolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(protocol => protocol.OriginProtocol)
                .WithMany()
                .HasForeignKey(protocol => protocol.OriginProtocolId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(protocol => protocol.EvolvedFromRun)
                .WithMany()
                .HasForeignKey(protocol => protocol.EvolvedFromRunId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(protocol => protocol.Items)
                .WithOne(item => item.Protocol)
                .HasForeignKey(item => item.ProtocolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(protocol => protocol.Runs)
                .WithOne(run => run.Protocol)
                .HasForeignKey(run => run.ProtocolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany<ProtocolComputationRecord>()
                .WithOne(record => record.Protocol)
                .HasForeignKey(record => record.ProtocolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany<ProtocolReviewCompletedEvent>()
                .WithOne(@event => @event.Protocol)
                .HasForeignKey(@event => @event.ProtocolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(protocol => protocol.PersonId);
            entity.HasIndex(protocol => protocol.ParentProtocolId);
            entity.HasIndex(protocol => protocol.OriginProtocolId);
            entity.HasIndex(protocol => protocol.EvolvedFromRunId);
            entity.HasIndex(protocol => new { protocol.PersonId, protocol.OriginProtocolId, protocol.Version });
        });

        modelBuilder.Entity<ProtocolRun>(entity =>
        {
            entity.HasKey(run => run.Id);
            entity.Property(run => run.ProtocolId).IsRequired();
            entity.Property(run => run.PersonId).IsRequired();
            entity.Property(run => run.Status).HasConversion<int>();
            entity.Property(run => run.Notes).HasMaxLength(2000);
            entity.HasIndex(run => run.PersonId);
            entity.HasIndex(run => run.ProtocolId);
            entity.HasIndex(run => new { run.PersonId, run.Status });
        });

        modelBuilder.Entity<ProtocolComputationRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.ProtocolId).IsRequired();
            entity.Property(record => record.Type).HasMaxLength(100).IsRequired();
            entity.Property(record => record.InputSnapshot).HasMaxLength(4000);
            entity.Property(record => record.OutputResult).HasMaxLength(4000);
            entity.HasOne(record => record.ProtocolRun)
                .WithMany()
                .HasForeignKey(record => record.ProtocolRunId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(record => record.ProtocolId);
            entity.HasIndex(record => record.ProtocolRunId);
            entity.HasIndex(record => record.TimestampUtc);
        });

        modelBuilder.Entity<ProtocolReviewCompletedEvent>(entity =>
        {
            entity.HasKey(@event => @event.Id);
            entity.Property(@event => @event.ProtocolId).IsRequired();
            entity.Property(@event => @event.Notes).HasMaxLength(2000);
            entity.HasOne(@event => @event.ProtocolRun)
                .WithMany()
                .HasForeignKey(@event => @event.ProtocolRunId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(@event => @event.ProtocolId);
            entity.HasIndex(@event => @event.ProtocolRunId);
            entity.HasIndex(@event => @event.CompletedAtUtc);
        });

        modelBuilder.Entity<ProtocolItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProtocolId).IsRequired();
            entity.Property(item => item.CompoundRecordId).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(2000);
            entity.Property(item => item.CompoundNameSnapshot).HasMaxLength(255);
            entity.Property(item => item.CompoundCategorySnapshot).HasMaxLength(100);
            entity.Property(item => item.CompoundStatusSnapshot).HasMaxLength(100);
            entity.Property(item => item.CompoundNotesSnapshot).HasMaxLength(2000);
            entity.Property(item => item.CompoundGoalSnapshot).HasMaxLength(255);
            entity.Property(item => item.CompoundSourceSnapshot).HasMaxLength(255);
            entity.Property(item => item.CompoundPricePaidSnapshot).HasPrecision(18, 2);
            entity.HasOne(item => item.CompoundRecord)
                .WithMany()
                .HasForeignKey(item => item.CompoundRecordId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => item.ProtocolId);
            entity.HasIndex(item => item.CompoundRecordId);
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

        modelBuilder.Entity<CompoundInteractionHint>(entity =>
        {
            entity.HasKey(hint => hint.Id);
            entity.Property(hint => hint.CompoundA).HasMaxLength(255).IsRequired();
            entity.Property(hint => hint.CompoundB).HasMaxLength(255).IsRequired();
            entity.Property(hint => hint.InteractionType).HasConversion<int>();
            entity.Property(hint => hint.Strength).HasPrecision(3, 2);
            entity.Property(hint => hint.Notes).HasMaxLength(2000);
            entity.Property(hint => hint.MechanismOverlap).HasConversion(
                v => v == null ? null : string.Join("|", v),
                v => string.IsNullOrWhiteSpace(v)
                    ? null
                    : v.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.HasIndex(hint => new { hint.CompoundA, hint.CompoundB }).IsUnique();
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
    }
}
