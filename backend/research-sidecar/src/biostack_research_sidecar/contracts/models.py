"""BioStack-owned research contracts."""

from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Literal
from uuid import uuid4

from pydantic import BaseModel, Field, field_validator

WorkflowId = Literal[
    "resolve_compound_identity",
    "research_compound_evidence",
    "research_published_regimens",
    "research_adverse_events",
    "research_mechanisms_and_targets",
    "research_pathways",
    "refresh_evidence_packet",
]

ALLOWED_WORKFLOWS: frozenset[str] = frozenset(
    {
        "resolve_compound_identity",
        "research_compound_evidence",
        "research_published_regimens",
        "research_adverse_events",
        "research_mechanisms_and_targets",
        "research_pathways",
        "refresh_evidence_packet",
    }
)

ExecutionMode = Literal[
    "auto",
    "gpu_preferred",
    "gpu_required",
    "cpu_only",
    "hosted_fallback_allowed",
]

EvidenceRiskClass = Literal["low", "medium", "high"]
ExactnessMode = Literal[
    "verbatim_required",
    "lossless_only",
    "reversible_lossy_allowed",
    "summary_candidate_only",
    "compression_prohibited",
]


class ResearchJobStatusCode(str, Enum):
    QUEUED = "queued"
    RESOLVING_IDENTITY = "resolving_identity"
    GATHERING_EVIDENCE = "gathering_evidence"
    NORMALIZING = "normalizing"
    PENDING_REVIEW = "pending_review"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"
    PARTIAL = "partial"
    REJECTED_BY_POLICY = "rejected_by_policy"


class ScientificExecutionProfile(BaseModel):
    mode: ExecutionMode = "auto"
    allow_gpu: bool = True
    allow_cpu_fallback: bool = True
    allow_hosted_fallback: bool = False
    maximum_gpu_memory_bytes: int | None = None
    maximum_execution_duration_seconds: int = 600
    approved_model_profile: str | None = None


class ScientificResearchRequest(BaseModel):
    """Inbound research request. Must not contain user health PII fields."""

    research_request_id: str = Field(default_factory=lambda: str(uuid4()))
    research_subject_type: str = "compound"
    subject_name: str = Field(..., min_length=1, max_length=128)
    known_identifiers: dict[str, str] = Field(default_factory=dict)
    workflow: WorkflowId
    evidence_categories: list[str] = Field(default_factory=list)
    source_allowlist: list[str] = Field(default_factory=list)
    maximum_source_age_days: int | None = None
    maximum_execution_time_seconds: int = 600
    maximum_source_count: int = 50
    correlation_id: str = Field(default_factory=lambda: str(uuid4()))
    requested_by_actor: str = "biostack-system"
    purpose: str = "compound_research"
    execution: ScientificExecutionProfile = Field(default_factory=ScientificExecutionProfile)
    data_classification: str = "public_scientific"
    task_class: str | None = None
    evidence_risk_class: EvidenceRiskClass = "medium"
    exactness_requirement: ExactnessMode = "verbatim_required"
    local_inference_permitted: bool = True
    hosted_inference_permitted: bool = False
    compression_permitted: bool = True
    compression_exactness_mode: ExactnessMode = "compression_prohibited"
    cross_check_required: bool = False

    @field_validator("subject_name")
    @classmethod
    def _validate_subject_name(cls, value: str) -> str:
        # Shape enforcement lives in privacy.assert_subject_name_shape; pydantic re-checks
        # length after strip so blank/whitespace cannot pass model_validate alone.
        from biostack_research_sidecar.privacy import assert_subject_name_shape

        cleaned = value.strip()
        assert_subject_name_shape(cleaned)
        return cleaned

    @field_validator("known_identifiers")
    @classmethod
    def _validate_known_identifiers(cls, value: dict[str, str]) -> dict[str, str]:
        from biostack_research_sidecar.privacy import assert_known_identifiers

        assert_known_identifiers(value)
        # Normalize keys to lowercase for stable sequence lookups.
        return {str(k).strip().lower(): str(v).strip() for k, v in value.items()}


