# Evidence

| Parcel | Commit | Command | Result | Claim status |
|---|---|---|---|---|
| Parser | `817f6f3331c2b7c3410da63289c02fb27a98ed74` | Targeted `dotnet test` filter | 7 intended assertion failures | Reproduced: alias boundary, compound collapse, culture/decimal/name/unit defects, and missing PDF cancellation boundary. Regex runtime exploitability not established. |
| Interaction | `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f` | Targeted `dotnet test` filter | 4 intended assertion failures | Reproduced: safety precedence, NeedsReview exposure, canonical self-pair, and non-finite confidence. |
| Evidence/provenance | `2c9d6cabce4bad853a63365c03e55c2fc612cb70` | Targeted `dotnet test` filter | 2 intended assertion failures | Reproduced: failed artifact promotion and synthetic citation acceptance. Endpoint-wide gate coverage remains inconclusive. |
| Sidecar lifecycle | `82295c3f36b412b9917eaf047a70b10ee2a67cdc` | Targeted `pytest` | 2 intended assertion failures; 19 adjacent tests pass | Reproduced: late terminal overwrite and unenforced maximum-source count. |
| Outbound-data boundary | `c3e30a93e64be5a2662bb9040a52105d3dc909c9` | Targeted `dotnet test` and Vitest | 2 intended assertion failures | Reproduced latent/config-dependent OCR raw-byte transmission and unauthenticated frontend relay. Collective is inconclusive because the contracted test project does not exist. No active production exposure was tested. |

All parcel branches have refreshed `main` commit `339f259b1a467034db4f57cf9d774c292f11b53a` as an ancestor. No parcel changed production source.
