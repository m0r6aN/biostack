# BioStack ToolUniverse Scientific Research Sidecar


## Local plugin skill discovery

Before implementation, inspect:

```text
D:\Repos\agent-skills\plugins\
```

Load and review any relevant plugin skill instructions before planning or modifying code.

Requirements:

* Discover available plugin folders.
* Identify each plugin's `skill.md`, README, manifest, or equivalent instruction file.
* Summarize relevant capabilities before using them.
* Prefer project-specific skills over generic behavior when they apply.
* Do not assume a plugin is safe or applicable only from its name.
* Follow the skill's own constraints, input requirements, and safety boundaries.
* Record which skills were loaded and why.
* If a referenced skill is unavailable or malformed, document that and continue only with explicit fallback behavior.

The agent must not copy large plugin content into generated code unless the license and project intent allow it.


## Mission

Design and implement a bounded ToolUniverse integration that strengthens BioStack's compound research, evidence ingestion, dosage-context extraction, mechanism analysis, adverse-event research, pathway intelligence, local model-assisted extraction, reversible context optimization, and local-first scientific processing.

The integration must fit the existing BioStack backend rather than creating a parallel architecture.

Repository:

```text
D:\Repos\BioStack
````

Create In:
```text
D:\Repos\BioStack\backend\research-sidecar
````

## Sidecar language and container decision

Implement the BioStack scientific research sidecar in Python 3.12.

Use this initial Docker base image:

```text
ghcr.io/astral-sh/uv:python3.12-bookworm-slim
```


## Minimal starting Dockerfile

```dockerfile
FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

WORKDIR /app

# Install dependencies separately for Docker layer caching.
COPY pyproject.toml uv.lock ./

RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync \
      --locked \
      --no-dev \
      --no-install-project

COPY . .

RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync \
      --locked \
      --no-dev

ENV PATH="/app/.venv/bin:$PATH"

# Replace with the actual non-root UID/GID convention selected for BioStack.
RUN addgroup --system biostack \
    && adduser --system --ingroup biostack biostack \
    && chown -R biostack:biostack /app

USER biostack

EXPOSE 8080

CMD ["python", "-m", "biostack_research_sidecar"]


**Do not begin implementation until the current backend, ingestion lifecycle, governance controls, data model, and deployment topology have been inspected and documented.
Do not blindly install tooluniverse[all].**

---

# Product intent

BioStack is an evidence-informed protocol intelligence and harm-reduction platform.

It must help users understand:

* What published research studied
* What amounts, routes, schedules, and escalation patterns were used
* Which populations were studied
* What outcomes were reported
* What adverse events and discontinuations occurred
* What remains unknown, conflicting, extrapolated, or unsupported
* How a user-recorded protocol compares with reviewed evidence
* Whether an entered amount appears materially outside the researched context

BioStack must not withhold useful evidence merely because that evidence includes dosage or treatment context.

BioStack must also not transform research evidence into an invented personal prescription.

## Required distinction

BioStack may say:

> Reviewed trials initiated participants between 0.5 and 1.0 mg weekly and used the following escalation schedules.

BioStack may say:

> The recorded 12 mg amount is 12 to 24 times the initiation range used in the reviewed trials. No reviewed trial in this evidence set initiated participants at 12 mg.

BioStack may say:

> The reviewed evidence supports a lower-exposure initiation context than the amount entered.

BioStack must not say:

> You should take 0.5 mg.

BioStack must not say:

> 0.5 mg is safe for you.

BioStack must not calculate a personalized dose from age, weight, sex, goals, symptoms, or other profile fields.

Personal context may be used to explain evidence applicability, such as whether a user resembles or differs from a study population. It must not be used to manufacture a personalized prescription.

---

# Execution environment guidance

The user intends to run this work through Warp where possible, so the implementation agent should operate as a focused engineering worker and keep the human operator in the approval loop.

Use these principles:

* Prefer small, reviewable changes.
* Produce a written plan before modifying files.
* Ask before installing new dependencies, changing infrastructure, running destructive commands, or altering public behavior.
* Keep the repository buildable after each pull-request-sized unit of work.
* Preserve BioStack's local-first, review-first, evidence-first posture.
* Do not merge unrelated cleanup into this work.
* Do not silently widen product claims or user-facing guidance.

## Foreman-line plugin / Foreman guardrail requirement

Use the `foreman-line` plugin, Foreman Agent, or the locally configured Foreman-equivalent guardrail if available in the execution environment.

Before implementation, inspect and document:

```text
Foreman-line or Foreman availability
Installed version
Configuration path
Plugin mode
MCP mediation support
Command approval support
Risk scoring support
Local audit log location
SQLite log location, if applicable
Integration with Warp, shell, MCP, Codex, Claude Code, or other agent runtime
```

Do not assume the command name. Check local documentation, shell aliases, package scripts, Warp integrations, plugin metadata, or repo-specific instructions.

Possible discovery commands may include, but are not limited to:

```bash
foreman --version
foreman-agent --version
which foreman
which foreman-agent
npm list -g --depth=0 | grep -i foreman
```

Use the actual installed tool contract if it differs from these examples.

## Required Foreman behavior

When available, route agent tool calls, shell execution, MCP calls, file edits, and network-sensitive actions through Foreman or the configured foreman-line mediation layer.

Foreman must be used for:

* Shell command approval
* MCP tool-call mediation
* Network-access approval
* Secret-file access protection
* Destructive command review
* Cross-agent tool-call visibility
* Local audit logging
* Session traceability
* Permission history

Foreman must not be used as an excuse to bypass BioStack's own safeguards.

The agent must still obey:

* BioStack review fences
* Git discipline
* Human approval requirements
* Secret-handling rules
* Test requirements
* Product-boundary constraints

## Foreman failure behavior

If Foreman or foreman-line is unavailable:

1. Do not fake Foreman usage.
2. Record that the guardrail is unavailable.
3. Continue only with explicit human approval for sensitive actions.
4. Use Warp's normal command approval flow where available.
5. Preserve a manual session log in the implementation notes.
6. Avoid unattended execution.

If Foreman blocks a tool call, do not route around it unless the human operator explicitly approves the alternative path.

## Foreman audit expectations

The final report must include:

```text
Foreman-line or Foreman status
Version
Configuration used
Approval mode
Sensitive actions requested
Sensitive actions approved
Sensitive actions denied
Tool calls mediated
Known gaps
Fallback approval process used, if any
```

---

# Phase 0: Establish repository truth

## 0.1 Inspect the existing architecture

Before writing code, inspect and document:

* Solution and project structure
* Domain, Application, Infrastructure, and API boundaries
* Current .NET and database versions
* Dependency-injection patterns
* Background job and queue infrastructure
* Existing provider interfaces
* Existing knowledge-ingestion interfaces
* Admin intake workflows
* Resolve, stage, review, reject, defer, approve, and promote states
* Provenance and source-lane receipt handling
* Duplicate-intake and retry behavior
* Knowledge-ingest fences and overrides
* Existing compound and evidence entities
* Citation and evidence-tier models
* Current migrations
* Current API surfaces
* Existing tests and architectural guards
* Configuration, secrets, deployment, and Docker support
* Current privacy boundaries for local and hosted data
* Any Commander or Protocol Intelligence dependency on the knowledge base
* Development and deployment host operating systems
* Available NVIDIA GPU hardware, exact model, VRAM, compute capability, and power profile
* Installed NVIDIA driver version
* CUDA runtime and toolkit compatibility
* WSL 2 configuration, if the development host is Windows
* Docker Desktop GPU passthrough support
* NVIDIA Container Toolkit availability
* Existing local AI inference services or model runtimes
* Existing CPU, RAM, storage, and GPU resource constraints
* Whether the production deployment environment will have a GPU
* Which sidecar workloads can benefit materially from GPU acceleration
* Which workloads must remain CPU-capable
* Installed Ollama version and runtime configuration
* Ollama host address and network exposure
* Currently installed Ollama models
* Model artifact sizes, digests, quantization, and licenses
* Runtime context allocation for each installed model
* GPU and CPU offload behavior for each model
* Ollama concurrency, queue, and keep-alive configuration
* Ollama structured-output support
* Ollama tool-calling support
* Available embedding models
* Existing local model benchmarks
* Existing model routing or inference abstractions
* Keon Kompress repository version and deployed image version
* Keon Kompress HTTP and MCP surfaces
* Keon Kompress tenant-resolution behavior
* Keon Kompress message-role handling
* Keon Kompress system-message compression configuration
* Keon Kompress storage path, TTL, eviction, and durability
* Keon Kompress retrieval-marker contract
* Keon Kompress authentication and network exposure
* Whether BioStack and the scientific sidecar can reach Ollama and Kompress through private container networking
* Foreman-line or Foreman availability and configuration
* Warp execution constraints and approval behavior

Do not assume that `RTX 3500` uniquely identifies the hardware generation or capabilities. Detect and record the exact device model, architecture, VRAM, supported CUDA version, driver version, and runtime configuration from the target host.

The design must distinguish between:

1. Developer-workstation GPU acceleration
2. Optional self-hosted production GPU acceleration
3. CPU-only production deployment
4. Hosted-model or remote-compute fallback

GPU availability must be treated as an optimization capability, not as an architectural prerequisite.

Do not infer model capability from model name, parameter count, advertised context length, or reputation.

Inspect the actual local runtime and record:

* Model digest
* Quantization
* Runtime-allocated context
* GPU offload percentage
* Peak VRAM
* Peak system RAM
* Tokens per second
* Structured-output reliability
* Tool-calling reliability
* Extraction accuracy
* Source-location accuracy
* Failure behavior

The router must make decisions from verified capability profiles and BioStack-owned benchmarks.

## 0.2 Determine the correct integration seam

Research and explicitly decide whether ToolUniverse should integrate through:

1. An existing BioStack provider interface
2. An extension of the current knowledge-intake pipeline
3. A new scientific-research provider abstraction
4. A combination of the above

Prefer reuse over duplication.

Do not create a second review lifecycle if the existing intake, staging, promotion, provenance, retry, and knowledge-ingest controls can support this work.

## 0.3 Produce an architecture decision record

Create an ADR covering:

* Current backend integration points
* Selected integration seam
* Why a Python sidecar is preferred or rejected
* Transport selection
* Job lifecycle
* Data ownership
* Persistence ownership
* Failure behavior
* Privacy boundary
* Security boundary
* Tool allowlisting
* Version pinning
* Review and promotion workflow
* Rollback and kill-switch behavior
* GPU acceleration boundary
* GPU-capable workload selection
* CPU and hosted fallback strategy
* GPU worker isolation
* GPU model lifecycle and unloading
* VRAM budgeting
* GPU concurrency limits
* CUDA and driver compatibility
* Local model provenance
* Development versus production GPU differences
* Behavior when no GPU is available
* Local inference provider abstraction
* Ollama integration method
* Intelligent model-routing boundary
* Model registry ownership
* Model capability discovery
* Model benchmark and approval process
* Task-to-model routing policy
* Local versus hosted escalation policy
* Structured-output validation
* Model context budgeting
* Model loading and unloading policy
* Model concurrency and queue behavior
* Keon Kompress integration boundary
* Compression eligibility policy
* Protected message and content classes
* Reversible retrieval flow
* Compression-store retention
* Kompress tenant and job isolation
* Full evidence hash versus Kompress marker hash
* Behavior when Ollama is unavailable
* Behavior when Kompress is unavailable
* Foreman-line or Foreman usage during implementation
* Warp execution assumptions and approval workflow

The ADR must explicitly decide whether GPU workloads run:

1. Inside the primary scientific-research sidecar
2. In a separate GPU worker service
3. Through an existing local inference runtime
4. Through a combination of these approaches

The default recommendation is a separately bounded GPU worker or GPU execution lane so that model loading, CUDA failures, memory exhaustion, and heavyweight inference cannot destabilize network retrieval, evidence normalization, or the main BioStack backend.

The ADR must explicitly determine whether the scientific sidecar calls:

1. Ollama's native API
2. Ollama's OpenAI-compatible API
3. A BioStack-owned inference gateway wrapping Ollama
4. A combination of these surfaces

The preferred architecture is a BioStack-owned provider abstraction with an Ollama infrastructure adapter.

The ADR must also determine whether Keon Kompress is consumed through:

1. An internal production HTTP contract
2. Its MCP surface
3. An in-process .NET adapter
4. A dedicated BioStack integration host

Do not couple the Python sidecar directly to a demo-only contract without first defining tenant, role, authorization, retention, correlation, and failure semantics.

No implementation should proceed until this ADR is complete.

---

# Phase 1: Reconcile the guidance boundary

The existing BioStack legal and governance drafts prohibit recommending a dose or generating personalized dose, titration, route, or treatment instructions.

The intended product posture now permits richer harm-reduction support using published evidence.

Create a formal decision record that distinguishes the following classes.

## Class A: Published evidence context

Permitted:

* Trial initiation amounts
* Trial escalation schedules
* Maintenance ranges
* Frequency and route used in a source
* Study duration
* Population characteristics
* Inclusion and exclusion criteria
* Outcomes
* Adverse events
* Discontinuation rates
* Regulatory label information
* Official safety communications
* Case-report amounts
* Observational or community-reported patterns, clearly labeled as such

## Class B: Evidence comparison

Permitted with reviewed wording:

* Comparing a user-recorded amount with published study ranges
* Calculating how many times higher or lower an entry is than a researched amount
* Identifying that no reviewed study used a comparable initiation amount
* Showing that evidence is unavailable for a route, frequency, combination, or population
* Flagging unit mismatches and likely decimal errors
* Identifying that evidence comes from animals, in-vitro work, case reports, or uncontrolled observations
* Showing that an entered plan differs materially from reviewed research protocols

## Class C: Evidence-guided harm-reduction context

Permitted only through an approved content contract:

* Highlighting lower-exposure initiation patterns found in credible human evidence
* Showing escalation approaches used in trials
* Explaining that slower or lower initiation was used to manage tolerability in a cited source
* Showing what researchers monitored
* Showing conditions that led to treatment interruption or discontinuation
* Surfacing official contraindications or warnings with exact scope
* Encouraging review before proceeding when an entry is materially outside the available evidence

## Class D: Personalized medical direction

Remain prohibited unless BioStack deliberately changes product and regulatory posture:

* Selecting the correct dose for a person
* Giving a personalized titration schedule
* Diagnosing a condition
* Declaring an amount safe
* Declaring a protocol appropriate
* Replacing clinical monitoring
* Predicting that a user will experience a particular outcome
* Automatically changing a protocol
* Issuing an uncited start, stop, increase, decrease, combine, or substitute instruction

## Required deliverable

Create a versioned `BioStack Guidance Content Contract` defining:

* Permitted output classes
* Prohibited output classes
* Required evidence fields
* Required warning fields
* Required uncertainty language
* Required approval levels
* Examples and counterexamples
* Copy-guard terms
* Escalation rules
* Policy and consent impacts

Do not expose new dosing-context behavior publicly until the product canon, legal drafts, governance manual, and human approval records are reconciled.

---

# Phase 2: Implement the sidecar boundary

## Preferred architecture

Use a separately deployable Python scientific-research sidecar.

```text
BioStack API / Application
        |
        | BioStack-owned typed research contract
        v
