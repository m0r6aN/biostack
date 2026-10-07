# KEO-74 managed-identity Blob source-acquisition runtime

Status: Implemented for offline review; not deployed and not live-source validated.

## Boundary

This parcel replaces only the source-acquisition artifact persistence boundary. File storage remains the local/test default. Production source acquisition and its separate retention job require Azure Blob storage accessed through `ManagedIdentityCredential`; connection strings, account keys, SAS tokens, lifecycle deletion, database access, and secrets are not supported.

The acquisition inputs remain the exact tracked request, decision, and registry artifacts packaged in the worker image. This parcel does not activate a source, change a source authorization decision, contact a source endpoint during verification, promote evidence, or write canonical data.

## Runtime contract

- `Azure.Storage.Blobs` is pinned to `12.29.1`; `Azure.Identity` is pinned to `1.21.0`.
- Blob names are confined to one configured private container and fixed safe prefix.
- The SDK retry count is zero. The job configuration also sets replica retry limit `0`, parallelism `1`, and completion count `1`.
- One cycle owns a 60-second Blob lease renewed every 20 seconds. Lease renewal loss cancels the run.
- Immutable attempts, tombstones, and quarantine markers use `If-None-Match: *`; an existing immutable object is accepted only when its bytes match exactly.
- Mutable checkpoints, manifests, review queues, and deletes are conditional on the ETag observed by the writer.
- Blob attempts are rejected from metadata when `ContentLength` exceeds 8 MiB; tombstones are rejected above 64 KiB. Accepted objects are then range-read through a hard-capped stream using `If-Match` against the observed ETag.
- Tombstone crash-resume re-reads each residual attempt through that stable bounded path and requires the filename hash, content hash, and tombstone `AttemptSha256` to agree before conditional deletion.
- Integrity quarantine writes a content-free immutable marker before conditionally deleting only the suspect object. Suspect bytes and object names are never copied into quarantine, and retention continues with later items while aggregating failed and quarantined counts.
- File retention manually walks directories without recursive-follow behavior, rejects every nested reparse point, and rechecks canonical root containment immediately before reads and deletes.
- Retention writes the immutable tombstone before conditionally deleting the exact attempt and checkpoint observations.
- Production candidate and receipt retention are both exactly 30 days.
- `SourceAcquisitionRetention` is database-free and source-free. It scans only artifact storage and never resolves an acquisition adapter.

## Azure deployment contract

`infra/azure/source-acquisition-jobs.bicep` creates a hardened StorageV2 account, private container, Blob private endpoint, a manual acquisition Container App Job, a scheduled retention Container App Job, system-assigned identities, and container-scoped storage role assignments. It binds an existing private ACR in the deployment resource group as a managed-identity registry for both jobs and grants each system identity the built-in `AcrPull` role at ACR scope only. The template fails unless ACR public network access and the admin user are disabled; no registry credentials or ACR admin access are emitted. Acquisition remains manual because every authorized refresh must supply a distinct stable cycle ID; a static schedule would incorrectly reuse one cycle forever. The custom writer role includes Blob-object operations but explicitly excludes container write/delete. Clint receives the built-in Blob Data Reader role scoped to the container.

The template does not create ACR network infrastructure. Deployment must supply existing resource IDs for an approved ACR `registry` private endpoint, the `privatelink.azurecr.io` private DNS zone, that zone's VNet link, and the current Container Apps managed-environment infrastructure subnet. Deployment fails closed unless the private endpoint targets the declared ACR, the endpoint and DNS link use the managed environment's VNet, the existing environment reports the supplied subnet, the private connection is approved, and DNS registration is disabled.

The template disables public Blob access, shared-key authorization, public network access, and storage lifecycle deletion. The supplied Container Apps environment must already have private routing to the supplied private-endpoint subnet/VNet; the template links its private Blob DNS zone to that VNet.

`workerImage` must start with the declared ACR login server, include a non-empty repository, and end in `@sha256:` plus exactly 64 hexadecimal characters. Tagged images and non-hex digest-shaped strings are rejected. The tracked workflow gives OIDC only to the manual publish job; pull-request and push verification have read-only contents permission. Verification runs on `ubuntu-24.04`. Publication can run only on a self-hosted Linux x64 runner carrying the dedicated `biostack-private-acr` label, and it rejects ACR DNS results outside RFC1918 space before login or push. All actions are pinned to 40-character commits, the SDK is exact-version pinned, and Bicep installation is pinned to `v0.41.2`.

