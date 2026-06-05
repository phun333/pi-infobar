#!/bin/bash
# Build a distributable Pi Stats.app, then package it as a DMG (drag-to-Applications)
# and a zip. Optionally create a GitHub release:  ./release.sh v0.1.0
set -e
cd "$(dirname "$0")"

APP_NAME="Pi Stats"
DMG_VOL="Pi Stats"
DIST="dist"
TAG="${1:-}"

echo "▸ Building app bundle…"
./build_app.sh

APP="build/${APP_NAME}.app"
rm -rf "$DIST"; mkdir -p "$DIST"

# --- ZIP ---
echo "▸ Creating zip…"
ditto -c -k --keepParent "$APP" "$DIST/Pi-Stats.zip"

# --- DMG (with /Applications drop target) ---
echo "▸ Creating DMG…"
STAGE="build/dmg-stage"
rm -rf "$STAGE"; mkdir -p "$STAGE"
cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"
rm -f "$DIST/Pi-Stats.dmg"
hdiutil create -volname "$DMG_VOL" -srcfolder "$STAGE" -ov -format UDZO \
    "$DIST/Pi-Stats.dmg" >/dev/null
rm -rf "$STAGE"

echo "✓ Artifacts:"
ls -lh "$DIST"

# --- Optional GitHub release ---
if [ -n "$TAG" ]; then
    if ! command -v gh >/dev/null; then
        echo "⚠ gh not installed — skipping GitHub release."; exit 0
    fi
    echo "▸ Creating GitHub release $TAG…"
    gh release create "$TAG" \
        "$DIST/Pi-Stats.dmg" "$DIST/Pi-Stats.zip" \
        --title "Pi Stats $TAG" \
        --notes "Pi Stats $TAG — native menu-bar usage dashboard for the Pi agent.

**Install:** open the DMG and drag **Pi Stats** to Applications.
First launch: right-click the app → **Open** (unsigned build, one-time Gatekeeper prompt)."
    echo "✓ Released $TAG"
fi
