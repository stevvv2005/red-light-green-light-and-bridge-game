using UnityEngine;

/// <summary>
/// Backward-compatible wrapper for older scenes that still reference the legacy UIManager type.
/// The actual gameplay HUD now lives in <see cref="GameHUDManager"/>.
/// </summary>
public sealed class UIManager : GameHUDManager
{
}
