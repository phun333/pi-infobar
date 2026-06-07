using System.Diagnostics;
using System.IO;

namespace PiStats.Services;

public sealed class RemoteSyncException : Exception
{
    public RemoteSyncException(string message) : base(message) { }
}

/// <summary>
/// Pull Pi session logs from a remote host over SSH. Windows has no rsync, so
/// we stream a gzip tar of just the *.jsonl files (built remotely with
/// find + tar) and extract it locally — preserving the directory layout.
/// Mirrors the macOS RemoteSync behaviour.
/// </summary>
public static class RemoteSync
{
    private static string SshExe
    {
        get
        {
            var sys = Path.Combine(Environment.SystemDirectory, "OpenSSH", "ssh.exe");
            return File.Exists(sys) ? sys : "ssh";
        }
    }

    private static string TarExe
    {
        get
        {
            var sys = Path.Combine(Environment.SystemDirectory, "tar.exe");
            return File.Exists(sys) ? sys : "tar";
        }
    }

    /// Validate/normalize an SSH port (defaults to 22).
    public static string NormalizedPort(string port)
    {
        var t = (port ?? "").Trim();
        if (t.Length == 0) return "22";
        if (int.TryParse(t, out var n) && n >= 1 && n <= 65535) return n.ToString();
        throw new RemoteSyncException($"Invalid port \"{port}\" — must be 1–65535.");
    }

    /// Expand a leading ~ to the Windows user profile (for local key paths).
    private static string ExpandLocal(string path)
    {
        var p = (path ?? "").Trim();
        if (p.StartsWith("~"))
            p = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + p[1..];
        return p;
    }

    private static List<string> BaseSshArgs(string port, string keyPath, bool batch)
    {
        var args = new List<string>
        {
            "-p", NormalizedPort(port),
            "-o", "ConnectTimeout=5",
            "-o", "StrictHostKeyChecking=accept-new",
        };
        if (batch) { args.Add("-o"); args.Add("BatchMode=yes"); }

        var key = ExpandLocal(keyPath);
        if (!string.IsNullOrWhiteSpace(key))
        {
            args.Add("-i");
            args.Add(key);
        }
        return args;
    }

    // MARK: - Test connection

    public static async Task TestConnectionAsync(string host, string port, string user, string keyPath)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            throw new RemoteSyncException("Host and username are required.");

        var args = BaseSshArgs(port, keyPath, batch: true);
        args.Add($"{user}@{host}");
        args.Add("echo OK");

        var (exit, stderr) = await RunAsync(SshExe, args);
        if (exit != 0)
            throw new RemoteSyncException(stderr.Length > 0 ? stderr : $"SSH failed (code {exit}).");
    }

    // MARK: - Sync

    public static async Task SyncAsync(string host, string port, string user, string keyPath,
                                       string remotePath, string localPath)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
            throw new RemoteSyncException("Remote host and username must be configured in Settings.");

        Directory.CreateDirectory(localPath);

        // Remote: cd into the logs dir, tar only *.jsonl to stdout (gzip).
        // Unquoted remotePath so the remote shell expands a leading ~.
        var remoteCmd =
            $"cd {remotePath} && find . -name '*.jsonl' -print0 | tar --null -czf - -T -";

        var sshArgs = BaseSshArgs(port, keyPath, batch: true);
        sshArgs.Add($"{user}@{host}");
        sshArgs.Add(remoteCmd);

        var sshPsi = NewPsi(SshExe, sshArgs);
        sshPsi.RedirectStandardOutput = true;
        sshPsi.RedirectStandardError = true;

        var tarPsi = NewPsi(TarExe, new[] { "-xf", "-" });
        tarPsi.RedirectStandardInput = true;
        tarPsi.RedirectStandardError = true;
        tarPsi.WorkingDirectory = localPath;

        using var ssh = Process.Start(sshPsi) ?? throw new RemoteSyncException("Could not start ssh.");
        using var tar = Process.Start(tarPsi) ?? throw new RemoteSyncException("Could not start tar.");

        long bytes = 0;
        var sshErrTask = ssh.StandardError.ReadToEndAsync();
        var tarErrTask = tar.StandardError.ReadToEndAsync();

        // Pipe ssh stdout -> tar stdin.
        var pump = Task.Run(async () =>
        {
            try
            {
                var buf = new byte[81920];
                int read;
                var outStream = ssh.StandardOutput.BaseStream;
                var inStream = tar.StandardInput.BaseStream;
                while ((read = await outStream.ReadAsync(buf)) > 0)
                {
                    bytes += read;
                    await inStream.WriteAsync(buf.AsMemory(0, read));
                }
            }
            finally
            {
                tar.StandardInput.Close();
            }
        });

        await pump;
        ssh.WaitForExit();
        tar.WaitForExit();

        var sshErr = (await sshErrTask).Trim();
        var tarErr = (await tarErrTask).Trim();

        if (ssh.ExitCode != 0)
            throw new RemoteSyncException(sshErr.Length > 0 ? sshErr : $"SSH failed (code {ssh.ExitCode}).");

        // tar can complain on an empty archive when there are no logs yet —
        // only treat it as fatal if we actually received data.
        if (tar.ExitCode != 0 && bytes > 0)
            throw new RemoteSyncException(tarErr.Length > 0 ? tarErr : $"Extract failed (code {tar.ExitCode}).");
    }

    // MARK: - Process helpers

    private static ProcessStartInfo NewPsi(string exe, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }

    private static async Task<(int exit, string stderr)> RunAsync(string exe, IEnumerable<string> args)
    {
        var psi = NewPsi(exe, args);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using var p = Process.Start(psi) ?? throw new RemoteSyncException($"Could not start {exe}.");
        var err = await p.StandardError.ReadToEndAsync();
        _ = await p.StandardOutput.ReadToEndAsync();
        p.WaitForExit();
        return (p.ExitCode, err.Trim());
    }
}
