# Validates host GPU visibility for BioStack research development.
# Does not install drivers or pull models.

$ErrorActionPreference = "Continue"

Write-Host "=== BioStack GPU path validation ===" -ForegroundColor Cyan

Write-Host "`n[1] Host nvidia-smi"
nvidia-smi --query-gpu=name,driver_version,memory.total,compute_cap --format=csv

Write-Host "`n[2] WSL nvidia-smi (if available)"
wsl -e nvidia-smi --query-gpu=name,driver_version,memory.total,compute_cap --format=csv 2>&1

Write-Host "`n[3] Docker runtime"
docker info 2>&1 | Select-String -Pattern "Runtimes|nvidia|Operating System|Total Memory"

Write-Host "`n[4] Optional CUDA container probe (skipped if image pull undesired)"
Write-Host "Run manually when approved:"
Write-Host "  docker run --rm --gpus all nvidia/cuda:12.6.0-base-ubuntu22.04 nvidia-smi"

Write-Host "`n[5] Ollama"
ollama --version
ollama list

Write-Host "`nDone. Raise Docker Desktop memory before GPU containers if Total Memory is under ~16GB." -ForegroundColor Yellow
