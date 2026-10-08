namespace BioStack.Infrastructure.Governance;

/// <summary>
/// F3+ chain-head checkpoint configuration. The signing key must not live in the Spine DB.
/// Prefer environment / secret store: <c>SpineCheckpoint__SigningKey</c>.
/// </summary>
public sealed class SpineCheckpointOptions
{
    public const string SectionName = "SpineCheckpoint";

    /// <summary>
    /// UTF-8 secret used for HMAC-SHA256. Empty = checkpoints may still be stored but are
    /// marked <c>unsigned-local</c> and do not claim external anchoring.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// When true and <see cref="SigningKey"/> is set, source is recorded as <c>server-hmac</c>
    /// (operator asserts the key is held outside the device). Default false → <c>local-hmac</c>.
    /// </summary>
    public bool SigningKeyIsServerHeld { get; set; }

    /// <summary>
    /// Create a checkpoint after every N successful Spine appends (0 = disabled).
    /// </summary>
    public int AutoCheckpointEveryNEntries { get; set; } = 25;

    /// <summary>
    /// Background cadence in minutes (0 = disabled). Only creates a checkpoint when the chain
    /// head has advanced since the last checkpoint.
    /// </summary>
    public int CadenceMinutes { get; set; } = 60;

    /// <summary>
    /// R1/H2 remediation: path to a local truncation/rollback watermark file (see
    /// <see cref="SpineHeadWatermarkStore"/>). <c>VerifyChainAsync</c> fails closed if the
    /// chain's current head is behind the highest head ever observed at this path — catching a
    /// deleted tail, which leaves no internal gap for the hash-chain walk alone to find — and now
    /// also if the current head's hash does not match the watermark's recorded hash for that same
    /// sequence number (H2/Finding B: catches a deleted-and-reforged tail, not just a shortened
    /// one).
    ///
    /// H2 (default-on posture): truncation detection is ON by default. When this is left unset,
    /// <see cref="SpineRepository"/> derives a path automatically from the underlying database
    /// connection (see <see cref="SpineHeadWatermarkStore.ResolveDefaultPath"/>) rather than
    /// disabling the check — a fresh install gets protection without an operator having to find
    /// and set this setting first. Set this explicitly only to override that derived location
    /// (e.g. to point it at separate, more durable storage than the database's own directory).
    ///
    /// This is a LOCAL, same-machine anchor, not an external/off-box one: a holder who can edit
    /// the SQLite file can also edit or delete this one. See <see cref="SpineHeadWatermarkStore"/>
    /// for exactly what is and is not proven.
    /// </summary>
    public string? WatermarkFilePath { get; set; }

    /// <summary>
    /// Explicit opt-out of truncation/rollback detection (H2/Finding A). Default <c>false</c> —
    /// detection is ON by default (see <see cref="WatermarkFilePath"/>). Setting this to
    /// <c>true</c> is a deliberate reduced-posture decision: a deleted tail will once again be
    /// silently accepted as an intact chain, reproducing the original R1 failure mode on purpose.
    /// Activation is never silent — <see cref="SpineRepository"/> logs a <c>Warning</c> the first
    /// time this takes effect in a process, naming the setting that caused it, so a reduced
    /// posture shows up in ordinary operational logs instead of requiring an operator to already
    /// know to look for it.
    /// </summary>
    public bool DisableTruncationWatermark { get; set; }
}
