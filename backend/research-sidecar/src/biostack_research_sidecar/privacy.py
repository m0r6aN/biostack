"""Privacy boundary checks for sidecar requests.

Sidecar must not accept personal health or protocol data in the initial integration.

Enforcement layers:
1. Top-level request field allowlist (unknown keys rejected).
2. Nested key denylist for known health/identity fields.
3. Free-text value scanning (subject_name, purpose, identifier values, etc.).
4. subject_name compound-identifier shape (not free prose).
5. known_identifiers key whitelist (public scientific IDs only).
"""

from __future__ import annotations

import re
from typing import Any

# Only these top-level keys may appear on research job submissions.
ALLOWED_TOP_LEVEL_FIELDS = frozenset(
    {
        "research_request_id",
        "research_subject_type",
        "subject_name",
        "known_identifiers",
        "workflow",
        "evidence_categories",
        "source_allowlist",
        "maximum_source_age_days",
        "maximum_execution_time_seconds",
        "maximum_source_count",
        "correlation_id",
        "requested_by_actor",
        "purpose",
        "execution",
        "data_classification",
        "task_class",
        "evidence_risk_class",
        "exactness_requirement",
        "local_inference_permitted",
        "hosted_inference_permitted",
        "compression_permitted",
        "compression_exactness_mode",
        "cross_check_required",
    }
)

ALLOWED_EXECUTION_FIELDS = frozenset(
    {
        "mode",
        "allow_gpu",
        "allow_cpu_fallback",
        "allow_hosted_fallback",
        "maximum_gpu_memory_bytes",
        "maximum_execution_duration_seconds",
        "approved_model_profile",
    }
)

# Public scientific identifier keys only. Values are still value-scanned.
# Keys match sequences.py consumers plus common registry aliases.
ALLOWED_KNOWN_IDENTIFIER_KEYS = frozenset(
    {
        "cid",
        "pubchem",
        "pubchem_cid",
        "chembl",
        "chembl_id",
        "molecule_chembl_id",
        "uniprot",
        "accession",
        "pmid",
        "inchikey",
        "inchi",
        "smiles",
        "cas",
        "cas_rn",
        "drugbank",
        "mesh",
        "rxcui",
        "nct",
    }
)

# Compound / chemical name shape — not free-form health prose.
# Allows common nomenclature: BPC-157, N-acetylcysteine, (S)-ketamine, 5-HTP.
SUBJECT_NAME_MAX_LEN = 128
_SUBJECT_NAME_SHAPE = re.compile(
    r"^[A-Za-z0-9][A-Za-z0-9\s\-\(\)\[\]\.,'+/]{0,127}$"
)
# Identifier values: registry tokens, not sentences.
_IDENTIFIER_VALUE_SHAPE = re.compile(
    r"^[A-Za-z0-9][A-Za-z0-9\s\-\(\)\[\]\.,:+/=]{0,255}$"
)

# Nested key denylist (case-insensitive, separators stripped).
PROHIBITED_REQUEST_FIELDS = frozenset(
    {
        "userid",
        "user_id",
        "accountid",
        "account_id",
        "age",
        "sex",
        "weight",
        "symptoms",
        "biomarkers",
        "checkins",
        "check_ins",
        "protocolhistory",
        "protocol_history",
        "personalnotes",
        "personal_notes",
        "usernotes",
        "user_notes",
        "providerinfo",
        "provider_info",
        "healthdocument",
        "health_document",
        "personid",
        "person_id",
        "email",
        "phone",
        "dob",
        "dateofbirth",
        "date_of_birth",
        "diagnosis",
        "diagnoses",
        "medications",
        "medication",
        "labs",
        "labresult",
        "lab_results",
        "bmi",
        "mrn",
        "medicalrecordnumber",
        "medical_record_number",
        "ssn",
        "socialsecurity",
        "patientid",
        "patient_id",
        "height",
        "bloodpressure",
        "blood_pressure",
        "a1c",
        "hba1c",
        "glucose",
        "creatinine",
        "protocolnotes",
        "protocol_notes",
        "checkin",
        "check_in",
    }
)

# Free-text value scans (subject_name, purpose, known identifier values, etc.).
_HEALTH_VALUE_PATTERNS: tuple[re.Pattern[str], ...] = (
    re.compile(r"\b(mrn|ssn|dob|date of birth)\b", re.IGNORECASE),
    re.compile(r"\b(diagnos(?:is|ed|es)|medication|prescription|lab result|biomarker)\b", re.IGNORECASE),
    re.compile(r"\b(bmi|body mass|blood pressure|a1c|hba1c|glucose|creatinine)\b", re.IGNORECASE),
    re.compile(r"\b(my weight|years old|y/?o\b|patient id|medical record)\b", re.IGNORECASE),
    re.compile(r"\b(check[- ]?in|protocol history|personal notes)\b", re.IGNORECASE),
    re.compile(r"\b\d{3}-\d{2}-\d{4}\b"),  # SSN-like
    re.compile(r"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", re.IGNORECASE),  # email
    re.compile(r"\b(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b"),  # phone-like
)


class PrivacyViolation(ValueError):
    def __init__(self, code: str, message: str, fields: list[str] | None = None) -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.fields = fields or []


