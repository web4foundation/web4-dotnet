namespace Web4.WebSocket;

/// <summary>
/// Calculating diffs requires two snapshots - a "before" and an "after."
/// The before-snapshot can either reuse the after-snapshot of the previous diff,
/// (which requires more RAM per connection), or it can recapture the snapshot
/// just before any state changes (which requires more CPU per connection).
/// The defaults are to use `Retain` for WebAssembly and `Recapture` for WebSockets.
/// </summary>
public enum SnapshotStrategy
{
    /// <summary>
    /// Reuse the after-snapshot as the before-snapshot as preparation for the next diff 
    /// (requires more RAM per connection).
    /// </summary>
    Retain,
    /// <summary>
    /// Recapture the before-snapshot just before any state changes
    /// (requires more CPU per connection).
    /// </summary>
    Recapture
}