Residual Low: `ubuntu-24.04` is a maintained runner image rather than an immutable machine image, its preinstalled Azure CLI and Docker versions may drift, and the dedicated self-hosted publisher's operating system and Docker/Azure CLI servicing remain operator-controlled. The pinned actions, SDK, Bicep compiler, container bases, private-DNS preflight, and manual-only publish boundary limit that drift without introducing a new runner-image supply chain in this parcel.

The container bases were resolved from the official Microsoft Container Registry on 2026-07-26:

- `mcr.microsoft.com/dotnet/sdk:10.0.100@sha256:c7445f141c04f1a6b454181bd098dcfa606c61ba0bd213d0a702489e5bd4cd71`
- `mcr.microsoft.com/dotnet/runtime:10.0.0@sha256:d13bea17080a4fea1a7295a4fe29240123b1bf955a78ae08480d07bdf09496db`

The workflow action tag refs were resolved from their official GitHub repositories on 2026-07-26: `actions/checkout` v4 at `11d5960a326750d5838078e36cf38b85af677262`, `actions/setup-dotnet` v4 at `67a3573c9a986a3f9c594539f4ab511d57bb3ce9`, and `Azure/login` v2 at `1384c340ab2dda50fed2bee3041d1d87018aa5e8`.

## Configuration

Required production settings are emitted by Bicep:

- `Worker__RunMode=SourceAcquisition` or `SourceAcquisitionRetention`
- `Worker__SourceAcquisitionStorageProvider=AzureBlob`
- `Worker__SourceAcquisitionBlobServiceUri`
- `Worker__SourceAcquisitionBlobContainerName`
- `Worker__SourceAcquisitionBlobPrefix`
- `Worker__SourceAcquisitionCandidateRetentionDays=30`
- `Worker__SourceAcquisitionReceiptRetentionDays=30`

The acquisition job additionally supplies the exact three packaged input paths, a caller-owned cycle ID, and the public PubMed tool/contact values. No credentials are configuration values.

Required deployment-time private-network evidence:

- `acrPrivateEndpointResourceId`
- `acrPrivateDnsZoneResourceId` for exactly `privatelink.azurecr.io`
- `acrPrivateDnsVnetLinkResourceId`
- `containerAppsInfrastructureSubnetId`, matching the existing managed environment

## Verification

Run without source or Azure endpoint access:

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter "FullyQualifiedName~SourceAcquisitionStorageTests|FullyQualifiedName~SourceAcquisitionRuntimeTests" --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers --logger "trx;LogFileName=knowledge-worker-full.trx"
rtk proxy docker build --file backend/KnowledgeWorker.Dockerfile --tag biostack-knowledge-worker:keo74-repair .
rtk proxy az bicep build --file infra/azure/source-acquisition-role.bicep
rtk proxy az bicep build --file infra/azure/source-acquisition-jobs.bicep
rtk git diff --check
```

Offline deterministic coverage includes lease-loss cancellation, collision-byte non-disclosure and marker-first deletion, oversized/ETag-race rejection, crash-resume replacement quarantine, per-item continuation, and nested directory-link rejection where the host supports link creation. Azurite and live Azure were not used. Production deployment, ACR/role propagation, private-network reachability, Blob lease behavior against Azure, and real source acquisition remain explicit operator-owned gates. Independent runtime and infrastructure re-reviews closed with no blocking findings. Azure-specific collision/marker-order fault injection remains a documented Low test-depth gap until the operator-owned live preflight.

Exact remaining operator prerequisite: provision and label a trusted self-hosted Linux x64 publisher with private VNet/DNS reachability; provide an approved ACR private endpoint and `privatelink.azurecr.io` VNet link; and confirm the current Container Apps environment infrastructure subnet is on that reachable VNet. Public ACR access must remain disabled.

Final offline evidence on 2026-07-26: storage tests `14/14`, runtime tests `16/16`, and the full KnowledgeWorker suite `864/864` passed with `0` failed and `0` skipped; the full result is captured at `TestResults/keo74-repair-final5/knowledge-worker-full-final5.trx`. Both Bicep templates compiled, the final pinned container build passed as non-root user `app`, all three required research inputs were present, and `appsettings.json` was absent from the image.