def validate_research_payload(payload: dict[str, Any]) -> None:
    """Fail closed if the payload is not public-scientific research input."""
    unknown = sorted(set(payload.keys()) - ALLOWED_TOP_LEVEL_FIELDS)
    if unknown:
        raise PrivacyViolation(
            "unknown_request_fields",
            "Request contains fields outside the public research allowlist.",
            fields=unknown,
        )

    if "execution" in payload and isinstance(payload["execution"], dict):
        exec_unknown = sorted(set(payload["execution"].keys()) - ALLOWED_EXECUTION_FIELDS)
        if exec_unknown:
            raise PrivacyViolation(
                "unknown_execution_fields",
                "Execution profile contains unknown fields.",
                fields=[f"execution.{name}" for name in exec_unknown],
            )

    subject_name = payload.get("subject_name")
    if isinstance(subject_name, str):
        assert_subject_name_shape(subject_name)

    known_ids = payload.get("known_identifiers")
    if isinstance(known_ids, dict):
        assert_known_identifiers(known_ids)

    prohibited = find_prohibited_fields(payload)
    if prohibited:
        raise PrivacyViolation(
            "privacy_boundary_violation",
            "Request contains prohibited personal/health fields.",
            fields=prohibited,
        )

    free_text_hits = scan_free_text_values(payload)
    if free_text_hits:
        raise PrivacyViolation(
            "privacy_value_scan_violation",
            "Request free-text appears to include personal or health data.",
            fields=free_text_hits,
        )


def assert_subject_name_shape(subject_name: str) -> None:
    """Reject free prose; only compound-identifier shapes are accepted."""
    cleaned = subject_name.strip()
    if not cleaned:
        raise PrivacyViolation(
            "subject_name_invalid",
            "subject_name must not be blank.",
            fields=["subject_name"],
        )
    if len(cleaned) > SUBJECT_NAME_MAX_LEN:
        raise PrivacyViolation(
            "subject_name_invalid",
            f"subject_name exceeds {SUBJECT_NAME_MAX_LEN} characters.",
            fields=["subject_name"],
        )
    if not _SUBJECT_NAME_SHAPE.fullmatch(cleaned):
        raise PrivacyViolation(
            "subject_name_invalid",
            "subject_name must be a compound identifier (letters, digits, and "
            "limited chemical-name punctuation only), not free-form text.",
            fields=["subject_name"],
        )
    # Sentence-like free text with a colon / semicolon / question mark is out of charset,
    # but multi-clause prose with only spaces can still slip through — cap token count.
    tokens = cleaned.split()
    if len(tokens) > 8:
        raise PrivacyViolation(
            "subject_name_invalid",
            "subject_name looks like free prose (too many tokens for a compound name).",
            fields=["subject_name"],
        )


def assert_known_identifiers(known_identifiers: dict[Any, Any]) -> None:
    """Only public scientific registry keys; values must look like tokens, not notes."""
    bad_keys: list[str] = []
    bad_values: list[str] = []
    for key, value in known_identifiers.items():
        key_s = str(key).strip().lower()
        path = f"known_identifiers.{key}"
        if key_s not in ALLOWED_KNOWN_IDENTIFIER_KEYS:
            bad_keys.append(path)
            continue
        if not isinstance(value, str):
            bad_values.append(path)
            continue
        cleaned = value.strip()
        if not cleaned or not _IDENTIFIER_VALUE_SHAPE.fullmatch(cleaned):
            bad_values.append(path)
            continue
        if len(cleaned.split()) > 6:
            bad_values.append(path)

    if bad_keys:
        raise PrivacyViolation(
            "known_identifiers_key_not_allowlisted",
            "known_identifiers contains keys outside the public scientific ID allowlist.",
            fields=sorted(bad_keys),
        )
    if bad_values:
        raise PrivacyViolation(
            "known_identifiers_value_invalid",
            "known_identifiers values must be registry tokens, not free-form notes.",
            fields=sorted(bad_values),
        )


def find_prohibited_fields(payload: dict[str, Any], prefix: str = "") -> list[str]:
    found: list[str] = []
    for key, value in payload.items():
        path = f"{prefix}.{key}" if prefix else key
        normalized = key.replace("-", "").replace(" ", "").lower()
        if normalized in PROHIBITED_REQUEST_FIELDS:
            found.append(path)
        if isinstance(value, dict):
            found.extend(find_prohibited_fields(value, path))
        elif isinstance(value, list):
            for index, item in enumerate(value):
                if isinstance(item, dict):
                    found.extend(find_prohibited_fields(item, f"{path}[{index}]"))
    return found


def scan_free_text_values(payload: dict[str, Any], prefix: str = "") -> list[str]:
    """Inspect string values that can carry pasted health content."""
    hits: list[str] = []
    for key, value in payload.items():
        path = f"{prefix}.{key}" if prefix else key
        if isinstance(value, str):
            if _string_looks_like_health_or_identity(value):
                hits.append(path)
        elif isinstance(value, dict):
            hits.extend(scan_free_text_values(value, path))
        elif isinstance(value, list):
            for index, item in enumerate(value):
                if isinstance(item, str) and _string_looks_like_health_or_identity(item):
                    hits.append(f"{path}[{index}]")
                elif isinstance(item, dict):
                    hits.extend(scan_free_text_values(item, f"{path}[{index}]"))
    return hits


def _string_looks_like_health_or_identity(value: str) -> bool:
    text = value.strip()
    if not text:
        return False
    # Keep subject_name short compound names out of false positives: only scan patterns.
    return any(pattern.search(text) for pattern in _HEALTH_VALUE_PATTERNS)
