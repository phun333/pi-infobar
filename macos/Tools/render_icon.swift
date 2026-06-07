// Renders the Pi Stats app icon (white π mark on a gradient squircle) to a PNG.
// Usage: swift Tools/render_icon.swift <output.png> <pixelSize>
import AppKit

let args = CommandLine.arguments
let outPath = args.count > 1 ? args[1] : "icon_1024.png"
let N = args.count > 2 ? Double(args[2]) ?? 1024 : 1024

guard let rep = NSBitmapImageRep(
    bitmapDataPlanes: nil, pixelsWide: Int(N), pixelsHigh: Int(N),
    bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
    colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
) else { fatalError("rep") }

NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
let ctx = NSGraphicsContext.current!.cgContext

// --- Squircle background ---
let inset = N * 0.085
let rect = CGRect(x: inset, y: inset, width: N - inset * 2, height: N - inset * 2)
let radius = rect.width * 0.2237   // Apple-ish continuous corner
let bg = CGPath(roundedRect: rect, cornerWidth: radius, cornerHeight: radius, transform: nil)

ctx.saveGState()
ctx.addPath(bg)
ctx.clip()
let colors = [
    CGColor(red: 0x7C/255, green: 0x5C/255, blue: 0xFF/255, alpha: 1),
    CGColor(red: 0x4D/255, green: 0x7C/255, blue: 0xFF/255, alpha: 1),
] as CFArray
let grad = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(), colors: colors,
                      locations: [0, 1])!
ctx.drawLinearGradient(grad, start: CGPoint(x: rect.minX, y: rect.maxY),
                       end: CGPoint(x: rect.maxX, y: rect.minY), options: [])
ctx.restoreGState()

// Subtle top highlight.
ctx.saveGState()
ctx.addPath(bg)
ctx.clip()
ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 0.08))
ctx.fill(CGRect(x: rect.minX, y: rect.midY, width: rect.width, height: rect.height / 2))
ctx.restoreGState()

// --- White π mark (P + i blocks, even-odd) ---
let off = 165.29, span = 634.72 - 165.29
let markSize = N * 0.46
let mx = (N - markSize) / 2
let my = (N - markSize) / 2
func p(_ x: Double, _ y: Double) -> CGPoint {
    let s = markSize / span
    // Flip Y: SVG is top-left origin, CG is bottom-left.
    return CGPoint(x: mx + (x - off) * s, y: my + (span - (y - off)) * s)
}

let mark = CGMutablePath()
// Outer P
mark.move(to: p(165.29, 165.29))
mark.addLine(to: p(517.36, 165.29))
mark.addLine(to: p(517.36, 400))
mark.addLine(to: p(400, 400))
mark.addLine(to: p(400, 517.36))
mark.addLine(to: p(282.65, 517.36))
mark.addLine(to: p(282.65, 634.72))
mark.addLine(to: p(165.29, 634.72))
mark.closeSubpath()
// Hole
mark.move(to: p(282.65, 282.65))
mark.addLine(to: p(282.65, 400))
mark.addLine(to: p(400, 400))
mark.addLine(to: p(400, 282.65))
mark.closeSubpath()
// i dot
mark.move(to: p(517.36, 400))
mark.addLine(to: p(634.72, 400))
mark.addLine(to: p(634.72, 634.72))
mark.addLine(to: p(517.36, 634.72))
mark.closeSubpath()

ctx.addPath(mark)
ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
ctx.fillPath(using: .evenOdd)

NSGraphicsContext.restoreGraphicsState()

guard let data = rep.representation(using: .png, properties: [:]) else { fatalError("png") }
try! data.write(to: URL(fileURLWithPath: outPath))
print("wrote \(outPath) @ \(Int(N))px")