Scientific Research Sidecar
        |
        | Allowlisted workflows only
        v
ToolUniverse Python SDK and approved scientific sources
```

## Important implementation rule

The BioStack backend must not receive unrestricted access to the full ToolUniverse registry.

The sidecar must expose narrow, BioStack-owned operations such as:

```text
ResolveCompoundIdentity
ResearchCompoundEvidence
ResearchPublishedRegimens
ResearchAdverseEvents
ResearchMechanismsAndTargets
ResearchPathways
RefreshEvidencePacket
GetResearchJob
CancelResearchJob
```

Do not expose a generic production endpoint equivalent to:

```text
ExecuteAnyTool(toolName, arbitraryArguments)
```

## Transport

Evaluate transport during the ADR.

Default preference:

* Internal HTTP or gRPC between the .NET backend and Python sidecar
* Asynchronous job semantics for long-running research
* JSON Schema or Protobuf contracts owned by BioStack
* MCP may be used by development agents, but raw MCP should not define BioStack's canonical domain contract

## Sidecar restrictions

The sidecar must:

* Run as a non-root user
* Be internal-only
* Require service authentication
* Use an outbound network allowlist where practical
* Use read-only application filesystems where practical
* Have CPU, memory, duration, and concurrency limits
* Disable arbitrary Python or shell execution
* Disable tools outside the approved allowlist
* Store secrets outside source control
* Redact secrets and sensitive arguments from logs
* Pin ToolUniverse to an exact tested release or commit
* Generate an SBOM
* Record upstream licenses and terms
* Support a global kill switch
* Support per-workflow kill switches
* Fail closed when required sources or identifiers cannot be resolved

---

# Phase 2A: Add optional local GPU acceleration

## Purpose

Use available NVIDIA GPU hardware to accelerate selected local scientific-processing workloads while preserving CPU-only operation and deployment portability.

The GPU is not expected to accelerate ordinary remote API access, database lookups, HTTP retrieval, deterministic unit conversion, JSON normalization, or BioStack review and promotion workflows.

The GPU should be considered for workloads such as:

* Scientific-text embeddings
* Semantic deduplication
* Publication relevance reranking
* Local structured evidence extraction
* PDF layout analysis
* OCR
* Table extraction
* Local language-model inference
* Local biomedical classification models
* Selected protein, molecular, or structure models
* Batch processing of downloaded scientific documents

Do not enable a GPU workflow merely because it supports CUDA. Require measurable benefit in latency, throughput, privacy, cost, or extraction quality.

## Required capability discovery

At sidecar or GPU-worker startup, detect and report:

```text
GPU available
GPU manufacturer
Exact GPU model
GPU architecture
Compute capability
Total VRAM
Available VRAM
NVIDIA driver version
CUDA runtime version
CUDA library compatibility
PyTorch or selected framework version
Container GPU passthrough status
Supported numerical precision
Supported local models
Configured VRAM budget
Configured concurrency
```

Represent this as a structured capability artifact such as:

```text
GpuCapabilityManifest
```

The manifest must be queryable through an internal administrative or health surface and must not expose unnecessary host details publicly.

## Required execution modes

Support the following execution modes:

```text
Auto
GpuPreferred
GpuRequired
CpuOnly
HostedFallbackAllowed
```

## Recommended worker topology

Prefer the following logical topology:

```text
BioStack .NET Backend
        |
        v
Scientific Research Coordinator
        |
        +------------------------------+
        |                              |
        v                              v
CPU and Network Worker             GPU Worker
-----------------------            -----------------------
PubMed retrieval                   Embeddings
PubChem retrieval                  Semantic deduplication
ClinicalTrials.gov                 Relevance reranking
FDA and label retrieval            Structured extraction
Source downloading                 PDF layout processing
Deterministic transforms           OCR and table extraction
Schema validation                  Approved local inference
        |                              |
        +---------------+--------------+
                        |
                        v
             Candidate Evidence Packet
                        |
                        v
             Existing Review Lifecycle
