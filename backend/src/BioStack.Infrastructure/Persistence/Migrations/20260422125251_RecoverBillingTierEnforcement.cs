using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecoverBillingTierEnforcement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompoundInteractionHints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompoundA = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CompoundB = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    InteractionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Strength = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    MechanismOverlap = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompoundInteractionHints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InteractionFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompoundNames = table.Column<string>(type: "TEXT", nullable: false),
                    OverlapType = table.Column<int>(type: "INTEGER", nullable: false),
                    PathwayTag = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EvidenceConfidence = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractionFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CanonicalName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: false),
                    Classification = table.Column<int>(type: "INTEGER", nullable: false),
                    RegulatoryStatus = table.Column<string>(type: "TEXT", nullable: false),
                    MechanismSummary = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceTier = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceReferences = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Pathways = table.Column<string>(type: "TEXT", nullable: false),
                    Benefits = table.Column<string>(type: "TEXT", nullable: false),
                    PairsWellWith = table.Column<string>(type: "TEXT", nullable: false),
                    AvoidWith = table.Column<string>(type: "TEXT", nullable: false),
                    CompatibleBlends = table.Column<string>(type: "TEXT", nullable: false),
                    VialCompatibility = table.Column<string>(type: "TEXT", nullable: false),
                    RecommendedDosage = table.Column<string>(type: "TEXT", nullable: false),
                    StandardDosageRange = table.Column<string>(type: "TEXT", nullable: false),
                    MaxReportedDose = table.Column<string>(type: "TEXT", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredTimeOfDay = table.Column<string>(type: "TEXT", nullable: false),
                    WeeklyDosageSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    IncrementalEscalationSteps = table.Column<string>(type: "TEXT", nullable: false),
                    TieredDosing = table.Column<string>(type: "TEXT", nullable: true),
                    DrugInteractions = table.Column<string>(type: "TEXT", nullable: false),
                    OptimizationProtein = table.Column<string>(type: "TEXT", nullable: false),
                    OptimizationCarbs = table.Column<string>(type: "TEXT", nullable: false),
                    OptimizationSupplements = table.Column<string>(type: "TEXT", nullable: false),
                    OptimizationSleep = table.Column<string>(type: "TEXT", nullable: false),
                    OptimizationExercise = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StripeEventId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ValueNormalized = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthIdentities_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Sex = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    GoalSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonProfiles_AppUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Tier = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StripePriceId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdentityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChallengeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RedirectPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthChallenges_AuthIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "AuthIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompoundRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Goal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PricePaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompoundRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompoundRecords_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolPhases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolPhases_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    SleepQuality = table.Column<int>(type: "INTEGER", nullable: false),
                    Energy = table.Column<int>(type: "INTEGER", nullable: false),
                    Appetite = table.Column<int>(type: "INTEGER", nullable: false),
                    Recovery = table.Column<int>(type: "INTEGER", nullable: false),
                    Focus = table.Column<int>(type: "INTEGER", nullable: true),
                    ThoughtClarity = table.Column<int>(type: "INTEGER", nullable: true),
                    SkinQuality = table.Column<int>(type: "INTEGER", nullable: true),
                    DigestiveHealth = table.Column<int>(type: "INTEGER", nullable: true),
                    Strength = table.Column<int>(type: "INTEGER", nullable: true),
                    Endurance = table.Column<int>(type: "INTEGER", nullable: true),
                    JointPain = table.Column<int>(type: "INTEGER", nullable: true),
                    Eyesight = table.Column<int>(type: "INTEGER", nullable: true),
                    SideEffects = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false),
                    GiSymptoms = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Mood = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckIns_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolComputationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InputSnapshot = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    OutputResult = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolComputationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompoundRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalculatorResultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CompoundNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CompoundCategorySnapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompoundStartDateSnapshot = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompoundEndDateSnapshot = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompoundStatusSnapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompoundNotesSnapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CompoundGoalSnapshot = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CompoundSourceSnapshot = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CompoundPricePaidSnapshot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolItems_CompoundRecords_CompoundRecordId",
                        column: x => x.CompoundRecordId,
                        principalTable: "CompoundRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolReviewCompletedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolReviewCompletedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProtocolId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolRuns_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ParentProtocolId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginProtocolId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EvolvedFromRunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    EvolutionContext = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Protocols_PersonProfiles_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Protocols_ProtocolRuns_EvolvedFromRunId",
                        column: x => x.EvolvedFromRunId,
                        principalTable: "ProtocolRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Protocols_Protocols_OriginProtocolId",
                        column: x => x.OriginProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Protocols_Protocols_ParentProtocolId",
                        column: x => x.ParentProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Provider_ProviderKey",
                table: "AppUsers",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_StripeCustomerId",
                table: "AppUsers",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthChallenges_ExpiresAtUtc",
                table: "AuthChallenges",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthChallenges_IdentityId",
                table: "AuthChallenges",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthChallenges_TokenHash",
                table: "AuthChallenges",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthIdentities_Type_ValueNormalized",
                table: "AuthIdentities",
                columns: new[] { "Type", "ValueNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthIdentities_UserId",
                table: "AuthIdentities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_Date",
                table: "CheckIns",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_PersonId",
                table: "CheckIns",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ProtocolRunId",
                table: "CheckIns",
                column: "ProtocolRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CompoundInteractionHints_CompoundA_CompoundB",
                table: "CompoundInteractionHints",
                columns: new[] { "CompoundA", "CompoundB" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompoundRecords_PersonId",
                table: "CompoundRecords",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_Email_Source",
                table: "LeadCaptures",
                columns: new[] { "Email", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonProfiles_OwnerId",
                table: "PersonProfiles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolComputationRecords_ProtocolId",
                table: "ProtocolComputationRecords",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolComputationRecords_ProtocolRunId",
                table: "ProtocolComputationRecords",
                column: "ProtocolRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolComputationRecords_TimestampUtc",
                table: "ProtocolComputationRecords",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolItems_CompoundRecordId",
                table: "ProtocolItems",
                column: "CompoundRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolItems_ProtocolId",
                table: "ProtocolItems",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolPhases_PersonId",
                table: "ProtocolPhases",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolReviewCompletedEvents_CompletedAtUtc",
                table: "ProtocolReviewCompletedEvents",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolReviewCompletedEvents_ProtocolId",
                table: "ProtocolReviewCompletedEvents",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolReviewCompletedEvents_ProtocolRunId",
                table: "ProtocolReviewCompletedEvents",
                column: "ProtocolRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolRuns_PersonId",
                table: "ProtocolRuns",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolRuns_PersonId_Status",
                table: "ProtocolRuns",
                columns: new[] { "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolRuns_ProtocolId",
                table: "ProtocolRuns",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_EvolvedFromRunId",
                table: "Protocols",
                column: "EvolvedFromRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_OriginProtocolId",
                table: "Protocols",
                column: "OriginProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_ParentProtocolId",
                table: "Protocols",
                column: "ParentProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_PersonId",
                table: "Protocols",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_PersonId_OriginProtocolId_Version",
                table: "Protocols",
                columns: new[] { "PersonId", "OriginProtocolId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAtUtc",
                table: "Sessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TokenHash",
                table: "Sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvents_StripeEventId",
                table: "StripeWebhookEvents",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_AppUserId",
                table: "Subscriptions",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeCustomerId",
                table: "Subscriptions",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeSubscriptionId",
                table: "Subscriptions",
                column: "StripeSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_OccurredAtUtc",
                table: "TimelineEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_PersonId",
                table: "TimelineEvents",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_ProtocolRuns_ProtocolRunId",
                table: "CheckIns",
                column: "ProtocolRunId",
                principalTable: "ProtocolRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolComputationRecords_ProtocolRuns_ProtocolRunId",
                table: "ProtocolComputationRecords",
                column: "ProtocolRunId",
                principalTable: "ProtocolRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolComputationRecords_Protocols_ProtocolId",
                table: "ProtocolComputationRecords",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolItems_Protocols_ProtocolId",
                table: "ProtocolItems",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolReviewCompletedEvents_ProtocolRuns_ProtocolRunId",
                table: "ProtocolReviewCompletedEvents",
                column: "ProtocolRunId",
                principalTable: "ProtocolRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolReviewCompletedEvents_Protocols_ProtocolId",
                table: "ProtocolReviewCompletedEvents",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolRuns_Protocols_ProtocolId",
                table: "ProtocolRuns",
                column: "ProtocolId",
                principalTable: "Protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonProfiles_AppUsers_OwnerId",
                table: "PersonProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProtocolRuns_PersonProfiles_PersonId",
                table: "ProtocolRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Protocols_PersonProfiles_PersonId",
                table: "Protocols");

            migrationBuilder.DropForeignKey(
                name: "FK_Protocols_ProtocolRuns_EvolvedFromRunId",
                table: "Protocols");

            migrationBuilder.DropTable(
                name: "AuthChallenges");

            migrationBuilder.DropTable(
                name: "CheckIns");

            migrationBuilder.DropTable(
                name: "CompoundInteractionHints");

            migrationBuilder.DropTable(
                name: "InteractionFlags");

            migrationBuilder.DropTable(
                name: "KnowledgeEntries");

            migrationBuilder.DropTable(
                name: "LeadCaptures");

            migrationBuilder.DropTable(
                name: "ProtocolComputationRecords");

            migrationBuilder.DropTable(
                name: "ProtocolItems");

            migrationBuilder.DropTable(
                name: "ProtocolPhases");

            migrationBuilder.DropTable(
                name: "ProtocolReviewCompletedEvents");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "StripeWebhookEvents");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "AuthIdentities");

            migrationBuilder.DropTable(
                name: "CompoundRecords");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "PersonProfiles");

            migrationBuilder.DropTable(
                name: "ProtocolRuns");

            migrationBuilder.DropTable(
                name: "Protocols");
        }
    }
}
