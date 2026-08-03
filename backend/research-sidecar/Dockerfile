FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

WORKDIR /app

# Install dependencies separately for Docker layer caching.
# TOOLUNIVERSE_EXTRA=tooluniverse installs the pinned base package (not [all]).
ARG TOOLUNIVERSE_EXTRA=tooluniverse
COPY pyproject.toml uv.lock ./

RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync \
      --locked \
      --no-dev \
      --extra ${TOOLUNIVERSE_EXTRA} \
      --no-install-project

COPY . .

RUN --mount=type=cache,target=/root/.cache/uv \
    uv sync \
      --locked \
      --no-dev \
      --extra ${TOOLUNIVERSE_EXTRA}

ENV PATH="/app/.venv/bin:$PATH"

RUN addgroup --system biostack \
    && adduser --system --ingroup biostack biostack \
    && chown -R biostack:biostack /app

USER biostack

EXPOSE 8080

CMD ["python", "-m", "biostack_research_sidecar"]