```

The GPU worker may run in the same deployable sidecar during the proof of concept, but its interfaces, queueing, resource ownership, and failure behavior must remain separable.

## GPU workload allowlist

Create an explicit allowlist of GPU-capable operations.

An initial allowlist may include:

```text
GenerateScientificEmbeddings
DeduplicateLiteratureCandidates
RerankLiteratureCandidates
ExtractStructuredStudyData
ExtractPublishedExposureRegimens
ExtractAdverseEventTables
ProcessScientificPdf
PerformDocumentOcr
ClassifyEvidenceType
RunApprovedBiomedicalModel
```

Do not expose arbitrary model execution.

Do not allow a request to specify an arbitrary Hugging Face repository, model URL, Python module, checkpoint, executable, or container image.

Every approved model must have:

```text
Canonical model identifier
Pinned version or revision
Model artifact hash
License
Approved purpose
Input contract
Output contract
VRAM estimate
Maximum input size
Maximum batch size
Expected precision
CPU fallback status
Hosted fallback status
Security review status
Validation status
```

## Resource management

The GPU worker must enforce:

* Per-model VRAM budgets
* Per-job VRAM estimates
* Maximum batch sizes
* Maximum input lengths
* Maximum execution duration
* Maximum queued GPU jobs
* Maximum concurrent model instances
* Maximum concurrent inference jobs
* Model idle-unload thresholds
* Graceful model unloading
* Cancellation
* Thermal and hardware-failure handling
* Out-of-memory recovery

Default to one heavyweight GPU job at a time until benchmarking proves that greater concurrency is safe.

Do not configure multiple server workers that each load independent copies of the same large model unless VRAM capacity and isolation have been explicitly validated.

## Model lifecycle

Models must not remain loaded indefinitely without need.

Support:

```text
Load on first approved use
Reuse while active
Unload after configured idle period
Unload under memory pressure
Unload before incompatible model execution
Unload during graceful shutdown
```

Model-loading failure must not crash the scientific-research coordinator.

## Precision and reproducibility

Select numerical precision deliberately.

Where supported, evaluate:

* FP32
* FP16
* BF16
* Approved quantized formats

Reduced precision must not be used blindly for high-impact extraction or scientific prediction. Validate that the selected precision does not materially degrade the output contract for the intended workflow.

Record the selected precision in execution provenance.

## Local inference boundary

Local GPU models may assist with:

* Candidate triage
* Document classification
* Information extraction
* Evidence structuring
* Reranking
* OCR
* Mechanism classification
* Research summarization

Local model output must remain candidate evidence until it passes BioStack validation and human review.

A locally generated statement is not a scientific source.

The source must remain the underlying publication, label, database record, trial record, or official communication.

## GPU failure behavior

Expected GPU failures include:

* GPU unavailable
* Driver mismatch
* CUDA runtime mismatch
* Unsupported compute capability
* Container passthrough unavailable
* Model incompatible with device
* Insufficient VRAM
* CUDA out-of-memory
* GPU reset
* Thermal throttling
* Model load timeout
* Inference timeout
* Invalid numerical output
* Corrupted model artifact
* Framework incompatibility

Required behavior:

* Return typed failures.
* Never crash the main BioStack backend.
* Never corrupt an in-progress evidence packet.
* Release GPU memory after failure.
* Retry only when the failure is plausibly transient.
* Reduce batch size when configured and safe.
* Fall back only through an explicitly approved path.
* Preserve completed partial work.
* Record whether the result used GPU, CPU, or hosted execution.
* Make degraded execution visible in operational telemetry.
* Keep core research retrieval operational when the GPU worker is disabled.

## Windows development configuration

When the development host is Windows, investigate and document the supported configuration for:

```text
Windows host
    -> Current NVIDIA Windows driver
    -> WSL 2
    -> Linux container runtime
    -> Docker Desktop WSL 2 backend
    -> NVIDIA GPU passthrough
    -> Scientific research sidecar
    -> Optional GPU worker
```

Do not require installation of a separate Linux NVIDIA display driver inside WSL when the supported Windows-to-WSL driver path is being used.

Provide a repeatable validation script that verifies, at minimum:

```text
Host can detect the GPU
WSL can detect the GPU
A test container can detect the GPU
The selected Python framework can detect CUDA
A small approved inference workload completes
GPU memory is released afterward
CPU fallback still works
```

The exact CUDA base image and framework versions must be selected from verified compatibility rather than copied from an example without validation.

## Deployment portability

The same BioStack research contract must work when:

* The GPU is present
* The GPU is absent
* The GPU worker is stopped
* CUDA is broken
* The host is CPU-only
* The deployment moves from a workstation to a server
* The deployment uses a hosted fallback
* GPU acceleration is administratively disabled

Do not leak CUDA-specific or model-runtime-specific types into the BioStack Domain or Application layers.

---

# Phase 2B: Add local inference and intelligent model routing

## Purpose

Use local Ollama models for approved scientific-processing tasks while selecting models according to measured capability, task requirements, privacy, latency, context size, resource availability, and evidence risk.

The model router is an execution-policy component.

It must not be an unbounded agent that decides its own authority.

## Core principle

Do not use a language model when deterministic code can perform the task correctly.

Examples that must remain deterministic include:

* Unit conversion
* Amount normalization
* Range comparison
* Hashing
* Identifier validation
* Citation formatting
* Schema validation
* Duplicate key detection
* Date calculations
* Evidence-state transitions
* Promotion authorization
* Policy enforcement
* Retrieval authorization
* Mathematical comparison of a user entry with reviewed evidence

Local models may assist with:

* Literature classification
* Semantic deduplication
* Relevance reranking
* Information extraction
* Study-arm extraction
* Regimen extraction
* Adverse-event extraction
* Table interpretation
* Contradiction discovery
* Mechanism summarization
* Evidence-packet drafting
* Source applicability analysis

## Required abstractions

Create provider-neutral abstractions similar to:

```csharp
public interface IModelRouter
{
    Task<ModelRouteDecision> SelectAsync(
        ModelRoutingRequest request,
        CancellationToken cancellationToken);
}

public interface IInferenceProvider
{
    Task<InferenceResult> ExecuteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken);
}

public interface IModelRegistry
{
    Task<IReadOnlyList<ModelCapabilityProfile>> GetAvailableAsync(
        CancellationToken cancellationToken);
}

public interface IInferenceOutputValidator
{
    Task<InferenceValidationResult> ValidateAsync(
        InferenceResult result,
        InferenceValidationContract contract,
        CancellationToken cancellationToken);
}
```

Exact names and project placement must follow existing BioStack conventions.

The Domain and Application layers must not depend on Ollama-specific request types.

## Model capability registry

Maintain a BioStack-owned registry of approved models.

Each model profile must include:

```text
Provider
Canonical model name
Model digest
Model family
Parameter class
Quantization
License
Input modalities
Output modalities
Structured-output support
Tool-calling support
Thinking-mode support
Embedding support
Maximum advertised context
Maximum validated context
Recommended context
GPU memory requirement
System memory requirement
Expected tokens per second
Approved task classes
Prohibited task classes
Benchmark version
Benchmark results
Prompt-template version
Known weaknesses
Validation status
Approval status
```

A model name alone is not an approved model identity.

Pin and record the digest or equivalent immutable artifact identity.

## Initial model candidates

Evaluate the user's currently installed models as candidates:

```text
qwen3.5:9b
gemma4:12b
```

Do not permanently assign responsibilities based only on general model reputation.

Benchmark both models for:

* Scientific entity extraction
* JSON-schema compliance
* Study-design classification
* Published regimen extraction
* Adverse-event extraction
* Source-passage citation
* Contradiction analysis
* Table interpretation
* Refusal to invent missing values
* Performance with compressed context
* Performance with retrieved original context

Download additional models only when a measured capability gap exists.

## Dedicated embedding models

Do not automatically use a general chat model for embeddings.

Evaluate one or more dedicated local embedding models for:

* Scientific semantic search
* Duplicate-publication detection
* Preprint and final-publication matching
* Evidence clustering
* Similar-study retrieval
* Source-passage retrieval

Embedding models require their own capability profiles, dimensions, normalization rules, versioning, and benchmark results.

Changing the embedding model creates a new embedding version and must not silently overwrite prior vectors.

## Task classification

Every inference request must declare a task class.

Initial task classes may include:

```text
ScientificEmbedding
LiteratureDeduplication
LiteratureReranking
EntityResolutionAssistance
StudyClassification
StructuredStudyExtraction
PublishedRegimenExtraction
AdverseEventExtraction
TableExtraction
MechanismExtraction
PathwayExtraction
ContradictionAnalysis
EvidenceApplicabilityAnalysis
EvidenceSummary
ResearchPacketDrafting
```

Each task class must define:

* Required capabilities
* Minimum quality score
* Required context
* Output schema
* Source-attribution requirement
* Exactness requirement
* Maximum latency
* Maximum retries
* Permitted providers
* Permitted compression modes
* Escalation policy

## Evidence-risk classes

Classify model tasks by impact.

### Low impact

Examples:

* Topic classification
* Candidate clustering
* Search-result reranking
* Formatting a reviewed record

A single approved local model may be sufficient when validation passes.

### Medium impact

Examples:

* Study-design classification
* Mechanism extraction
* Population extraction
* Limitation extraction
* Evidence applicability analysis

Require schema validation and source-location support.

### High impact

Examples:

* Dose or exposure-regimen extraction
* Escalation schedule extraction
* Adverse-event extraction
* Contraindication extraction
* Discontinuation-condition extraction
* Safety-warning interpretation
* Conflict adjudication

Require:

* Exact source-location support
* Validation against original text
* Deterministic unit checks
* Human review before promotion
* Escalation when confidence or agreement is insufficient

High-impact model output is never canonical by itself.

## Routing algorithm

The router must follow a bounded policy.

```text
1. Determine whether the task should be deterministic.
2. Resolve task requirements.
3. Resolve evidence-risk class.
4. Determine privacy and provider restrictions.
5. Determine required modalities.
6. Determine context requirement.
7. Determine structured-output and tool requirements.
8. Read available runtime capabilities.
9. Exclude models that do not satisfy hard requirements.
10. Exclude models that cannot fit safely in available resources.
11. Rank remaining models using benchmark evidence.
12. Select the least expensive model that satisfies the quality floor.
13. Execute with bounded retries.
14. Validate schema, citations, source locations, and exactness.
15. Escalate only through an approved route when validation fails.
16. Record the complete routing decision.
```

The router must not select models randomly.

A model may propose a route, but a model-generated routing proposal must never be the sole authority for model selection.

## Route scoring

Candidate route scoring may consider:

```text
Task benchmark score
Structured-output reliability
Source-location accuracy
Hallucination rate
Context fit
GPU fit
Expected latency
Queue depth
Warm or cold model state
Privacy
Provider cost
Fallback availability
Recent failure rate
```

Hard requirements must be applied before weighted scoring.

A high aggregate score must not override a missing hard capability.

## Local-first policy

Prefer approved local inference when:

* Quality meets the task threshold
* The model fits available resources
* Required context can be handled
* The model supports the output contract
* Privacy permits local processing
* Latency is acceptable

Escalate to a stronger local model when the first local route fails validation.

Use a hosted model only when:

* Hosted fallback is explicitly permitted
* The payload's data classification permits it
* Local models fail the quality threshold
* Local resources are unavailable or insufficient
* The task's escalation policy allows it

Hosted escalation must never occur silently.

## Ollama adapter

Implement an Ollama infrastructure adapter supporting, where applicable:

* Chat inference
* Structured JSON output
* JSON Schema output
* Embeddings
* Tool calling
* Vision input
* Model inventory
* Model status
* Runtime metrics
* Explicit context configuration
* Model preload
* Model keep-alive
* Model unload
* Cancellation
* Timeout
* Health checks

Prefer structured outputs for extraction tasks.

Do not rely on prompt wording alone to produce valid JSON.

## Context-size truth

Record all of the following separately:

```text
Advertised model context
Configured Ollama context
Actually allocated runtime context
Maximum BioStack-validated context
Context used for the request
```

Do not route a long-context task based only on the model's advertised maximum.

Context length consumes memory and may cause CPU offload, severe latency, or model-load failure.

The router must use runtime-validated context limits.

## Model lifecycle

Coordinate Ollama model lifecycle with the GPU resource manager.

Support:

```text
Preload approved warm model
Keep frequently used model warm
Unload idle model
Unload before loading an incompatible model
Avoid loading multiple large models when VRAM is insufficient
Queue requests when safe
Reject requests when queue limits are exceeded
```

The router must account for model-switching cost.

A smaller warm model may be preferable to a larger cold model only when it still meets the task-quality threshold.

## Structured-output validation

For structured extraction:

1. Provide a versioned JSON Schema.
2. Request schema-constrained output.
3. Parse the result strictly.
4. Reject unknown or malformed critical fields.
5. Validate units and identifiers deterministically.
6. Require source locations.
7. Verify source locations against original content.
8. Retry or escalate when validation fails.
9. Preserve the invalid attempt for operational diagnostics without promoting it.

Schema-valid output is not automatically factually valid.

## Model disagreement

For high-impact extraction, support bounded cross-checking.

Possible policy:

```text
Primary local extraction
        ↓
