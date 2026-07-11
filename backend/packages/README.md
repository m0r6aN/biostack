# Vendored Keon.Kompress packages

BioStack restores `Keon.Kompress` from this repository-local package source so
clean Linux CI and Docker builds do not depend on a developer-specific
`D:\nuget_store` feed.

The `0.1.0` packages were built on 2026-07-10 from the reviewed
`Keon-Systems/keon-kompress` source snapshot subsequently committed as
`c1c9224`. Later changes through `6577b6b` only alter an Infrastructure XML
comment in the four packaged projects. The package source remains the
authoritative implementation; these artifacts are a pinned distribution copy
for reproducible BioStack builds.

Regenerate with the package repository's normal Release pack process, replace
all four packages together, and update the hashes below before changing the
BioStack package version.

| Package | SHA-256 |
| --- | --- |
| `Keon.Kompress.0.1.0.nupkg` | `559cbce23c4c773cba68f808c301513c8e4bc7af60eeb22a6ce94790902ac4f8` |
| `Keon.Kompress.Compression.0.1.0.nupkg` | `3b2a9f95341b5637c40c1418c0d6395450599fc08de1ea88354abfc070297b57` |
| `Keon.Kompress.Core.0.1.0.nupkg` | `4a484a9f4d62e49e038a8fc8ae902ec3ff5714d75a87a9c4d22ece41d2cc251f` |
| `Keon.Kompress.Infrastructure.0.1.0.nupkg` | `c6daf2766abef79d20767c86ba1e281d3434a967296db76b0dcaeaf391221b3a` |
