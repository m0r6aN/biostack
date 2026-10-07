FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim@sha256:e5b65587bce7de595f299855d7385fe7fca39b8a74baa261ba1b7147afa78e58 AS builder

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

WORKDIR /app

# Install dependencies separately for Docker layer caching. The production
# default deliberately omits provider SDKs; the optional ToolUniverse source
# contract remains available for a separately governed future image.
ARG INCLUDE_TOOLUNIVERSE=false
COPY pyproject.toml uv.lock ./

RUN --mount=type=cache,target=/root/.cache/uv \
    case "${INCLUDE_TOOLUNIVERSE}" in \
      false) uv sync --locked --no-dev --no-install-project ;; \
      true) uv sync --locked --no-dev --extra tooluniverse --no-install-project ;; \
      *) echo "INCLUDE_TOOLUNIVERSE must be true or false" >&2; exit 2 ;; \
    esac

COPY README.md ./
COPY src ./src

RUN --mount=type=cache,target=/root/.cache/uv <<'P01_BUILD'
set -eu
cat > /tmp/p01-build-backend-requirements.txt <<'P01_REQUIREMENTS'
# P01_BUILD_BACKEND_REQUIREMENTS_BEGIN
hatchling==1.32.0 \
    --hash=sha256:0bdbde4a52b06c37e3eca395f85a762bf0ef06fe374fd8ae429dc6be10230f5f \
    --hash=sha256:0e17c9c3b9aa7c625acc8d0f5b622f107d5049af9ecf5ada4de1aada5be7cdbc
packaging==26.3 \
    --hash=sha256:94edc256424af38762eb31306eed28beb9f0efc50a8837492c9d6fd6004aed79 \
    --hash=sha256:d7193f7c8e4e93f444fde0262bf90af30e16fa0ad0ad44cb553c87339b23cd1c
pathspec==1.1.1 \
    --hash=sha256:17db5ecd524104a120e173814c90367a96a98d07c45b2e10c2f3919fff91bf5a \
    --hash=sha256:a00ce642f577bf7f473932318056212bc4f8bfdf53128c78bbd5af0b9b20b189
pluggy==1.6.0 \
    --hash=sha256:7dcc130b76258d33b90f61b658791dede3486c3e6bfb003ee5c9bfb396dd22f3 \
    --hash=sha256:e920276dd6813095e9377c0bc5566d94c932c33b27a3e3945d8389c374dd4746
tomlkit==0.15.1 \
    --hash=sha256:177a05aece5a8ca5266fd3c448abb47b8d352f09d477d3ca8332db4d89b24304 \
    --hash=sha256:e25bbf38843005246210a12982776f27f99cb9be67160e14434d0c0d21ee1e97
trove-classifiers==2026.6.1.19 \
    --hash=sha256:ab4c4ec93cc4a4e7815fa759906e05e6bb3f2fbd92ea0f897288c6a43efd15b3 \
    --hash=sha256:c5132b4b61a829d11cfbd2d72e97f20a45ed6edb95e45c5efdeb5e00836b2745
# P01_BUILD_BACKEND_REQUIREMENTS_END
P01_REQUIREMENTS
uv venv /tmp/p01-build-backend-venv --python /usr/local/bin/python
uv pip install \
    --python /tmp/p01-build-backend-venv/bin/python \
    --no-deps \
    --only-binary=:all: \
    --require-hashes \
    --requirement /tmp/p01-build-backend-requirements.txt
UV_NO_INDEX=1 /tmp/p01-build-backend-venv/bin/python -m hatchling build \
    --target wheel \
    --directory /tmp/p01-dist
uv pip install \
    --python /app/.venv/bin/python \
    --no-deps \
    --no-index \
    /tmp/p01-dist/biostack_research_sidecar-0.1.0-py3-none-any.whl
rm -rf /tmp/p01-build-backend-venv /tmp/p01-build-backend-requirements.txt /tmp/p01-dist
P01_BUILD

FROM python:3.12.12-slim-bookworm@sha256:593bd06efe90efa80dc4eee3948be7c0fde4134606dd40d8dd8dbcade98e669c AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PATH="/app/.venv/bin:$PATH"

WORKDIR /app

COPY --from=builder --chown=0:0 /app /app

RUN addgroup --system biostack \
    && adduser --system --ingroup biostack biostack \
    && rm -rf \
      /usr/local/lib/python3.12/ensurepip \
      /usr/local/lib/python3.12/site-packages/pip \
      /usr/local/lib/python3.12/site-packages/pip-*.dist-info \
      /app/.venv/lib/python3.12/site-packages/pip \
      /app/.venv/lib/python3.12/site-packages/pip-*.dist-info \
    && rm -f \
      /usr/local/bin/pip \
      /usr/local/bin/pip3 \
      /usr/local/bin/pip3.12 \
      /app/.venv/bin/pip \
      /app/.venv/bin/pip3 \
      /app/.venv/bin/pip3.12 \
    && chmod -R go-w /app

USER biostack

EXPOSE 8080

CMD ["python", "-m", "biostack_research_sidecar"]