Deterministic validation
        ↓
Second-model verification when required
        ↓
Agreement analysis
        ↓
Human review
```

Do not use majority voting to turn shared hallucination into evidence.

Model agreement increases confidence only when both outputs point to valid original-source locations.

## Prompt and contract versioning

Version all:

* System prompts
* Task prompts
* JSON Schemas
* Tool definitions
* Model profiles
* Routing policies
* Validation contracts
* Escalation policies

A prompt change that can alter extracted scientific claims requires a new version and regression validation.

## No autonomous promotion

The model router may select execution resources.

It may not:

* Approve evidence
* Promote knowledge
* Change a protocol
* Override a review fence
* Select a personal dose
* Suppress contradictory evidence
* Authorize hosted data transmission
* Modify routing policy
* Download unapproved models

---

# Phase 2C: Integrate Keon Kompress as a reversible context-optimization service

## Purpose

Use Keon Kompress to reduce repetitive or low-density context sent to local or hosted models while preserving the original material for authorized hash-based retrieval.

Kompress is a context optimization layer.

It is not:

* A scientific source
* A canonical evidence store
* A replacement for BioStack provenance
* A replacement for document chunking
* A replacement for retrieval
* A replacement for source verification
* Authority to modify system instructions

## Required placement

Use the following sequence:

```text
ToolUniverse or source retrieval
        ↓
Persist immutable raw source
        ↓
Calculate BioStack full-content hash
        ↓
Classify segment and exactness requirements
        ↓
Apply Kompress only when eligible
        ↓
Build model context
        ↓
Execute model
        ↓
Validate output against original source
```

Never compress content before BioStack has preserved the exact original and calculated its canonical evidence hash.

## Absolute system-message rule

System messages must never be compressed.

Developer messages, routing policy, authorization instructions, safety policy, and governance instructions must also remain verbatim.

Enforce this rule at multiple layers:

1. The BioStack context assembler marks these segments as protected.
2. The Kompress adapter refuses protected roles.
3. The BioStack Kompress profile disables system-message compression.
4. Contract tests verify byte-identical passthrough.
5. An invalid caller override cannot re-enable system-message compression.
6. The production Kompress service enforces the policy independently of the caller where technically feasible.

Do not depend only on a configuration default.

The BioStack integration must explicitly set:

```text
CompressSystemMessages = false
```

The preferred production rule is stronger:

```text
System and developer roles are never eligible, regardless of per-request configuration.
```

## Protected content classes

The following content must bypass lossy compression:

```text
System messages
Developer messages
Safety and governance instructions
Authorization context
Routing policy
Output JSON Schemas
Tool definitions
Unit-conversion rules
Canonical identifiers
Hash values
Cryptographic receipts
Exact source quotations used as evidence
Short high-density source passages
Final human-approved evidence records
```

Lossless normalization may be considered only where byte identity is not required and the transformation is explicitly approved.

## Eligible content classes

Initial Kompress candidates include:

```text
Verbose ToolUniverse responses
Repeated API metadata
Large search-result payloads
Duplicate literature metadata
Retry-heavy logs
Verbose diagnostic traces
Older non-protected conversation turns
Large JSON responses
Repeated boilerplate
Low-density document context used for discovery
```

Eligibility must depend on:

* Message role
* Content type
* Task class
* Exactness requirement
* Evidence-risk class
* Token count
* Expected savings
* Retrieval availability
* Retention requirement

## Exactness modes

Add explicit compression exactness modes:

```text
VerbatimRequired
LosslessOnly
ReversibleLossyAllowed
SummaryCandidateOnly
CompressionProhibited
```

### VerbatimRequired

Use for source passages supporting:

* Amounts
* Units
* Frequencies
* Escalation schedules
* Safety warnings
* Adverse-event counts
* Contraindications
* Direct quotations
* Legal or regulatory wording

The model must receive original text for final extraction or validation.

### LosslessOnly

Permit whitespace or representation optimization only when meaning and required source offsets remain stable.

### ReversibleLossyAllowed

Permit lossy compression only when:

* The original is stored successfully
* A valid retrieval marker is present
* Retrieval authorization is available
* The model or orchestrator can recover the original
* Final claims will be verified against the original

### SummaryCandidateOnly

Permit aggressive reduction for search triage or broad discovery where the output cannot directly support a promoted claim.

### CompressionProhibited

Return the original unchanged.

## BioStack hash versus Kompress hash

Maintain separate identities.

### BioStack canonical evidence hash

Use the complete BioStack-approved cryptographic digest of the exact original evidence artifact.

This hash supports:

* Provenance
* Deduplication
* Artifact identity
* Review
* Promotion
* Reproduction
* Change detection

### Kompress retrieval hash

Use the Kompress marker solely for reversible execution-time retrieval.

Do not:

* Use the shortened Kompress marker as BioStack's canonical artifact identity
* Use a Kompress marker as a citation
* Persist an unresolved marker inside canonical knowledge
* Assume the marker remains retrievable beyond its configured retention
* Treat matching compressed output as proof of matching original content

Store an explicit mapping where required:

```text
BioStackArtifactId
BioStackFullHash
KompressTenant
KompressRetrievalHash
CompressionExecutionId
Expiration
```

## Production integration contract

Do not bind directly to the existing demo contract without extending or wrapping it.

The BioStack integration requires operations similar to:

```text
CompressContextSegment
RetrieveOriginalSegment
GetCompressionStats
GetCompressionCapability
CheckCompressionHealth
```

A compression request should include:

```text
Tenant ID
Research job ID
Correlation ID
Context segment ID
Message role
Content type
Exactness mode
Original content
BioStack full hash
Target ratio
Minimum savings
Retention class
Expiration requirement
Model target
Task class
```

A compression result should include:

```text
Compression execution ID
Compressed content
Detected content type
Tokens before
Tokens after
Savings
Transforms applied
Lossless or lossy
Retrieval hash
Retrieval expiration
Original-store confirmation
Never-worse result
Failure or bypass reason
Kompress version
```

## Tenant and job isolation

Use a dedicated BioStack tenant or an approved finer-grained tenant model.

Every compression and retrieval operation must be bound to:

* Tenant
* Research job
* Correlation ID
* Authorized service identity

A hash alone must not authorize retrieval.

Tenant A must not retrieve Tenant B's content even when the hash is known.

## Retrieval modes

Support two retrieval patterns.

### Model-directed retrieval

For an approved tool-capable model, expose a bounded internal tool similar to:

```text
RetrieveKompressOriginal
```

The tool must:

* Require the current tenant and job context
* Accept only marker hashes issued within the current authorized context
* Enforce retrieval limits
* Return the original with provenance
* Record retrieval
* Reject arbitrary hashes
* Never expose another tenant's content

### Orchestrator-directed retrieval

For models without reliable tool calling, or for high-impact extraction:

* The orchestrator determines that original text is required.
* The orchestrator retrieves the original.
* The orchestrator reconstructs the context.
* The model is re-run or the output is validated against the original.

High-impact extraction must not depend on the model deciding to retrieve critical evidence.

## Retention policy

The current Kompress TTL must be inspected and reconciled with BioStack workflow duration.

Define retention classes such as:

```text
SingleInference
ResearchJob
RetryWindow
HumanReviewSupport
```

At minimum, a retrieval marker must remain valid for:

* The complete inference call
* Validation
* Bounded retries
* Approved escalation
* Job recovery after a transient failure

Do not persist unresolved markers into long-lived research artifacts unless their originals are guaranteed to remain retrievable for the complete required retention period.

Prefer persisting the original in BioStack and treating Kompress storage as an execution cache.

## Failure behavior

Kompress is optional optimization.

When Kompress fails:

```text
Original fits selected model context:
    Send original.

Original does not fit:
    Chunk, retrieve selectively, route to a larger context, or defer.

Never:
    Silently truncate required evidence.
```

Required behavior:

* Compression failure returns or preserves the original.
* Retrieval failure invalidates the compressed-only path.
* Missing original-store confirmation rejects the marker.
* Expired markers trigger BioStack-source recovery where possible.
* Context-overflow failure does not permit silent evidence omission.
* Kompress unavailability does not stop source gathering.
* Kompress unavailability is visible in telemetry.
* No canonical knowledge depends solely on compressed content.

## Compression-aware routing

The context budgeter must estimate:

```text
Raw token count
Expected compressed token count
Protected token count
Retrievable token count
Model context limit
Output-token reserve
Tool-call reserve
Validation reserve
```

The model router may choose among:

```text
Use original with current model
Use lossless compression with current model
Use reversible compression with current model
Chunk the original
Retrieve only relevant passages
Use a model with a larger validated context
Escalate to another approved provider
Defer the task
```

Do not choose aggressive compression merely to force a request into an undersized model.

## Source verification

Every promoted extracted claim must be verifiable against the original source.

For high-impact fields:

```text
Model extraction
        ↓
Reported source location
        ↓
Retrieve original content
        ↓
Deterministic or human verification
        ↓
