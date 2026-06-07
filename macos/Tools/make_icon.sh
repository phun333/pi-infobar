#!/bin/bash
# Generate Resources/AppIcon.icns from the Pi logo.
set -e
cd "$(dirname "$0")/.."

mkdir -p Resources build/icon.iconset
echo "▸ Rendering master icon…"
swift Tools/render_icon.swift build/icon_1024.png 1024

ICONSET="build/icon.iconset"
rm -rf "$ICONSET"; mkdir -p "$ICONSET"

# Apple's required iconset members (point size + scale).
declare -a sizes=("16:16" "16:32" "32:32" "32:64" "128:128" "128:256" "256:256" "256:512" "512:512" "512:1024")
for entry in "${sizes[@]}"; do
    pt="${entry%%:*}"; px="${entry##*:}"
    if [ "$pt" = "$px" ]; then name="icon_${pt}x${pt}.png"; else name="icon_${pt}x${pt}@2x.png"; fi
    sips -z "$px" "$px" build/icon_1024.png --out "$ICONSET/$name" >/dev/null
done

iconutil -c icns "$ICONSET" -o Resources/AppIcon.icns
echo "✓ Wrote Resources/AppIcon.icns"
