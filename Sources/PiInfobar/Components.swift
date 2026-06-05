import SwiftUI

// MARK: - Formatting helpers

enum Fmt {
    static func money(_ v: Double) -> String {
        if v >= 1000 { return String(format: "$%.0f", v) }
        return String(format: "$%.2f", v)
    }
    static func moneyShort(_ v: Double) -> String {
        if v >= 1000 { return String(format: "$%.1fk", v / 1000) }
        return String(format: "$%.0f", v)
    }
    static func int(_ v: Int) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        return f.string(from: NSNumber(value: v)) ?? "\(v)"
    }
    static func tokens(_ v: Int) -> String {
        let d = Double(v)
        if d >= 1_000_000_000 { return String(format: "%.1fB", d / 1e9) }
        if d >= 1_000_000 { return String(format: "%.1fM", d / 1e6) }
        if d >= 1_000 { return String(format: "%.1fK", d / 1e3) }
        return "\(v)"
    }
}

// MARK: - Stat card (Overview grid)

struct StatCard: View {
    let icon: String
    let iconColor: Color
    let title: String
    let value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(spacing: 6) {
                Image(systemName: icon)
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundStyle(iconColor)
                Text(title)
                    .font(.system(size: 11, weight: .medium))
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }
            Text(value)
                .font(.system(size: 19, weight: .bold, design: .rounded))
                .foregroundStyle(.primary)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.horizontal, 12)
        .padding(.vertical, 10)
        .background(
            RoundedRectangle(cornerRadius: 12, style: .continuous)
                .fill(Color(nsColor: .controlBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 12, style: .continuous)
                .strokeBorder(Color.primary.opacity(0.06), lineWidth: 1)
        )
    }
}

// MARK: - Section header

struct SectionHeader: View {
    let title: String
    var trailing: String? = nil
    var body: some View {
        HStack {
            Text(title)
                .font(.system(size: 13, weight: .bold))
                .foregroundStyle(.primary)
            Spacer()
            if let t = trailing {
                Text(t)
                    .font(.system(size: 11, weight: .medium))
                    .foregroundStyle(.secondary)
            }
        }
    }
}

// MARK: - Horizontal bar row (languages / models / projects)

struct BarRow: View {
    let rank: Int
    let icon: String
    let color: Color
    let title: String
    let subtitle: String
    let value: String
    let fraction: Double   // 0...1

    var body: some View {
        HStack(spacing: 10) {
            ZStack {
                RoundedRectangle(cornerRadius: 7, style: .continuous)
                    .fill(color.opacity(0.16))
                    .frame(width: 28, height: 28)
                Image(systemName: icon)
                    .font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(color)
            }
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(title)
                        .font(.system(size: 12.5, weight: .semibold))
                        .lineLimit(1)
                    Spacer(minLength: 6)
                    Text(value)
                        .font(.system(size: 12, weight: .bold, design: .rounded))
                        .foregroundStyle(.primary)
                }
                GeometryReader { geo in
                    ZStack(alignment: .leading) {
                        Capsule().fill(Color.primary.opacity(0.06))
                            .frame(height: 5)
                        Capsule().fill(color)
                            .frame(width: max(4, geo.size.width * fraction), height: 5)
                    }
                }
                .frame(height: 5)
                Text(subtitle)
                    .font(.system(size: 10.5))
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }
        }
    }
}

// MARK: - Empty / loading states

struct CenteredMessage: View {
    let icon: String
    let title: String
    var subtitle: String? = nil
    var body: some View {
        VStack(spacing: 10) {
            Image(systemName: icon)
                .font(.system(size: 30, weight: .light))
                .foregroundStyle(.secondary)
            Text(title)
                .font(.system(size: 13, weight: .semibold))
            if let s = subtitle {
                Text(s)
                    .font(.system(size: 11))
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding(30)
    }
}
