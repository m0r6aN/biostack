"""Entry point: python -m biostack_research_sidecar"""

from __future__ import annotations

import uvicorn

from biostack_research_sidecar.config import get_settings


def main() -> None:
    settings = get_settings()
    uvicorn.run(
        "biostack_research_sidecar.app:create_app",
        factory=True,
        host=settings.host,
        port=settings.port,
        log_level=settings.log_level.lower(),
    )


if __name__ == "__main__":
    main()
