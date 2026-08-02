"""GPU capability discovery.

Does not require CUDA Python packages. Uses nvidia-smi when present.
GPU absence is a normal condition, not an error.
"""

from __future__ import annotations

import os
import re
import shutil
import subprocess
from datetime import datetime, timezone

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import GpuCapabilityManifest


def detect_gpu_capability(settings: Settings) -> GpuCapabilityManifest:
    notes: list[str] = []
    if not settings.gpu_enabled:
        notes.append("GPU administratively disabled via BIOSTACK_RESEARCH_GPU_ENABLED=false.")
        return GpuCapabilityManifest(
            gpu_available=False,
            configured_concurrency=settings.max_concurrent_gpu_jobs,
            detection_notes=notes,
        )

    nvidia_smi = shutil.which("nvidia-smi")
    if nvidia_smi is None:
        notes.append("nvidia-smi not found on PATH; treating GPU as unavailable.")
        return GpuCapabilityManifest(
            gpu_available=False,
            configured_concurrency=settings.max_concurrent_gpu_jobs,
            container_gpu_passthrough=_guess_container_passthrough(),
            detection_notes=notes,
        )

    try:
        completed = subprocess.run(
            [
                nvidia_smi,
                "--query-gpu=name,driver_version,memory.total,memory.free,compute_cap",
                "--format=csv,noheader,nounits",
            ],
            check=True,
            capture_output=True,
            text=True,
            timeout=10,
        )
    except (subprocess.SubprocessError, OSError) as exc:
        notes.append(f"nvidia-smi failed: {exc}")
        return GpuCapabilityManifest(
            gpu_available=False,
            configured_concurrency=settings.max_concurrent_gpu_jobs,
            detection_notes=notes,
        )

    line = completed.stdout.strip().splitlines()[0] if completed.stdout.strip() else ""
    if not line:
        notes.append("nvidia-smi returned empty GPU inventory.")
        return GpuCapabilityManifest(
            gpu_available=False,
            configured_concurrency=settings.max_concurrent_gpu_jobs,
            detection_notes=notes,
        )

    parts = [part.strip() for part in line.split(",")]
    # name, driver_version, memory.total, memory.free, compute_cap
    name = parts[0] if len(parts) > 0 else None
    driver = parts[1] if len(parts) > 1 else None
    total_mib = _parse_int(parts[2]) if len(parts) > 2 else None
    free_mib = _parse_int(parts[3]) if len(parts) > 3 else None
    compute = parts[4] if len(parts) > 4 else None

    architecture = None
    if name and re.search(r"Ada", name, re.IGNORECASE):
        architecture = "Ada Lovelace"
    elif name and re.search(r"Ampere", name, re.IGNORECASE):
        architecture = "Ampere"
    elif name and re.search(r"Hopper", name, re.IGNORECASE):
        architecture = "Hopper"

    notes.append("Capability probe uses nvidia-smi only; framework CUDA binding not loaded.")

    return GpuCapabilityManifest(
        gpu_available=True,
        manufacturer="NVIDIA" if name else None,
        exact_model=name,
        architecture=architecture,
        compute_capability=compute,
        total_vram_bytes=total_mib * 1024 * 1024 if total_mib is not None else None,
        available_vram_bytes=free_mib * 1024 * 1024 if free_mib is not None else None,
        nvidia_driver_version=driver,
        cuda_runtime_version=None,
        framework_versions={},
        container_gpu_passthrough=_guess_container_passthrough(),
        supported_precisions=["fp32"],
        supported_local_models=[],
        configured_vram_budget_bytes=None,
        configured_concurrency=settings.max_concurrent_gpu_jobs,
        detection_notes=notes,
        detected_at_utc=datetime.now(timezone.utc),
    )


def _parse_int(value: str) -> int | None:
    try:
        return int(float(value.strip()))
    except (TypeError, ValueError):
        return None


def _guess_container_passthrough() -> bool | None:
    # Best-effort: cgroup / dockerenv hints. None means unknown.
    if os.path.exists("/.dockerenv"):
        return None
    return None
