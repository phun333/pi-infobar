import Foundation

enum RemoteSyncError: Error, LocalizedError {
    case sshFailed(String)
    case processFailed(Int32, String)

    var errorDescription: String? {
        switch self {
        case .sshFailed(let msg):
            return "SSH connection failed: \(msg)"
        case .processFailed(let code, let msg):
            return "Sync failed (code \(code)): \(msg)"
        }
    }
}

struct RemoteSync {
    static func sync(
        host: String,
        port: String,
        user: String,
        keyPath: String,
        remotePath: String,
        localPath: URL
    ) async throws {
        // Create the local directory if it doesn't exist
        try FileManager.default.createDirectory(at: localPath, withIntermediateDirectories: true)

        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/rsync")

        // Expand key path if it contains ~
        let expandedKeyPath = keyPath.replacingOccurrences(of: "~", with: FileManager.default.homeDirectoryForCurrentUser.path)

        // Format port
        let sshPort = port.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? "22" : port

        // Build SSH option string:
        // -o ConnectTimeout=5: Wait at most 5 seconds for connection
        // -o StrictHostKeyChecking=accept-new: Automatically accept new host keys
        var sshCommand = "ssh -p \(sshPort) -o ConnectTimeout=5 -o StrictHostKeyChecking=accept-new"
        if !expandedKeyPath.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            sshCommand += " -i \"\(expandedKeyPath)\""
        }

        // Format remote path with trailing slash for proper rsync directory copy
        var formattedRemotePath = remotePath
        if !formattedRemotePath.hasSuffix("/") {
            formattedRemotePath += "/"
        }

        // Build arguments:
        // rsync -avz -e "ssh ..." --include="*/" --include="*.jsonl" --exclude="*" user@host:remotePath/ localPath/
        let remoteSrc = "\(user)@\(host):\(formattedRemotePath)"
        let arguments = [
            "-avz",
            "-e", sshCommand,
            "--include=*/",
            "--include=*.jsonl",
            "--exclude=*",
            remoteSrc,
            localPath.path
        ]

        process.arguments = arguments

        let errorPipe = Pipe()
        process.standardError = errorPipe

        let outputPipe = Pipe()
        process.standardOutput = outputPipe

        try process.run()
        process.waitUntilExit()

        let status = process.terminationStatus
        if status != 0 {
            let errorData = errorPipe.fileHandleForReading.readDataToEndOfFile()
            let errorString = String(data: errorData, encoding: .utf8) ?? "Unknown rsync error"
            throw RemoteSyncError.processFailed(status, errorString.trimmingCharacters(in: .whitespacesAndNewlines))
        }
    }

    // Function to test the SSH connection (runs ssh command to check if it succeeds)
    static func testConnection(
        host: String,
        port: String,
        user: String,
        keyPath: String
    ) async throws {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/ssh")

        let expandedKeyPath = keyPath.replacingOccurrences(of: "~", with: FileManager.default.homeDirectoryForCurrentUser.path)
        let sshPort = port.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? "22" : port

        var arguments = [
            "-p", sshPort,
            "-o", "ConnectTimeout=5",
            "-o", "StrictHostKeyChecking=accept-new",
            "-o", "BatchMode=yes" // ensures it doesn't prompt for password/interactive input
        ]

        if !expandedKeyPath.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            arguments.append(contentsOf: ["-i", expandedKeyPath])
        }

        arguments.append("\(user)@\(host)")
        arguments.append("echo OK") // command to execute

        process.arguments = arguments

        let errorPipe = Pipe()
        process.standardError = errorPipe
        let outputPipe = Pipe()
        process.standardOutput = outputPipe

        try process.run()
        process.waitUntilExit()

        let status = process.terminationStatus
        if status != 0 {
            let errorData = errorPipe.fileHandleForReading.readDataToEndOfFile()
            let errorString = String(data: errorData, encoding: .utf8) ?? "Unknown SSH error"
            throw RemoteSyncError.sshFailed(errorString.trimmingCharacters(in: .whitespacesAndNewlines))
        }
    }
}
