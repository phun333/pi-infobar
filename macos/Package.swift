// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "PiInfobar",
    platforms: [
        .macOS(.v14)
    ],
    targets: [
        .executableTarget(
            name: "PiInfobar",
            path: "Sources/PiInfobar"
        ),
        .testTarget(
            name: "PiInfobarTests",
            dependencies: ["PiInfobar"],
            path: "Tests/PiInfobarTests"
        )
    ]
)