class ResearchJobHandle(BaseModel):
    job_id: str
    research_request_id: str
    workflow: WorkflowId
    status: ResearchJobStatusCode
    submitted_at_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    correlation_id: str


class ResearchJobStatus(BaseModel):
    job_id: str
    research_request_id: str
    workflow: WorkflowId
    status: ResearchJobStatusCode
    progress_message: str | None = None
    partial: bool = False
    error_code: str | None = None
    error_message: str | None = None
    submitted_at_utc: datetime
    updated_at_utc: datetime
    finished_at_utc: datetime | None = None
    correlation_id: str


class SourceManifestEntry(BaseModel):
    source_id: str
    source_name: str
    source_url: str | None = None
    retrieved_at_utc: datetime | None = None
    content_hash: str | None = None
    license: str | None = None


class NormalizedClaim(BaseModel):
    claim_id: str
    claim_type: str
    text: str
    evidence_class: str | None = None
    source_ids: list[str] = Field(default_factory=list)
    source_locations: list[str] = Field(default_factory=list)
    confidence: float | None = None
    review_status: str = "candidate"


class ScientificResearchArtifact(BaseModel):
    research_artifact_id: str
    job_id: str
    research_request_id: str
    provider: str = "biostack-research-sidecar"
    provider_version: str
    workflow: WorkflowId
    workflow_version: str = "0.1.0"
    tooluniverse_version: str | None = None
    status: ResearchJobStatusCode
    partial: bool = False
    started_at_utc: datetime
    finished_at_utc: datetime | None = None
    tools_invoked: list[str] = Field(default_factory=list)
    source_manifest: list[SourceManifestEntry] = Field(default_factory=list)
    raw_artifact_hashes: list[str] = Field(default_factory=list)
    normalized_claims: list[NormalizedClaim] = Field(default_factory=list)
    unresolved_ambiguities: list[str] = Field(default_factory=list)
    conflicting_evidence: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    failure_details: str | None = None
    execution_device: str = "cpu"
    model_route_decision_id: str | None = None
    selected_model: str | None = None
    selected_model_digest: str | None = None
    compression_execution_ids: list[str] = Field(default_factory=list)
    provenance: dict[str, Any] = Field(default_factory=dict)


class CancelJobResponse(BaseModel):
    job_id: str
    status: ResearchJobStatusCode
    message: str


class GpuCapabilityManifest(BaseModel):
    gpu_available: bool
    manufacturer: str | None = None
    exact_model: str | None = None
    architecture: str | None = None
    compute_capability: str | None = None
    total_vram_bytes: int | None = None
    available_vram_bytes: int | None = None
    nvidia_driver_version: str | None = None
    cuda_runtime_version: str | None = None
    framework_versions: dict[str, str] = Field(default_factory=dict)
    container_gpu_passthrough: bool | None = None
    supported_precisions: list[str] = Field(default_factory=list)
    supported_local_models: list[str] = Field(default_factory=list)
    configured_vram_budget_bytes: int | None = None
    configured_concurrency: int = 1
    detection_notes: list[str] = Field(default_factory=list)
    detected_at_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))


class ModelCapabilityProfile(BaseModel):
    provider: str
    canonical_model_name: str
    model_digest: str | None = None
    quantization: str | None = None
    advertised_context: int | None = None
    validated_context: int | None = None
    structured_output_support: bool | None = None
    tool_calling_support: bool | None = None
    embedding_support: bool | None = None
    approval_status: str = "candidate"
    known_weaknesses: list[str] = Field(default_factory=list)


class InferenceCapabilityManifest(BaseModel):
    ollama_reachable: bool
    ollama_base_url: str
    ollama_version: str | None = None
    local_inference_enabled: bool
    hosted_fallback_enabled: bool
    models: list[ModelCapabilityProfile] = Field(default_factory=list)
    detection_notes: list[str] = Field(default_factory=list)
    detected_at_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
