namespace BioStack.KnowledgeWorker.Tests;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class SourceAcquisitionRuntimeTests
{
    private const string RegistrySha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string RequestSha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string DecisionSha = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    [Fact]
    public async Task Exact_campaign_runs_serially_emits_manual_receipts_and_resumes_without_calls()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var calls = new ConcurrentQueue<string>();
        var adapters = CreateAdapters((intent, _) =>
        {
            calls.Enqueue($"{intent.RequestId}:{intent.SourceId}");
            return Task.FromResult(EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var runner = new SourceAcquisitionRunner();

        var first = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-001"));

        Assert.True(first.Manifest.Complete);
        Assert.Equal(70, first.Manifest.UniqueRequestCount);
        Assert.Equal(490, first.Manifest.IntentCount);
        Assert.Equal(420, first.Manifest.NoMatchCount);
        Assert.Equal(70, first.Manifest.ManualReviewPendingCount);
        Assert.Equal(420, calls.Count);
        Assert.Equal(
            490,
            Directory.EnumerateFiles(
                first.OutputDirectory,
                "*.json",
                SearchOption.AllDirectories)
                .Count(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}attempts{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)));

        var second = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-001"));

        Assert.True(second.Manifest.Complete);
        Assert.Equal(420, calls.Count);
        var queue = JsonNode.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    second.OutputDirectory,
                    "source-acquisition-review-queue.json")))!;
        Assert.Equal(490, queue["items"]!.AsArray().Count);
    }

    [Theory]
    [InlineData(SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(SourceAcquisitionBatchStatus.BackPressure)]
    public async Task Throttle_halts_six_api_lanes_without_retry_and_manual_lane_still_receives_receipts(
        SourceAcquisitionBatchStatus throttleStatus)
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var callCount = 0;
        var adapters = CreateAdapters((intent, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(
                intent.SourceId == "fda"
                    ? new SourceAcquisitionBatch(
                        throttleStatus,
                        [],
                        false,
                        "120")
                    : EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });

        var run = await new SourceAcquisitionRunner().RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-rate-limited"));

        Assert.False(run.Manifest.Complete);
        Assert.Equal(1, callCount);
        Assert.Equal(
            throttleStatus == SourceAcquisitionBatchStatus.RateLimited ? 1 : 0,
            run.Manifest.RateLimitedCount);
        Assert.Equal(
            throttleStatus == SourceAcquisitionBatchStatus.BackPressure ? 1 : 0,
            run.Manifest.BackPressureCount);
        Assert.Equal(419, run.Manifest.NotAttemptedCount);
        Assert.Equal(70, run.Manifest.ManualReviewPendingCount);
        Assert.Equal(0, run.Manifest.ErrorCount);
    }

    [Fact]
    public async Task Candidates_are_ordered_and_checkpoint_integrity_is_fail_closed()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        const string quarantinedCandidateContent =
            "quarantined-candidate-content-must-not-enter-metadata";
        var callCount = 0;
        var adapters = CreateAdapters((intent, _) =>
        {
            Interlocked.Increment(ref callCount);
            if (intent.SourceId == "fda" && intent.RequestId == "request-000")
            {
                var candidateTwo = Candidate(intent, "2") with
                {
                    Fields = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["identity"] = [quarantinedCandidateContent],
                    },
                };
                return Task.FromResult(new SourceAcquisitionBatch(
                    SourceAcquisitionBatchStatus.Completed,
                    [candidateTwo, Candidate(intent, "1")],
                    false,
                    null));
            }
            return Task.FromResult(EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var config = Configuration(temp.Path, "cycle-integrity");
        var clock = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 26, 1, 0, 0, TimeSpan.Zero));
        var runner = new SourceAcquisitionRunner(clock);
        var run = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config);

        var intent = campaign.Plan.Intents.Single(item =>
            item.RequestId == "request-000" && item.SourceId == "fda");
        var intentId = SourceAcquisitionRunner.ComputeIntentId(
            config.CycleId,
            Bindings(),
            intent);
        var attemptDirectory = Path.Combine(
            run.OutputDirectory,
            "intents",
            intentId,
            "attempts");
        var attemptPath = Assert.Single(
            Directory.EnumerateFiles(
                attemptDirectory,
                "*.json",
                SearchOption.TopDirectoryOnly));
        var attempt = JsonNode.Parse(await File.ReadAllTextAsync(attemptPath))!;
        Assert.Equal("1", attempt["candidates"]![0]!["sourceItemId"]!.GetValue<string>());
        Assert.Equal("2", attempt["candidates"]![1]!["sourceItemId"]!.GetValue<string>());

        var checkpointPath = Path.Combine(
            run.OutputDirectory,
            "intents",
            intentId,
            "checkpoint.json");
        var checkpoint = JsonNode.Parse(await File.ReadAllTextAsync(checkpointPath))!;
        checkpoint["attemptSha256"] = new string('0', 64);
        await File.WriteAllTextAsync(
            checkpointPath,
            checkpoint.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var callsBeforeResume = callCount;

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config));
        Assert.Equal(callsBeforeResume, callCount);
        Assert.False(File.Exists(attemptPath));
        Assert.False(File.Exists(checkpointPath));

        var quarantineRoot = Path.Combine(
            run.OutputDirectory,
            "quarantine",
            intentId);
        var metadataPath = Assert.Single(
            Directory.EnumerateFiles(
                quarantineRoot,
                "quarantine-metadata.json",
                SearchOption.AllDirectories));
        var metadata = await File.ReadAllTextAsync(metadataPath);
        Assert.DoesNotContain(
            quarantinedCandidateContent,
            metadata,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"reasonCode\": \"integrity-validation-failed\"",
            metadata,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"artifactDisposition\": \"content-free-evidence-only\"",
            metadata,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            quarantinedCandidateContent,
            string.Join(
                "\n",
                Directory.EnumerateFiles(
                        quarantineRoot,
                        "*",
                        SearchOption.AllDirectories)
                    .Select(File.ReadAllText)),
            StringComparison.Ordinal);
        Assert.All(
            Directory.EnumerateFiles(
                quarantineRoot,
                "*",
                SearchOption.AllDirectories),
            path => Assert.Equal(
                "quarantine-metadata.json",
                Path.GetFileName(path)));

        clock.Advance(TimeSpan.FromDays(config.CandidateRetentionDays + 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config));
        Assert.Equal(callsBeforeResume, callCount);
        Assert.DoesNotContain(
            quarantinedCandidateContent,
            string.Join(
                "\n",
                Directory.EnumerateFiles(
                        quarantineRoot,
                        "*",
                        SearchOption.AllDirectories)
                    .Select(File.ReadAllText)),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Orphan_checkpoint_is_quarantined_and_fails_before_transport()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var callCount = 0;
        var adapters = CreateAdapters((_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult(EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var config = Configuration(temp.Path, "cycle-orphan-checkpoint");
        var runner = new SourceAcquisitionRunner();
        var first = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config);
        var firstEntry = campaign.Preflight.Entries
            .OrderBy(entry => entry.StableOrdinal)
            .First();
        var firstIntent = campaign.Plan.Intents.Single(intent =>
            intent.RequestId == firstEntry.RequestId
            && intent.SourceId == firstEntry.SourceId);
        var intentId = SourceAcquisitionRunner.ComputeIntentId(
            config.CycleId,
            Bindings(),
            firstIntent);
        var attemptPath = Assert.Single(
            Directory.EnumerateFiles(
                Path.Combine(
                    first.OutputDirectory,
                    "intents",
                    intentId,
                    "attempts"),
                "*.json",
                SearchOption.TopDirectoryOnly));
        var checkpointPath = Path.Combine(
            first.OutputDirectory,
            "intents",
            intentId,
            "checkpoint.json");
        Assert.True(File.Exists(checkpointPath));
        File.Delete(attemptPath);
        var callsBeforeResume = callCount;

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config));

        Assert.Equal(callsBeforeResume, callCount);
        Assert.False(File.Exists(checkpointPath));
        Assert.Single(
            Directory.EnumerateFiles(
                Path.Combine(first.OutputDirectory, "quarantine", intentId),
                "quarantine-metadata.json",
                SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Source_error_is_bounded_generic_does_not_leak_message_and_makes_run_incomplete()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        const string secret = "raw-body-and-contact-secret";
        var adapters = CreateAdapters((_, _) =>
            throw new SourceAcquisitionException(
                "malformed-json",
                $"{secret}\n{new string('x', 4096)}"));

        var run = await new SourceAcquisitionRunner().RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-error"));

        Assert.False(run.Manifest.Complete);
        Assert.Equal(1, run.Manifest.ErrorCount);
        Assert.Equal(419, run.Manifest.NotAttemptedCount);
        var allJson = string.Join(
            "\n",
            Directory.EnumerateFiles(
                    run.OutputDirectory,
                    "*.json",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));
        Assert.DoesNotContain(secret, allJson, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceAcquisitionRuntimeTests", allJson, StringComparison.Ordinal);
        Assert.Contains(
            "The source adapter reported a bounded acquisition error.",
            allJson,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_api_candidate_without_governed_persistence_metadata_fails_closed()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var calls = 0;
        var adapters = CreateAdapters((intent, _) =>
        {
            Interlocked.Increment(ref calls);
            var legacyCandidate = Candidate(intent, "legacy") with
            {
                AuthorizedFieldUses = [],
                RightsAttributions = [],
                ReuseBoundary = SourceReuseBoundary.Unspecified,
            };
            return Task.FromResult(new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.Completed,
                [legacyCandidate],
                false,
                null));
        });

        var run = await new SourceAcquisitionRunner().RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-legacy-api-candidate"));

        Assert.False(run.Manifest.Complete);
        Assert.Equal(1, calls);
        Assert.Equal(1, run.Manifest.ErrorCount);
        Assert.Equal(419, run.Manifest.NotAttemptedCount);
        var persistedJson = string.Join(
            "\n",
            Directory.EnumerateFiles(
                    run.OutputDirectory,
                    "*.json",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));
        Assert.Contains(
            "candidate-persistence-invariant-invalid",
            persistedJson,
            StringComparison.Ordinal);
        Assert.DoesNotContain("\"sourceItemId\": \"legacy\"", persistedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Truncated_batch_is_incomplete_halts_transport_and_preserves_status_after_expiry()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var calls = 0;
        var adapters = CreateAdapters((intent, _) =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.Completed,
                [Candidate(intent, "truncated")],
                Truncated: true,
                RetryAfter: null));
        });
        var clock = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 26, 1, 0, 0, TimeSpan.Zero));
        var runner = new SourceAcquisitionRunner(clock);
        var configuration = Configuration(temp.Path, "cycle-truncated");

        var first = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            configuration);

        Assert.False(first.Manifest.Complete);
        Assert.Equal(1, calls);
        Assert.Equal(1, first.Manifest.TruncatedCount);
        Assert.Equal(419, first.Manifest.NotAttemptedCount);
        Assert.Equal(70, first.Manifest.ManualReviewPendingCount);
        var truncatedAttempt = Assert.Single(
            Directory.EnumerateFiles(
                first.OutputDirectory,
                "*.json",
                SearchOption.AllDirectories),
            path => path.Contains(
                $"{Path.DirectorySeparatorChar}attempts{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
                && File.ReadAllText(path).Contains(
                    "\"status\": \"truncated\"",
                    StringComparison.Ordinal));
        Assert.True(File.Exists(truncatedAttempt));

        var automatedEntries = campaign.Preflight.Entries
            .Where(entry =>
                entry.Classification
                == SourceAcquisitionPreflightClassification.ReadyAutomated)
            .OrderBy(entry => entry.StableOrdinal)
            .Take(2)
            .ToList();
        Assert.Equal(2, automatedEntries.Count);
        var secondAutomated = automatedEntries[1];
        var secondIntent = campaign.Plan.Intents.Single(intent =>
            intent.RequestId == secondAutomated.RequestId
            && intent.SourceId == secondAutomated.SourceId);
        var secondIntentId = SourceAcquisitionRunner.ComputeIntentId(
            configuration.CycleId,
            Bindings(),
            secondIntent);
        var secondAttemptPath = Assert.Single(
            Directory.EnumerateFiles(
                Path.Combine(
                    first.OutputDirectory,
                    "intents",
                    secondIntentId,
                    "attempts"),
                "*.json",
                SearchOption.TopDirectoryOnly));
        var secondCheckpointPath = Path.Combine(
            first.OutputDirectory,
            "intents",
            secondIntentId,
            "checkpoint.json");
        File.Delete(secondAttemptPath);
        File.Delete(secondCheckpointPath);

        clock.Advance(TimeSpan.FromDays(31));
        var resumed = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            configuration);

        Assert.False(resumed.Manifest.Complete);
        Assert.Equal(1, calls);
        Assert.Equal(1, resumed.Manifest.TruncatedCount);
        Assert.Equal(419, resumed.Manifest.NotAttemptedCount);
        Assert.Equal(70, resumed.Manifest.ManualReviewPendingCount);
        Assert.Equal(1, resumed.Manifest.ExpiredCount);
        var tombstonePath = Assert.Single(
            Directory.EnumerateFiles(
                resumed.OutputDirectory,
                "tombstone.json",
                SearchOption.AllDirectories),
            path => File.ReadAllText(path).Contains(
                    "\"originalStatus\": \"truncated\"",
                    StringComparison.Ordinal));
        var tombstone = File.ReadAllText(tombstonePath);
        Assert.DoesNotContain(
            "\"status\": \"completed\"",
            tombstone,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_lock_rejects_a_second_runner_and_cancellation_propagates()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var adapters = CreateAdapters(async (_, cancellationToken) =>
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch);
        });
        var config = Configuration(temp.Path, "cycle-lock");
        using var cancellation = new CancellationTokenSource();
        var first = new SourceAcquisitionRunner().RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            config,
            cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SourceAcquisitionRunner().RunAsync(
                campaign.Plan,
                campaign.Preflight,
                adapters,
                Bindings(),
                config));

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
    }

    [Fact]
    public async Task Unsafe_cycle_or_missing_retention_fails_before_adapter_transport()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var calls = 0;
        var adapters = CreateAdapters((_, _) =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var runner = new SourceAcquisitionRunner();

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "../escape")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            Configuration(temp.Path, "cycle-no-retention") with
            {
                CandidateRetentionDays = 0,
            }));

        Assert.Equal(0, calls);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "source-acquisition")));
    }

    [Fact]
    public void Adapter_factory_requires_pubmed_identity_rejects_key_and_has_exact_authoritative_catalog()
    {
        var factory = new SourceAcquisitionAdapterFactory();
        Assert.Equal(7, factory.Descriptors.Count);
        Assert.Equal(
            6,
            factory.Descriptors.Count(item => item.CandidateMethod == "api"));
        Assert.Single(
            factory.Descriptors,
            item => item.SourceId == "nih-nccih"
                    && item.CandidateMethod == "manual-review");

        Assert.Throws<InvalidOperationException>(() => factory.Create(
            RegistrySha,
            new WorkerOptions()));
        Assert.Throws<InvalidOperationException>(() => factory.Create(
            RegistrySha,
            new WorkerOptions
            {
                SourceAcquisitionPubMedTool = "biostack",
                SourceAcquisitionPubMedContactEmail = "ops@example.test",
                SourceAcquisitionPubMedApiKey = "not-approved",
            }));
    }

    [Fact]
    public void Source_acquisition_is_explicitly_database_free()
    {
        Assert.True(WorkerRunModePolicy.IsDatabaseFree(RunMode.SourceAcquisition));
        Assert.False(WorkerRunModePolicy.IsDatabaseFree(RunMode.Seed));
        Assert.False(WorkerRunModePolicy.IsDatabaseFree(RunMode.Refresh));
    }

    [Fact]
    public void Intent_identity_is_stable_for_set_order_and_changes_with_cycle()
    {
        var intent = CreateCampaign().Plan.Intents[0];
        var reordered = intent with
        {
            AuthorizedFieldUses = intent.AuthorizedFieldUses.Reverse().ToList(),
            RequiredProvenanceFields =
                intent.RequiredProvenanceFields.Reverse().ToList(),
        };

        var first = SourceAcquisitionRunner.ComputeIntentId(
            "cycle-a",
            Bindings(),
            intent);
        var same = SourceAcquisitionRunner.ComputeIntentId(
            "cycle-a",
            Bindings(),
            reordered);
        var different = SourceAcquisitionRunner.ComputeIntentId(
            "cycle-b",
            Bindings(),
            intent);

        Assert.Equal(first, same);
        Assert.NotEqual(first, different);
    }

    [Fact]
    public async Task Job_validates_current_exact_inputs_and_preflight_without_transport()
    {
        using var temp = new TempDirectory();
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var requestPath = Path.Combine(
            repositoryRoot,
            "research",
            "research-requests",
            "market-interest-coverage-2026-07-24.v1.json");
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var registryPath = Path.Combine(
            repositoryRoot,
            "research",
            "input",
            "sources",
            "pilot-source-registry.json");
        var options = new WorkerOptions
        {
            RunMode = RunMode.SourceAcquisition,
            ResearchOutputDirectory = temp.Path,
            SourceAcquisitionResearchRequestPath = requestPath,
            SourceAcquisitionDecisionPath = decisionPath,
            SourceAcquisitionRegistryPath = registryPath,
            SourceAcquisitionCycleId = "current-input-validation",
            SourceAcquisitionCandidateRetentionDays = 30,
            SourceAcquisitionReceiptRetentionDays = 90,
            SourceAcquisitionPubMedTool = "biostack",
            SourceAcquisitionPubMedContactEmail = "ops@example.test",
        };
        var capture = new CapturingRunner(temp.Path);
        var job = new SourceAcquisitionJob(
            options,
            ResearchArtifactValidator.LoadFromDirectory(
                TestPaths.WorkerSchemaDirectory()),
            new SourceAcquisitionPlanBuilder(),
            new SourceAcquisitionExecutionPreflight(),
            new SourceAcquisitionAdapterFactory(),
            capture);
        var context = new IngestionContext(options, NullLogger.Instance);

        var result = await job.RunAsync(context);

        Assert.True(result.Success);
        Assert.NotNull(capture.Preflight);
        Assert.Equal(70, capture.Preflight!.UniqueRequestCount);
        Assert.Equal(490, capture.Preflight.IntentCount);
        Assert.Equal(490, capture.Preflight.ReadyCount);
        Assert.Equal(0, capture.Preflight.BlockedCount);
        Assert.Equal(7, capture.Preflight.SourceCount);
        Assert.Equal(420, capture.Preflight.DispatchableCount);
        Assert.Equal(70, capture.Preflight.ManualReviewPendingCount);
        var exactRegistrySha = Convert.ToHexString(
                SHA256.HashData(await File.ReadAllBytesAsync(registryPath)))
            .ToLowerInvariant();
        Assert.Equal(exactRegistrySha, capture.Bindings!.SourceRegistrySha256);
    }

    [Fact]
    public async Task Expired_content_is_tombstoned_before_removal_and_same_cycle_stays_terminal()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        const string removedContent = "candidate-content-must-be-removed";
        var calls = 0;
        var adapters = CreateAdapters((intent, _) =>
        {
            Interlocked.Increment(ref calls);
            if (intent.SourceId == "fda" && intent.RequestId == "request-000")
            {
                var candidate = Candidate(intent, "retained") with
                {
                    Fields = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["identity"] = [removedContent],
                    },
                };
                return Task.FromResult(new SourceAcquisitionBatch(
                    SourceAcquisitionBatchStatus.Completed,
                    [candidate],
                    false,
                    null));
            }
            return Task.FromResult(EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var clock = new ManualTimeProvider(
            new DateTimeOffset(2026, 7, 26, 1, 0, 0, TimeSpan.Zero));
        var runner = new SourceAcquisitionRunner(clock);
        var configuration = Configuration(temp.Path, "cycle-expiry") with
        {
            CandidateRetentionDays = 1,
            ReceiptRetentionDays = 1,
        };
        var first = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            configuration);
        Assert.Equal(420, calls);
        Assert.Contains(
            removedContent,
            string.Join(
                "\n",
                Directory.EnumerateFiles(
                        first.OutputDirectory,
                        "*.json",
                        SearchOption.AllDirectories)
                    .Select(File.ReadAllText)),
            StringComparison.Ordinal);

        clock.Advance(TimeSpan.FromDays(2));
        var resumed = await runner.RunAsync(
            campaign.Plan,
            campaign.Preflight,
            adapters,
            Bindings(),
            configuration);

        Assert.True(resumed.Manifest.Complete);
        Assert.Equal(490, resumed.Manifest.ExpiredCount);
        Assert.Equal(420, calls);
        Assert.DoesNotContain(
            Directory.EnumerateFiles(
                resumed.OutputDirectory,
                "*.json",
                SearchOption.AllDirectories),
            path => path.Contains(
                $"{Path.DirectorySeparatorChar}attempts{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(
                resumed.OutputDirectory,
                "checkpoint.json",
                SearchOption.AllDirectories),
            _ => true);
        Assert.Equal(
            490,
            Directory.EnumerateFiles(
                resumed.OutputDirectory,
                "tombstone.json",
                SearchOption.AllDirectories).Count());
        var retainedJson = string.Join(
            "\n",
            Directory.EnumerateFiles(
                    resumed.OutputDirectory,
                    "*.json",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));
        Assert.DoesNotContain(removedContent, retainedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lease_loss_cancels_before_transport_or_artifact_mutation()
    {
        using var temp = new TempDirectory();
        var campaign = CreateCampaign();
        var transportCalls = 0;
        var adapters = CreateAdapters((_, _) =>
        {
            Interlocked.Increment(ref transportCalls);
            return Task.FromResult(
                EmptyBatch(SourceAcquisitionBatchStatus.NoMatch));
        });
        var store = new LeaseLostArtifactStore();
        var runner = new SourceAcquisitionRunner(
            new ManualTimeProvider(
                new DateTimeOffset(2026, 7, 26, 1, 0, 0, TimeSpan.Zero)),
            _ => store);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            runner.RunAsync(
                campaign.Plan,
                campaign.Preflight,
                adapters,
                Bindings(),
                Configuration(temp.Path, "cycle-lease-loss")));

        Assert.Equal(0, transportCalls);
        Assert.Equal(0, store.MutationCount);
    }

    private static Campaign CreateCampaign()
    {
        var sourceContracts = new[]
        {
            ("fda", "fda-planning-v1", "api"),
            ("pubchem", "pubchem-planning-v1", "api"),
            ("pubmed", "pubmed-planning-v1", "api"),
            ("clinicaltrials", "clinicaltrials-planning-v1", "api"),
            ("dailymed", "dailymed-planning-v1", "api"),
            ("nih-ods", "nih-ods-planning-v1", "api"),
            ("nih-nccih", "nih-nccih-planning-v1", "manual-review"),
        };
        var intents = Enumerable.Range(0, 70)
            .SelectMany(requestIndex => sourceContracts.Select(source =>
                new SourceAcquisitionIntent(
                    source.Item1,
                    source.Item2,
                    $"request-{requestIndex:D3}",
                    $"Compound {requestIndex:D3}",
                    [$"Compound {requestIndex:D3}", $"Alias {requestIndex:D3}"],
                    source.Item3,
                    ["identity", "regulatory"],
                    [
                        "sourceRegistryId",
                        "sourceItemId",
                        "sourceUrl",
                        "retrievedAtUtc",
                        "rightsReviewStatusAtRetrieval",
                        "transformationPipelineVersion",
                        "humanReviewStatus",
                    ],
                    "2.0.0",
                    RegistrySha,
                    SourceAcquisitionDisposition.Ready,
                    [])))
            .ToList();
        var plan = new SourceAcquisitionPlan(intents, 490, 0);
        var factory = new SourceAcquisitionAdapterFactory();
        var preflight = new SourceAcquisitionExecutionPreflight().Evaluate(
            plan,
            factory.Descriptors,
            SourceAcquisitionCampaignExpectation.CurrentRecommendedSevenActivation);
        Assert.True(preflight.CanActivate);
        return new Campaign(plan, preflight);
    }

    private static IReadOnlyDictionary<string, ISourceAcquisitionAdapter> CreateAdapters(
        Func<SourceAcquisitionIntent, CancellationToken, Task<SourceAcquisitionBatch>> acquire)
    {
        var sourceIds = new[]
        {
            "fda",
            "pubchem",
            "pubmed",
            "clinicaltrials",
            "dailymed",
            "nih-ods",
        };
        return sourceIds.ToDictionary(
            sourceId => sourceId,
            sourceId => (ISourceAcquisitionAdapter)new FakeAdapter(sourceId, acquire),
            StringComparer.Ordinal);
    }

    private static SourceAcquisitionCandidate Candidate(
        SourceAcquisitionIntent intent,
        string itemId) =>
        new(
            intent.RequestId,
            intent.CompoundName,
            intent.SourceId,
            itemId,
            $"https://example.test/{intent.SourceId}/{itemId}",
            $"https://example.test/{intent.SourceId}/query?q=test",
            "2026-07-26",
            new DateTimeOffset(2026, 7, 26, 1, 0, 0, TimeSpan.Zero),
            "reviewed",
            RegistrySha,
            "fake-v1",
            "review-required",
            ["Synthetic candidate for deterministic runtime testing."],
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity"] = [$"Candidate {itemId}"],
            })
        {
            AuthorizedFieldUses = ["identity"],
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase),
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    "candidate-fields",
                    "Synthetic official source",
                    $"https://example.test/{intent.SourceId}/{itemId}",
                    "https://example.test/terms",
                    "reviewed",
                    ["identity"]),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                "Synthetic source acknowledgement retained.",
                ["restricted-third-party-content"],
                NonEndorsementRequired: true),
        };

    private static SourceAcquisitionBatch EmptyBatch(SourceAcquisitionBatchStatus status) =>
        new(status, [], false, null);

    private static SourceAcquisitionInputBindings Bindings() =>
        new(RequestSha, DecisionSha, RegistrySha);

    private static SourceAcquisitionRuntimeConfiguration Configuration(
        string output,
        string cycle) =>
        new(output, cycle, CandidateRetentionDays: 30, ReceiptRetentionDays: 90);

    private sealed record Campaign(
        SourceAcquisitionPlan Plan,
        SourceAcquisitionExecutionPreflightResult Preflight);

    private sealed class FakeAdapter(
        string sourceId,
        Func<SourceAcquisitionIntent, CancellationToken, Task<SourceAcquisitionBatch>> acquire)
        : ISourceAcquisitionAdapter
    {
        public string SourceId { get; } = sourceId;
        public string AdapterId => $"fake-{SourceId}-v1";

        public Task<SourceAcquisitionBatch> AcquireAsync(
            SourceAcquisitionIntent intent,
            DateTimeOffset retrievedAtUtc,
            CancellationToken cancellationToken = default) =>
            acquire(intent, cancellationToken);
    }

    private sealed class LeaseLostArtifactStore
        : ISourceAcquisitionArtifactStore
    {
        public string Location => "fake://lease-lost";
        public int MutationCount { get; private set; }

        public Task<ISourceAcquisitionRunLease> AcquireRunLeaseAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<ISourceAcquisitionRunLease>(
                new AlreadyLostLease());

        public Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptAsync(
            string intentId,
            SourceAcquisitionIntent intent,
            SourceAcquisitionPreflightEntry entry,
            SourceAcquisitionInputBindings bindings,
            SourceAcquisitionRuntimeConfiguration configuration,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Lease loss must stop before reads.");

        public Task WriteAttemptAndCheckpointAsync(
            SourceAcquisitionAttemptArtifact attempt,
            CancellationToken cancellationToken)
        {
            MutationCount++;
            throw new InvalidOperationException(
                "Lease loss must stop before writes.");
        }

        public Task EnsureCheckpointAsync(
            SourceAcquisitionAttemptArtifact attempt,
            CancellationToken cancellationToken)
        {
            MutationCount++;
            throw new InvalidOperationException(
                "Lease loss must stop before writes.");
        }

        public Task WriteDerivedArtifactsAsync(
            SourceAcquisitionRunManifest manifest,
            SourceAcquisitionReviewQueue reviewQueue,
            CancellationToken cancellationToken)
        {
            MutationCount++;
            throw new InvalidOperationException(
                "Lease loss must stop before writes.");
        }
    }

    private sealed class AlreadyLostLease : ISourceAcquisitionRunLease
    {
        private readonly CancellationTokenSource _lost = CreateLost();

        public CancellationToken LeaseLost => _lost.Token;

        public ValueTask DisposeAsync()
        {
            _lost.Dispose();
            return ValueTask.CompletedTask;
        }

        private static CancellationTokenSource CreateLost()
        {
            var source = new CancellationTokenSource();
            source.Cancel();
            return source;
        }
    }

    private sealed class CapturingRunner(string outputDirectory)
        : ISourceAcquisitionRunner
    {
        public SourceAcquisitionExecutionPreflightResult? Preflight { get; private set; }
        public SourceAcquisitionInputBindings? Bindings { get; private set; }

        public Task<SourceAcquisitionRunResult> RunAsync(
            SourceAcquisitionPlan plan,
            SourceAcquisitionExecutionPreflightResult preflight,
            IReadOnlyDictionary<string, ISourceAcquisitionAdapter> adapters,
            SourceAcquisitionInputBindings inputBindings,
            SourceAcquisitionRuntimeConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            Preflight = preflight;
            Bindings = inputBindings;
            var manifest = new SourceAcquisitionRunManifest(
                "source-acquisition-runtime-v1",
                configuration.CycleId,
                inputBindings,
                preflight.UniqueRequestCount,
                preflight.IntentCount,
                preflight.ReadyCount,
                preflight.BlockedCount,
                preflight.SourceCount,
                CompletedCount: 420,
                NoMatchCount: 0,
                RateLimitedCount: 0,
                BackPressureCount: 0,
                TruncatedCount: 0,
                ErrorCount: 0,
                ManualReviewPendingCount: 70,
                NotAttemptedCount: 0,
                ExpiredCount: 0,
                Complete: true,
                IntentIds: []);
            return Task.FromResult(
                new SourceAcquisitionRunResult(manifest, outputDirectory));
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "biostack-source-acquisition-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (!Directory.Exists(Path)) return;
            Directory.Delete(Path, recursive: true);
        }
    }
}