Candidate evidence record
```

Compressed text may assist discovery.

Original text must support the claim.

## Kompress versioning and provenance

Record:

```text
Kompress service version
Kompress image digest
Compression profile version
Compression configuration
Detected content type
Transforms applied
Original BioStack hash
Retrieval hash
Retrieval expiration
Token counts
Savings
Lossless or lossy
Retrieval events
Bypass reason
Failure reason
```

Do not store private original content in ordinary application logs.

---

# Phase 3: Protect the local-first boundary

The initial integration must not transmit user health or protocol data to ToolUniverse or its downstream APIs.

Permitted sidecar inputs:

* Compound name
* Known compound identifiers
* Research question
* Public disease or pathway name
* Public citation identifier
* Requested evidence categories
* Research freshness date
* Approved source filters

Prohibited sidecar inputs for the first implementation:

* User identity
* Account identifier
* Age
* Sex
* Weight
* Symptoms
* Biomarkers
* Check-ins
* Personal protocol history
* User notes
* Provider information
* Uploaded health documents
* Any directly or indirectly identifying health data

GPU acceleration does not change the privacy boundary.

General scientific documents and public-source content may be processed locally by the GPU worker. User-specific protocol comparisons must continue to occur inside BioStack unless a later, explicitly approved design permits otherwise.

Do not transmit private BioStack data to a hosted fallback merely because the local GPU is unavailable.

Any hosted fallback must receive only data permitted by the existing sidecar privacy contract. A failure of local acceleration must never silently widen the data-sharing boundary.

Local Ollama inference and local Keon Kompress processing preserve BioStack's local-first posture only when both services remain privately reachable and do not forward data to external providers.

The implementation must verify:

* Ollama is operating locally for local-model routes.
* Ollama cloud models are not selected unless explicitly approved.
* Kompress storage remains on approved local or private infrastructure.
* Hosted fallback cannot receive a compressed payload merely because the original appears absent.
* Compression does not change the payload's data classification.
* A Kompress marker must be treated as sensitive because it can reference retrievable original content.
* Model prompts, compressed content, retrieval markers, and model output follow the same privacy policy as the original data.

Never assume that compressed content is anonymized content.

The sidecar gathers general scientific evidence.

BioStack performs user-specific comparison locally, after the reviewed research data has entered BioStack's trusted knowledge layer.

This prevents ToolUniverse and its downstream sources from becoming processors of personal protocol data during the initial implementation.

Local GPU processing is especially valuable for privacy-preserving document extraction, embeddings, reranking, and evidence normalization. However, local execution does not reduce the need for evidence review, source attribution, content controls, or deterministic comparison logic.

---

# Phase 4: Create the BioStack scientific research contract

Create a provider-neutral contract that does not leak ToolUniverse-specific response formats into the Domain or Application layers.

Suggested abstractions:

```csharp
public interface IScientificResearchProvider
{
    Task<ResearchJobHandle> SubmitAsync(
        ScientificResearchRequest request,
        CancellationToken cancellationToken);

    Task<ResearchJobStatus> GetStatusAsync(
        ResearchJobId jobId,
        CancellationToken cancellationToken);

    Task<ScientificResearchArtifact> GetResultAsync(
        ResearchJobId jobId,
        CancellationToken cancellationToken);

    Task CancelAsync(
        ResearchJobId jobId,
        CancellationToken cancellationToken);
}
```

Names may be changed to fit current repository conventions.

## Required request fields

```text
Research request ID
Research subject type
Canonical or candidate subject name
Known identifiers
Requested workflow
Requested evidence categories
Source allowlist
Maximum source age
Maximum execution time
Maximum source count
Correlation ID
Requested-by actor
Purpose
Preferred execution mode
GPU permitted
Hosted fallback permitted
Maximum GPU memory
Maximum model execution time
Approved model profile
Data classification
Task class
Evidence-risk class
Exactness requirement
Required modalities
Required structured-output schema
Minimum model benchmark score
Local inference permitted
Hosted inference permitted
Compression permitted
Compression exactness mode
Maximum input tokens
Required output-token reserve
Required source-location behavior
Cross-check required
```

## Required result fields

```text
Research artifact ID
Provider
Provider version
Workflow version
ToolUniverse version or commit
Execution start and finish
Tools invoked
Arguments with sensitive fields redacted
Source manifest
Raw artifact hashes
Normalized claims
Unresolved ambiguities
Conflicting evidence
Warnings
Partial-result status
Freshness date
Failure details
Execution device
GPU device identifier
GPU model
GPU architecture
CUDA version
Inference framework and version
Model identifier
Model revision
Model artifact hash
Numerical precision
Peak GPU memory
Fallback path used
Fallback reason
Model route decision ID
Selected provider
Selected model
Selected model digest
Routing-policy version
Routing candidates considered
Candidate rejection reasons
Selection rationale
Configured context
Actual context used
Prompt version
Output-schema version
Validation result
Escalation history
Compression execution ID
Compression mode
Original token count
Compressed token count
Kompress retrieval hashes
Original-retrieval events
```

## Execution profile

Add a provider-neutral execution profile.

Suggested shape:

```csharp
public sealed record ScientificExecutionProfile(
    ScientificExecutionMode Mode,
    bool AllowGpu,
    bool AllowCpuFallback,
    bool AllowHostedFallback,
    long? MaximumGpuMemoryBytes,
    TimeSpan MaximumExecutionDuration,
    string? ApprovedModelProfile);
```

The exact type names must follow existing BioStack conventions.

Do not expose CUDA device indexes, framework tensors, model-runtime objects, or ToolUniverse-specific execution types outside the Infrastructure boundary.

## Local inference and compression abstractions

Add provider-neutral abstractions for:

```text
IModelRouter
IInferenceProvider
IModelRegistry
IInferenceOutputValidator
IContextBudgeter
IContextCompressionProvider
IOriginalContextRetriever
```

Infrastructure implementations may include:

```text
OllamaInferenceProvider
KeonKompressProvider
HostedInferenceProvider
```

The Application layer must depend on abstractions rather than Ollama, ToolUniverse, Kompress, CUDA, or hosted-provider types.

---

# Phase 5: Add canonical scientific entities

Do not store the sidecar's Markdown report as the sole knowledge representation.

Store the raw report for review, but normalize research into typed BioStack records.

## Compound identity

```text
Canonical name
Aliases
Parent compound
Salt or formulation
Stereochemistry
PubChem CID
ChEMBL ID
InChIKey
SMILES
Molecular formula
Molecular weight
Identity confidence
Identity conflicts
Identity sources
```

## Study record

```text
Citation
Study type
Publication status
Retraction status
Population
Sample size
Age range
Sex distribution
Condition or research context
Intervention
Comparator
Duration
Endpoints
Results
Limitations
Funding and conflicts
Evidence class
```

## Published exposure regimen

This is a critical first-class entity, not a paragraph hidden inside a summary.

```text
Study arm
Substance
Formulation
Amount
Unit
Route
Frequency
Initiation amount
Escalation step
Escalation interval
Maximum studied amount
Maintenance amount or range
Exposure duration
Population
Reason for escalation
Reason for interruption
Reason for discontinuation
Source location
Extraction confidence
Reviewer status
```

Never collapse initiation, escalation, maximum, and maintenance values into one ambiguous `dose` field.

## Outcome

```text
Outcome name
Outcome type
Measurement
Time point
Result
Effect size
Confidence interval
Statistical significance
Clinical relevance reported by source
Source wording
BioStack interpretation
Limitations
```

## Adverse-event evidence

```text
Event
Severity
Frequency
Study arm
Dose or exposure context
Time to event
Discontinuation relationship
Serious adverse event status
Label warning status
FAERS or spontaneous-report signal
Causality limitations
Source
```

Spontaneous reporting and disproportionality signals must never be represented as proven causation.

## Mechanism and pathway claim

```text
Claim
Target
Pathway
Tissue or system
Evidence class
Direct or inferred relationship
Species
Source
Conflicting evidence
Confidence
```

## Evidence assessment

At minimum distinguish:

* Mechanistic or theoretical
* In-vitro
* Animal
* Human case report
* Human observational
* Controlled human trial
* Systematic review or meta-analysis
* Regulatory label
* Official safety communication
* Community-reported
* Unknown
* Conflicting
* Retracted or superseded

Do not assign evidence strength from source count alone.

---

# Phase 6: Implement bounded research workflows

Start with the following ToolUniverse skills:

1. `tooluniverse-chemical-compound-retrieval`
2. `tooluniverse-literature-deep-research`
3. `tooluniverse-drug-research`
4. `tooluniverse-adverse-event-detection`
5. `tooluniverse-pharmacovigilance`
6. `tooluniverse-systems-biology`
7. `tooluniverse-target-research`

Do not run every skill for every request.

## GPU-assisted processing stages

GPU execution may be used only for approved processing stages.

Recommended initial stages:

### Literature candidate embeddings

Generate local scientific-text embeddings for titles, abstracts, keywords, and approved full-text segments.

Use cases:

* Semantic grouping
* Duplicate detection
* Preprint and published-version matching
* Similar-study discovery
* Topic clustering

### Candidate reranking

Rerank retrieved publications against the original research question after deterministic source filtering.

Reranking must not remove lower-ranked evidence permanently. Preserve the complete candidate inventory and the ranking rationale.

### Structured study extraction

Use an approved local model to propose structured fields such as:

```text
Study population
Sample size
Intervention
Comparator
Initiation amount
Escalation schedule
Maintenance exposure
Maximum exposure
Route
Frequency
Duration
Outcome
Adverse event
Discontinuation condition
Limitation
```

All extracted fields must point back to a source passage or source location.

### PDF and table processing

Use approved local document models for:

* Page layout detection
* Table recognition
* OCR
* Section segmentation
* Caption association
* Footnote association
* Column-order recovery

Preserve the original document and page coordinates needed to reproduce the extraction.

### Evidence classification

A local model may propose an evidence class, but deterministic metadata and human review take precedence.

A model must not promote an animal study into human evidence, a preprint into peer-reviewed evidence, or a spontaneous report into demonstrated causality.

## Initial GPU priority

For the proof of concept, prioritize:

1. Scientific embeddings
2. Literature deduplication
3. Candidate reranking
4. Structured exposure-regimen extraction
5. PDF and table extraction

Defer heavyweight molecular docking, large structure prediction, or other VRAM-intensive scientific models until the GPU capability manifest and benchmark results show that they can run reliably on the actual device.

## Local-model and Kompress execution path

For each model-assisted workflow:

1. Persist the raw source.
2. Calculate the canonical BioStack hash.
3. Assign the task class.
4. Assign the evidence-risk class.
5. Assign the exactness requirement.
6. Separate protected instructions from evidence payload.
7. Classify evidence segments for compression eligibility.
8. Apply Kompress only to eligible segments.
9. Calculate the complete context budget.
10. Route to an approved model.
11. Request structured output.
12. Validate the output schema.
13. Validate identifiers and units deterministically.
14. Resolve all source locations against original text.
15. Retrieve originals when required.
16. Cross-check high-impact fields when required.
17. Escalate only through approved routes.
18. Stage the candidate evidence for review.
19. Record model, routing, compression, and retrieval provenance.

## Recommended initial routing experiments

Benchmark these candidate routes:

### Qwen 3.5 9B

Evaluate for:

* Structured scientific extraction
* JSON Schema compliance
* Tool use
* Evidence classification
* Regimen extraction
* Contradiction analysis

### Gemma 4 12B

Evaluate for:

* Multimodal document understanding
* Table interpretation
* Scientific reasoning
* Source applicability analysis
* Secondary verification

These are candidate roles, not preapproved assignments.

The benchmark results must decide final routing.

## Compression experiments

Benchmark each representative task with:

```text
Original context
Lossless Kompress context
Reversible lossy Kompress context
Chunked original context
Retrieval-augmented original passages
```

Measure:

* Extraction accuracy
* Source-location accuracy
* Numeric-field accuracy
* Unit accuracy
* Missing-evidence rate
* Unsupported-claim rate
* Token reduction
* Latency
* GPU memory
* Retrieval frequency
* Escalation frequency

Do not approve lossy compression for a task class when it materially reduces evidence accuracy.

## Workflow A: Compound identity resolution

1. Normalize user-supplied name.
2. Search approved identity sources.
3. Resolve synonyms.
4. Distinguish parent, salt, formulation, and stereochemistry.
5. Cross-check identifiers.
6. Return multiple candidates when ambiguous.
7. Fail closed rather than selecting an uncertain identity.
8. Require review before canonical identity changes.

## Workflow B: Compound evidence profile

1. Resolve identity.
2. Build search synonyms.
3. Search literature sources.
4. Deduplicate publications.
5. Classify source types.
6. Extract study populations.
7. Extract published exposure regimens.
8. Extract outcomes.
9. Extract adverse events.
10. Extract limitations.
11. Detect conflicting conclusions.
12. Detect retractions or corrections.
13. Normalize claims.
14. Produce raw and structured artifacts.
15. Stage for human review.

## Workflow C: Safety and adverse-event profile

1. Retrieve official labels and safety communications.
2. Retrieve controlled-trial adverse events.
3. Retrieve discontinuation data.
4. Retrieve spontaneous-report signals.
5. Keep source classes separate.
6. Preserve denominator information when available.
7. Preserve absence of denominator information when unavailable.
8. Prevent FAERS signals from becoming incidence estimates.
9. Stage high-impact wording for Clinical Safety review.

## Workflow D: Mechanism and pathway profile

1. Resolve compound targets.
2. Cross-check target relationships.
3. Retrieve pathway membership.
4. Separate experimentally supported relationships from inferred relationships.
5. Record species and tissue.
6. Detect pathway overlap with existing compounds.
7. Present overlap as a research signal, not proof of synergy, safety, or causation.

---

# Phase 7: Integrate with existing intake and review controls

Reuse the existing BioStack intake and promotion lifecycle where technically appropriate.

The expected conceptual lifecycle is:

```text
Research requested
    |
    v
