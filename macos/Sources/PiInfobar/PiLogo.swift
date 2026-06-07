import SwiftUI
import AppKit

/// The Pi "P + i" block mark, recreated from logo-auto.svg as a vector Shape
/// (cropped to its content bounding box so it fills the target rect).
struct PiLogoShape: Shape {
    // Content bounds in the original 800×800 SVG space.
    private let off: CGFloat = 165.29
    private let span: CGFloat = 634.72 - 165.29   // 469.43

    func path(in rect: CGRect) -> Path {
        let s = min(rect.width, rect.height) / span
        let dx = rect.minX + (rect.width - span * s) / 2
        let dy = rect.minY + (rect.height - span * s) / 2
        func p(_ x: CGFloat, _ y: CGFloat) -> CGPoint {
            CGPoint(x: dx + (x - off) * s, y: dy + (y - off) * s)
        }

        var path = Path()
        // Outer P boundary.
        path.move(to: p(165.29, 165.29))
        path.addLine(to: p(517.36, 165.29))
        path.addLine(to: p(517.36, 400))
        path.addLine(to: p(400, 400))
        path.addLine(to: p(400, 517.36))
        path.addLine(to: p(282.65, 517.36))
        path.addLine(to: p(282.65, 634.72))
        path.addLine(to: p(165.29, 634.72))
        path.closeSubpath()
        // Inner hole (even-odd).
        path.move(to: p(282.65, 282.65))
        path.addLine(to: p(282.65, 400))
        path.addLine(to: p(400, 400))
        path.addLine(to: p(400, 282.65))
        path.closeSubpath()
        // i dot.
        path.move(to: p(517.36, 400))
        path.addLine(to: p(634.72, 400))
        path.addLine(to: p(634.72, 634.72))
        path.addLine(to: p(517.36, 634.72))
        path.closeSubpath()
        return path
    }
}

extension PiLogoShape {
    /// A template NSImage for the menu bar (adapts to light/dark automatically).
    @MainActor
    static func menuBarImage(size: CGFloat = 15) -> NSImage? {
        let renderer = ImageRenderer(
            content: PiLogoShape()
                .fill(.black, style: FillStyle(eoFill: true))
                .frame(width: size, height: size)
        )
        renderer.scale = 3
        guard let img = renderer.nsImage else { return nil }
        img.isTemplate = true
        return img
    }
}
