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
    /// R1 remediation: optional path to a local truncation/rollback watermark file (see
    /// <see cref="SpineHeadWatermarkStore"/>). When set, <c>VerifyChainAsync</c> fails closed if
    /// the chain's current head is behind the highest head ever observed at this path — catching
    /// a deleted tail, which leaves no internal gap for the hash-chain walk alone to find.
    ///
    /// This is a LOCAL, same-machine anchor, not an external/off-box one: a holder who can edit
    /// the SQLite file can also edit or delete this one. See <see cref="SpineHeadWatermarkStore"/>
    /// for exactly what is and is not proven. Empty/unset (the default) = disabled, no behavior
    /// change to existing deployments or tests.
    /// </summary>
    public string? WatermarkFilePath { get; set; }
}