Queued
    |
    v
Resolving identity
    |
    v
Gathering evidence
    |
    v
Normalizing
    |
    v
Pending review
    |
    +--> Deferred
    |
    +--> Rejected
    |
    +--> Approved for promotion
                    |
                    v
          Canonical knowledge promotion
```

Requirements:

* Sidecar output never writes directly to canonical compound tables.
* Raw evidence is immutable after receipt.
* Normalized candidates are versioned.
* Duplicate research requests are detected.
* Failed research can be retried safely.
* Partial results are clearly marked.
* Promotion is idempotent.
* Every promoted claim links to its source manifest.
* Every promotion records reviewer and approval state.
* New source evidence does not overwrite historical evidence.
* Corrections and retractions create new versions and reopen review.
* Knowledge-ingest fences apply to ToolUniverse research.
* Administrative override is explicit, authorized, and receipted.
* Existing provenance and source-lane receipts are reused when possible.

---

# Phase 8: Implement evidence comparison locally

Create a deterministic BioStack domain service that compares a user-recorded amount or proposed protocol entry against reviewed published evidence.

Suggested abstraction:

```csharp
public interface IEvidenceContextComparisonService
{
    EvidenceContextComparison Compare(
        ProtocolExposure exposure,
        ReviewedEvidenceProfile evidence);
}
```

## Comparison outputs

```text
Exact-match study context
Closest studied initiation range
Closest studied maintenance range
Highest studied exposure
Unit-normalized difference
Frequency-normalized difference
Route mismatch
Population applicability limitations
Evidence coverage
Out-of-context flags
Decimal or unit anomaly flags
Source references
```

## Example risk signals

```text
NO_REVIEWED_INITIATION_MATCH
ABOVE_REVIEWED_INITIATION_RANGE
ABOVE_HIGHEST_REVIEWED_EXPOSURE
BELOW_REVIEWED_RANGE
ROUTE_NOT_STUDIED
FREQUENCY_NOT_STUDIED
UNIT_MISMATCH_SUSPECTED
DECIMAL_ERROR_SUSPECTED
EVIDENCE_LIMITED_TO_ANIMALS
EVIDENCE_LIMITED_TO_CASE_REPORTS
CONFLICTING_HUMAN_EVIDENCE
NO_HUMAN_EVIDENCE
```

## Example generated language

Acceptable:

> Reviewed trials initiated participants at 0.5 to 1.0 mg weekly. The entered 12 mg amount is 12 to 24 times that initiation range.

Acceptable:

> No reviewed trial in this evidence packet initiated participants at the entered amount.

Acceptable:

> Human evidence was not found for this route. The available evidence cannot establish how the entered protocol compares.

Acceptable:

> This trial population differed materially from the profile information stored in BioStack, so applicability is uncertain.

Prohibited:

> Start at 0.5 mg.

Prohibited:

> 12 mg will harm you.

Prohibited:

> 1 mg is safe.

The comparison service must be deterministic and must not require an LLM to perform unit math.

---

# Phase 9: Provenance and reproducibility

Every research execution must preserve:

```text
Research request
Canonicalized request
Provider
Provider version
ToolUniverse release or commit
Skill version
Tool names
Tool configuration hashes
Input arguments
Retrieval timestamps
Source identifiers
Source URLs or canonical identifiers
Raw response hashes
Normalized artifact hashes
Extraction model and version, if used
Prompt or extraction schema version, if used
Warnings
Failures
Reviewer decisions
Promotion receipt
Execution device type
GPU model
GPU architecture
GPU driver version
CUDA runtime version
Inference framework version
Model identifier
Model revision
Model artifact hash
Model license
Numerical precision
Quantization configuration
Input token or sequence length
Batch size
Peak VRAM use
GPU execution duration
CPU fallback used
Hosted fallback used
Fallback reason
Model route decision ID
Routing-policy version
Task class
Evidence-risk class
Model provider
Model name
Model digest
Model quantization
Configured context
Runtime context
Prompt version
JSON Schema version
Structured-output settings
Temperature and sampling settings
Random seed, when supported
Validation outcome
Escalation path
Cross-check model
Model disagreement
Compression eligibility decision
Compression exactness mode
Kompress version
Kompress image digest
Compression profile
Transforms applied
BioStack original hash
Kompress retrieval hash
Retrieval expiration
Retrieval events
Original-verification result
Foreman-line or Foreman session reference, when available
Implementation approval history, when available
```

Store large raw artifacts separately if necessary, but preserve content-addressed references.

The system must be able to answer:

* Where did this claim come from?
* Which source passage supports it?
* Which workflow extracted it?
* Which version of ToolUniverse was used?
* Which model was selected?
* Why was this model selected?
* Was content compressed?
* Why was content compressed or not compressed?
* Was original text retrieved?
* Which human approved it?
* What changed since the previous version?
* Has the source been corrected or retracted?
* Can the research packet be reproduced?

GPU acceleration must remain reproducible enough to explain how a candidate artifact was created.

Where GPU kernels or reduced-precision execution are nondeterministic, record that limitation. Do not claim bit-for-bit reproducibility when the selected framework, operation, or hardware cannot provide it.

Scientific-source reproducibility is more important than model-output identity. The system must always preserve the source material and extraction locations needed to independently verify the normalized claim.

The system must be able to explain both decisions:

1. Why this model was selected
2. Why this content was or was not compressed

A final claim must remain traceable through:

```text
Canonical claim
    -> normalized extraction
    -> model execution
    -> routing decision
    -> compressed or original context
    -> original source location
    -> immutable raw artifact
