#!/bin/bash
# Build PiInfobar.app — a menu-bar-only macOS app, bundled from the SwiftPM binary.
set -e
cd "$(dirname "$0")"

APP_NAME="Pi Stats"
BUNDLE_ID="com.pi.infobar"
VERSION="0.1.0"

echo "▸ Building release binary…"
swift build -c release

BIN=".build/release/PiInfobar"
APP="build/${APP_NAME}.app"
CONTENTS="$APP/Contents"

if [ ! -f Resources/AppIcon.icns ]; then
    echo "▸ Generating app icon…"
    ./Tools/make_icon.sh
fi

echo "▸ Assembling bundle: $APP"
rm -rf "$APP"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"
cp "$BIN" "$CONTENTS/MacOS/PiInfobar"
cp Resources/AppIcon.icns "$CONTENTS/Resources/AppIcon.icns"

cat > "$CONTENTS/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key><string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key><string>${BUNDLE_ID}</string>
    <key>CFBundleVersion</key><string>${VERSION}</string>
    <key>CFBundleShortVersionString</key><string>${VERSION}</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleExecutable</key><string>PiInfobar</string>
    <key>CFBundleIconFile</key><string>AppIcon</string>
    <key>CFBundleIconName</key><string>AppIcon</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>LSUIElement</key><true/>
    <key>NSHighResolutionCapable</key><true/>
    <key>NSHumanReadableCopyright</key><string>Pi Stats</string>
</dict>
</plist>
PLIST

# Ad-hoc sign so macOS lets it run without quarantine prompts on this machine.
codesign --force --deep --sign - "$APP" 2>/dev/null || true

echo "✓ Built $APP"
