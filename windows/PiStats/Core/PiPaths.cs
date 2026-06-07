using System.IO;

namespace PiStats.Core;

/// <summary>Filesystem locations for Pi data and the Windows cache.</summary>
public static class PiPaths
{
    public static string Home =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// Local Pi session logs: %USERPROFILE%\.pi\agent\sessions
    public static string LocalSessionsDir =>
        Path.Combine(Home, ".pi", "agent", "sessions");

    /// Where remote-synced logs land (mirrors the macOS layout).
    public static string RemoteSessionsDir =>
        Path.Combine(Home, ".pi", "remote-agent-sessions");

    public static string LocalCacheFile =>
        Path.Combine(Home, ".pi", "pi-infobar-windows-cache.json");

    public static string RemoteCacheFile =>
        Path.Combine(Home, ".pi", "pi-infobar-windows-remote-cache.json");
}