```

---

# Phase 10: Reliability and failure behavior

Expected failures include:

* Source API unavailable
* Authentication failure
* Rate limiting
* Timeout
* Schema drift
* Empty result
* Ambiguous identity
* Conflicting identifiers
* Tool removed or renamed
* Partial literature coverage
* Source content changed
* Invalid units
* Extraction failure
* Sidecar unavailable
* Cancellation
* ToolUniverse upgrade regression
* GPU unavailable
* CUDA initialization failure
* NVIDIA driver mismatch
* CUDA runtime mismatch
* Unsupported GPU architecture
* GPU container passthrough failure
* Model load failure
* Model artifact hash mismatch
* GPU memory exhaustion
* GPU execution timeout
* GPU reset
* Thermal throttling
* Invalid reduced-precision output
* GPU worker unavailable
* Ollama unavailable
* Ollama overloaded
* Model not installed
* Model digest changed
* Model context too small
* Model partially offloaded to CPU
* Model response timeout
* Invalid structured output
* Tool-calling failure
* Unsupported model capability
* Low-confidence extraction
* Missing source locations
* Source-location mismatch
* Cross-check disagreement
* Model-routing dead end
* Kompress unavailable
* Kompress unauthorized
* Kompress store unavailable
* Kompress marker missing
* Kompress marker expired
* Kompress retrieval denied
* Kompress retrieval miss
* Compression larger than original
* Protected content incorrectly marked eligible
* Context still too large after compression
* Foreman-line or Foreman unavailable
* Foreman approval denied
* Warp command approval denied
* Agent runtime loses context
* Session interrupted mid-implementation

Required behavior:

* Distinguish all major failure classes.
* Never convert an empty response into `no evidence exists`.
* Never promote partial research without an explicit partial status.
* Retry only retryable failures.
* Use bounded exponential backoff.
* Respect source rate limits.
* Support cancellation.
* Preserve completed partial work.
* Keep canonical knowledge unchanged when research fails.
* Expose a health endpoint that verifies service readiness without running scientific inference.
* Provide a global disable switch that leaves BioStack's existing knowledge base operational.
* Core scientific retrieval must remain operational without a GPU.
* GPU failure must not bring down the CPU and network worker.
* A CUDA out-of-memory failure must release allocated memory before retry or fallback.
* Automatic batch reduction must be bounded and observable.
* CPU fallback must preserve the same output schema.
* Hosted fallback must not widen the approved privacy boundary.
* GPU-required jobs must fail explicitly when the required capability is unavailable.
* GPU-preferred jobs must record the fallback path.
* GPU health must be observable independently from overall sidecar health.
* A model must not execute when its immutable identity differs from the approved registry entry.
* Invalid structured output must not enter staging as valid evidence.
* Missing source locations must fail high-impact extraction.
* Model disagreement must remain visible.
* The router must not loop indefinitely through models.
* Retry and escalation counts must be bounded.
* Ollama failure must not stop deterministic and retrieval-only work.
* Kompress failure must preserve the original.
* System and developer messages must remain byte-identical.
* Retrieval failure must invalidate compressed-only evidence.
* Context overflow must not silently discard required evidence.
* Hosted fallback must require explicit policy permission.
* Foreman or Warp approval denial must stop the requested sensitive action.
* Foreman unavailability must be disclosed rather than hidden.
* Implementation work must remain resumable after interrupted sessions.

---

# Phase 11: Testing

## Contract tests

* .NET request matches sidecar schema.
* Sidecar response matches BioStack schema.
* Unknown fields are handled safely.
* Required fields cannot disappear silently.
* ToolUniverse upgrades cannot merge without contract validation.

## Identity tests

* Ambiguous names return candidates.
* Salt and parent forms remain distinct.
* Stereoisomers remain distinct.
* Conflicting PubChem and ChEMBL identifiers fail review.
* Synonyms deduplicate correctly.

## Exposure and unit tests

* mg and mcg conversion
* daily and weekly frequency normalization
* amount per administration versus total weekly exposure
* initiation versus maintenance
* escalation interval
* decimal placement
* route mismatch
* body-weight-normalized study values
* missing units
* conflicting source values

Include a test where:

```text
Reviewed initiation range: 0.5 to 1.0 mg weekly
User-recorded amount: 12 mg weekly
```

Expected result:

* `ABOVE_REVIEWED_INITIATION_RANGE`
* 12 to 24 times the reviewed initiation range
* Source citations attached
* No claim that harm is certain
* No recommended personal dose generated

## Evidence tests

* Human trial and animal study remain separate.
* Case report is not promoted as controlled evidence.
* Preprint is labeled.
* Retracted source is blocked.
* Conflicting studies remain visible.
* Lack of evidence is not treated as evidence of safety.
* FAERS signal is not represented as incidence or causation.
* Mechanistic plausibility is not represented as demonstrated outcome.

## Pipeline tests

* Duplicate request handling
* Retry after provider failure
* Idempotent promotion
* Review rejection
* Review deferral
* Approved promotion
* Reopened review after correction
* Knowledge-ingest fence enforcement
* Administrative override receipt
* Raw artifact hash verification

## Privacy and security tests

* No user profile data reaches the sidecar.
* No protocol history reaches ToolUniverse.
* Secrets are absent from logs.
* Arbitrary tool invocation is rejected.
* Non-allowlisted tools are rejected.
* Sidecar cannot write to BioStack's database.
* Sidecar is not publicly reachable.
* Execution limits are enforced.

## GPU capability tests

Test the following conditions:

* Supported NVIDIA GPU detected
* No GPU present
* GPU administratively disabled
* CUDA runtime unavailable
* Driver and runtime incompatibility
* Container GPU passthrough unavailable
* Insufficient VRAM
* CUDA out-of-memory
* Model artifact missing
* Model hash mismatch
* Unsupported model
* Unsupported numerical precision
* GPU worker timeout
* GPU worker cancellation
* GPU worker restart
* CPU fallback
* Hosted fallback allowed
* Hosted fallback prohibited
* GPU-required operation without a GPU
* GPU-preferred operation without a GPU
* GPU memory released after execution
* GPU memory released after failure
* Concurrent job limit enforced
* Model idle unloading
* Sidecar remains healthy after GPU failure

## GPU output contract tests

For every GPU-assisted workflow:

* GPU and CPU paths return the same schema.
* Required evidence fields remain present.
* Source citations remain attached.
* Source passage references remain attached.
* Model-generated content remains marked as extracted or inferred.
* The model output itself is not treated as a scientific source.
* Reduced precision does not exceed approved validation tolerances.
* Fallback does not silently change the evidence classification.
* The selected device and model are captured in provenance.

## GPU benchmark tests

Create a repeatable benchmark corpus using representative BioStack documents.

Measure:

```text
Cold model-load time
Warm inference latency
Documents per minute
Embedding throughput
Reranking throughput
Peak VRAM
Average VRAM
CPU utilization
GPU utilization
Extraction accuracy
Structured-field completeness
Source-location accuracy
Failure rate
```

Run the benchmark for:

* CPU-only execution
* GPU execution
* Approved hosted fallback, when applicable

GPU acceleration should be retained only where it provides a meaningful operational or quality benefit.

## Workstation acceptance test

Provide a script or documented command sequence that verifies:

1. The host detects the NVIDIA GPU.
2. WSL detects the NVIDIA GPU when WSL is used.
3. The container runtime detects the NVIDIA GPU.
4. The Python inference framework reports CUDA availability.
5. A small approved model loads successfully.
6. A representative BioStack extraction completes.
7. Provenance records the GPU and model.
8. GPU memory is released.
9. The same request completes through CPU fallback.
10. Core BioStack operation continues when the GPU worker is stopped.

## Intelligent model-routing tests

Test:

* Deterministic task bypasses all models
* Local model selected when quality floor is met
* Larger local model selected for a higher requirement
* Model lacking structured output is excluded
* Model lacking required modality is excluded
* Model lacking sufficient context is excluded
* Model that does not fit VRAM is excluded
* Unapproved model is excluded
* Model digest mismatch is rejected
* Hosted fallback prohibited
* Hosted fallback explicitly permitted
* Router records rejected candidates
* Router records selection rationale
* Invalid structured output triggers validation failure
* Bounded retry
* Bounded escalation
* No eligible route returns a typed failure
* High-impact extraction requires original-source verification
* Model disagreement reaches review unchanged

## Ollama integration tests

Test:

* Local health check
* Model inventory
* Model digest discovery
* Model preload
* Model keep-alive
* Model unload
* Structured JSON output
* JSON Schema output
* Embeddings
* Tool calling
* Vision input where approved
* Timeout
* Cancellation
* Overload response
* Context-limit handling
* CPU fallback or offload detection
* Model removal during operation
* Model update or digest change

## Keon Kompress policy tests

Test:

* System message is never compressed
* Developer message is never compressed
* System-message caller override is rejected
* Protected schema remains byte-identical
* Protected tool definition remains byte-identical
* Protected authorization context remains byte-identical
* Below-threshold content passes through
* Never-worse guard preserves original
* Lossy output has a retrievable original
* Missing store confirmation rejects the marker
* Cross-tenant retrieval fails
* Cross-job retrieval fails when job isolation applies
* Unknown hash fails
* Expired marker follows recovery policy
* Kompress unavailable returns original
* Retrieval unavailable invalidates compressed-only execution
* BioStack full hash remains distinct from Kompress hash
* No Kompress marker becomes a canonical citation
* No unresolved marker enters promoted knowledge

## Compression-quality tests

For every approved task class, compare:

```text
Original execution
Compressed execution
Original-restored execution
```

Assert:

* Numeric values remain correct
* Units remain correct
* Study arms remain distinct
* Initiation and maintenance remain distinct
* Adverse-event counts remain correct
* Source locations resolve
* Unsupported claims do not increase beyond tolerance
* Compression savings meet the minimum threshold
* Exactness policy is enforced

## Retrieval-tool tests

For tool-capable local models:

* Model can retrieve an issued marker
* Model cannot retrieve an arbitrary marker
* Retrieval is tenant-bound
* Retrieval is job-bound
* Retrieval limit is enforced
* Retrieval is audited
* Retrieved text remains original and byte-identical
* High-impact validation does not depend solely on voluntary model retrieval

## Foreman and Warp execution tests

When Foreman-line or Foreman is available, verify:

* Foreman mediates configured MCP/tool calls.
* Sensitive shell commands require approval.
* Secret-file access is blocked or requires approval.
* Network-sensitive calls are visible.
* Denied actions are not rerouted silently.
* Local audit logs are written.
* Session logs can be referenced in the final report.
* Warp approval behavior is respected.
* Implementation can resume from a documented session state.

When Foreman is unavailable, verify:

* The agent records that Foreman is unavailable.
* Sensitive actions require explicit human approval.
* Manual implementation notes preserve enough detail to resume safely.

## Copy and behavior guards

Add tests preventing unreviewed forms of:

```text
safe dose
recommended dose
correct dose for you
you should take
start taking
increase to
decrease to
titrate to
ideal amount
guaranteed safe
clinically proven for you
```

Do not ban legitimate source quotations or evidence fields merely because they contain dosage language. Guard how BioStack interprets and presents them.

---

# Phase 12: Initial validation corpus

Validate the complete pipeline against compounds with different research characteristics.

## Retatrutide

Tests:

* Human trial discovery
* Initiation and escalation extraction
* Maintenance and maximum exposure distinction
* Adverse-event extraction
* Trial population applicability
* Investigational status
* 12 mg versus trial-initiation comparison
* Embed and rerank trial literature
* Extract initiation and escalation tables
* Compare GPU extraction with manual review
* Confirm that 12 mg remains a deterministic BioStack comparison, not a model-generated judgment

## BPC-157

Tests:

* Weak or limited human evidence
* Animal-heavy evidence
* Compound identity ambiguity
* Unsupported online claims
* Clear evidence-tier separation
* Resistance to influencer-source contamination
* Cluster animal, human, review, and unsupported sources
* Verify that semantic similarity does not blur evidence classes
* Confirm that low-quality online repetition does not become high-confidence evidence

## Metformin

Tests:

* Approved drug
* Rich label and trial evidence
* Multiple indications
* Established adverse events
* Regulatory sources
* Large literature volume
* Deduplication performance
* Benchmark large-literature deduplication and reranking
* Test throughput under a high-volume evidence corpus
* Confirm that label, trial, review, and observational evidence remain separate

## NAD+

Tests:

* Compound and formulation ambiguity
* NAD+, NADH, NMN, and NR distinction
* Route differences
* Mechanistic versus human-outcome evidence
* Supplement and infusion literature separation
* Test synonym and formulation clustering
* Confirm that NAD+, NADH, NMN, and NR are not incorrectly merged by embeddings
* Compare GPU-assisted semantic grouping against canonical identity controls

---

# Phase 13: Delivery sequence

Implement through small, reviewable pull requests.

## PR 1: Discovery, ADR, and execution guardrail setup

* Repository architecture map
* Integration options
* Selected seam
* Data-flow diagram
* Threat model
* Product-boundary decision requirements
* Warp execution notes
* Foreman-line or Foreman availability check
* Foreman or manual approval workflow
* No runtime behavior

## PR 2: Contracts and domain model

* Provider-neutral research contracts
* Scientific evidence entities
* JSON schemas
* Unit tests
* No ToolUniverse dependency

## PR 3: Sidecar skeleton and capability discovery

* Python service
* Health endpoint
* Authentication
* Configuration
* ToolUniverse version pin
* Tool allowlist
* Container hardening
* Host capability inspection
* Optional GPU capability manifest
* CPU-only startup
* GPU-disabled startup
* No production research workflow yet

## PR 4: Optional GPU worker foundation

* GPU worker boundary
* CUDA capability detection
* Approved model registry
* GPU execution modes
* VRAM budgeting
* Concurrency limits
* Model loading and unloading
* CPU fallback
* GPU failure isolation
* GPU acceptance tests
* No scientific claims or canonical promotion

## PR 5: Local inference abstraction and Ollama discovery

* Provider-neutral inference contracts
* Ollama infrastructure adapter
* Local model inventory
* Model digest discovery
* Runtime capability discovery
* Structured-output proof
* Embedding proof
* Tool-calling proof
* Model lifecycle controls
* No scientific production workflow
* No hosted fallback

## PR 6: Intelligent model-routing foundation

* Model capability registry
* Task classes
* Evidence-risk classes
* Routing policy
* Candidate filtering
* Route scoring
* Context budgeting
* Validation contracts
* Bounded retry
* Bounded escalation
* Routing provenance
* Initial Qwen and Gemma benchmarks
* No canonical promotion

## PR 7: Keon Kompress production integration

* Inspect current Kompress deployment and repository
* Define BioStack production contract
* Enforce system and developer bypass
* Tenant and job binding
* Full-hash and retrieval-hash mapping
* Compression exactness modes
* Context eligibility policy
* Retrieval adapter
* Retention policy
* Failure fallback
* Integration tests
* No canonical promotion

## PR 8: Compression-aware model execution

* Context segment classification
* Protected-content handling
* Context-budget integration
* Model-directed retrieval
* Orchestrator-directed retrieval
* Original-source verification
* Compression benchmarks
* High-impact extraction guards
* No direct canonical writes

## PR 9: Compound identity workflow

* Chemical compound retrieval
* Identity normalization
* Raw artifact storage
* Integration tests
* No canonical promotion

## PR 10: Literature and regimen extraction

* Literature research
* Study normalization
* Published exposure regimen extraction
* Outcomes and limitations
* Staged review records

## PR 11: Safety and pathway research

* Adverse-event research
* Pharmacovigilance separation
* Mechanism and pathway extraction
* Source-class safeguards

## PR 12: Existing intake integration

* Queue and job lifecycle
* Retry and dedupe
* Staging
* Review states
* Knowledge-ingest fence
* Promotion receipts

## PR 13: Local evidence comparison

* Deterministic unit normalization
* Range comparison
* Risk signals
* Copy guards
* Retatrutide 12 mg scenario

## PR 14: Hardening and operational documentation

* Threat-model verification
* License and source-term inventory
* ToolUniverse upgrade procedure
* GPU model upgrade procedure
* CUDA and driver compatibility matrix
* Model license inventory
* Model benchmark report
* Model approval registry
* Ollama upgrade procedure
* Ollama rollback procedure
* Keon Kompress version pinning
* Kompress retention and recovery runbook
* Compression-policy documentation
* Routing-policy documentation
* Hosted-escalation policy
* Kill switches
* GPU disable switch
* Ollama disable switch
* Kompress disable switch
* Monitoring
* GPU resource telemetry
* Model telemetry
* Compression and retrieval telemetry
* Foreman or manual execution audit summary
* Failure runbook
* CPU fallback runbook
* Full validation corpus
* CPU, GPU, compressed, and uncompressed benchmark comparison

Do not combine all phases into one large pull request.

---

# Definition of done

The work is complete when:

1. The existing BioStack backend integration seam is documented and justified.
2. ToolUniverse runs in an isolated, version-pinned sidecar.
3. The BioStack Domain and Application layers do not depend on ToolUniverse types.
4. Only approved workflows and tools can execute.
5. No personal health or protocol data is transmitted to the sidecar.
6. Compound identity is resolved and ambiguity is preserved.
7. Published study regimens are extracted into structured fields.
8. Initiation, escalation, maintenance, and maximum exposure remain distinct.
9. Outcomes, adverse events, limitations, and populations are source-linked.
10. Raw evidence and normalized artifacts are hashed and traceable.
11. Research results enter the existing staged review and promotion lifecycle.
12. ToolUniverse cannot write directly to canonical knowledge.
13. User-recorded amounts can be compared deterministically with reviewed evidence.
14. The 12 mg Retatrutide test produces a clear, cited, high-severity evidence-context warning.
15. The system does not invent or select a personal dose.
16. All unit, identity, evidence, pipeline, privacy, security, and copy-guard tests pass.
17. The prior BioStack behavior remains operational when the sidecar is disabled.
18. Product canon and policy documents are reconciled before public release of expanded dosage-context features.
19. The sidecar starts and operates correctly without a GPU.
20. The exact NVIDIA GPU and runtime capabilities are detected rather than assumed.
21. GPU acceleration is available only for explicitly approved workloads.
22. GPU-specific types do not leak into the Domain or Application layers.
23. GPU failure cannot bring down the BioStack backend or CPU retrieval worker.
24. CPU fallback preserves the same scientific research contract.
25. Hosted fallback cannot silently widen the privacy boundary.
26. Every local model is version-pinned, hashed, licensed, and allowlisted.
27. Every GPU-assisted result records device, model, precision, and fallback provenance.
28. GPU memory and concurrency limits are enforced.
29. GPU memory is released after successful and failed execution.
30. The benchmark report demonstrates where GPU use provides meaningful value.
31. Core knowledge intake, review, promotion, and evidence comparison remain functional when GPU acceleration is disabled.
32. Ollama is integrated behind a provider-neutral interface.
33. Installed local models are identified by immutable digest.
34. Model routes are selected from measured capability profiles.
35. Deterministic tasks bypass language models.
36. Structured extraction uses versioned schemas and strict validation.
37. High-impact extractions resolve to original source locations.
38. Model retries and escalation are bounded.
39. Hosted fallback cannot occur without explicit permission.
40. Keon Kompress is integrated through a production-appropriate contract.
41. System and developer messages can never be compressed.
42. Protected policy, schema, tool, and authorization content remains verbatim.
43. Original source content is preserved before compression.
44. BioStack full hashes remain canonical.
45. Kompress hashes are used only for reversible retrieval.
46. Kompress retrieval is tenant-bound and authorized.
47. Kompress failure preserves the original.
48. Expired or missing markers cannot support promoted evidence.
49. No unresolved Kompress marker enters canonical knowledge.
50. Compression is approved only for task classes where benchmarks show acceptable evidence accuracy.
51. Qwen 3.5 9B and Gemma 4 12B are benchmarked rather than assigned roles by assumption.
52. Core research workflows remain operational when Ollama and Kompress are disabled.
53. Foreman-line or Foreman usage is documented, or unavailability is explicitly documented.
54. Sensitive implementation actions were approved through Foreman, Warp, or manual human approval.
55. The final implementation report contains enough operational state for another agent to resume safely.

---

# Expected final report

At completion, provide:

* Architecture summary
* ADR
* Data-flow diagram
* Threat model
* Tool and source allowlist
* Added domain contracts
* Added database entities and migrations
* Research workflow descriptions
* Product-boundary decision record
* Test inventory and results
* Validation results for Retatrutide, BPC-157, Metformin, and NAD+
* Known limitations
* Source licensing or usage restrictions
* Operational runbook
* Upgrade and rollback procedure
* Remaining approval gates
* Exact GPU capability report
* CUDA and driver compatibility report
* Windows, WSL 2, and container GPU configuration, when applicable
* GPU worker architecture
* Approved local model inventory
* Model license inventory
* Model hashes and pinned revisions
* VRAM budget
* GPU concurrency policy
* GPU failure and fallback matrix
* CPU versus GPU benchmark results
* Accuracy comparison for GPU-assisted extraction
* GPU operational runbook
* GPU disable and rollback procedure
* Production portability assessment
* Ollama runtime capability report
* Installed local model inventory
* Model digest and license inventory
* Model benchmark matrix
* Approved task-to-model routing table
* Model-routing policy
* Hosted-escalation policy
* Context-budget policy
* Structured-output validation report
* Qwen 3.5 9B benchmark results
* Gemma 4 12B benchmark results
* Recommended additional models, supported by measured gaps
* Keon Kompress integration architecture
* Kompress production-contract decision
* System-message protection verification
* Compression eligibility matrix
* Compression exactness matrix
* Kompress retention policy
* Hash-identity mapping design
* Compression benchmark results
* Retrieval authorization test results
* Original-source verification results
* Ollama disable and recovery runbook
* Kompress disable and recovery runbook
* Foreman-line or Foreman status
* Foreman or manual approval summary
* Warp execution notes
* Session-resume instructions
* Sensitive-action approval log summary

Do not declare success based only on successful ToolUniverse calls.

Success means BioStack can ingest, review, reproduce, and safely present stronger scientific evidence without losing provenance, local-first protections, deterministic validation, compression safety, model-routing discipline, or human control.

---

# Local intelligence governing principles

The GPU, local models, Keon Kompress, ToolUniverse, Warp, and Foreman-line exist to make BioStack research:

* Faster
* More private
* More economical
* More reproducible
* More capable
* More observable
* Easier to supervise

They must not become:

* Hidden dependencies
* Unrestricted model-execution surfaces
* Substitutes for scientific sources
* Substitutes for deterministic validation
* Substitutes for original evidence
* Substitutes for human review
* Reasons to weaken BioStack's privacy boundary
* Reasons to silently omit evidence
* Reasons to compress protected instructions
* Reasons to trust a model because it is larger
* Reasons to couple BioStack to one workstation, model, vendor, plugin, or terminal environment
* Reasons to bypass human approval

The final authority hierarchy is:

```text
Original scientific source
        ↓
BioStack deterministic contracts
        ↓
BioStack governance and review
        ↓
Model-assisted extraction
        ↓
Compression and hardware optimization
        ↓
Implementation tooling and agent orchestration
```

Never invert this hierarchy.

Build the local intelligence plane so BioStack benefits from available NVIDIA hardware, Ollama models, Keon Kompress, ToolUniverse, Warp, and Foreman-line today without becoming dependent on any of them tomorrow.

