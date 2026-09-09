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

COPY . .

RUN --mount=type=cache,target=/root/.cache/uv \
    case "${INCLUDE_TOOLUNIVERSE}" in \
      false) uv sync --locked --no-dev ;; \
      true) uv sync --locked --no-dev --extra tooluniverse ;; \
      *) echo "INCLUDE_TOOLUNIVERSE must be true or false" >&2; exit 2 ;; \
    esac

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
