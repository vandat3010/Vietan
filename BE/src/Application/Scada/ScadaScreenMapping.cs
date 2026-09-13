using Backend.Domain.Enums;

namespace Backend.Application.Scada;

/// <summary>Screen slug / Excel-cell mapping rules. No tag codes here.</summary>
public static class ScadaScreenMapping
{
    public static bool IsRealtime(ScadaScreenType screen) =>
        screen is ScadaScreenType.NguyenLy
            or ScadaScreenType.CongNghe
            or ScadaScreenType.ChiTietBom
            or ScadaScreenType.Loi;

    /// <summary>
    /// Excel Trung tâm: Lỗi is marked with "Có". Nguyên lý / Công nghệ / Chi tiết bơm
    /// mix "Có" and display labels — nonempty means mapped (counts 48 / 51 / 232).
    /// Trend / Báo cáo: nonempty display text.
    /// </summary>
    public static bool IsMappedCell(ScadaScreenType screen, string? cell)
    {
        if (string.IsNullOrWhiteSpace(cell))
            return false;

        var text = cell.Trim();
        return screen switch
        {
            ScadaScreenType.Loi => IsCo(text),
            _ => true
        };
    }

    public static bool IsCo(string? cell) =>
        string.Equals(cell?.Trim(), "Có", StringComparison.OrdinalIgnoreCase)
        || string.Equals(cell?.Trim(), "Co", StringComparison.OrdinalIgnoreCase)
        || string.Equals(cell?.Trim(), "1", StringComparison.Ordinal);

    public static bool TryParse(string? raw, out ScadaScreenType screen)
    {
        screen = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var key = raw.Trim().ToLowerInvariant().Replace('_', '-');
        switch (key)
        {
            case "nguyen-ly":
            case "nguyenly":
            case "nguyên lý":
            case "nguyen ly":
                screen = ScadaScreenType.NguyenLy;
                return true;
            case "cong-nghe":
            case "congnghe":
            case "công nghệ":
            case "cong nghe":
                screen = ScadaScreenType.CongNghe;
                return true;
            case "chi-tiet-bom":
            case "chitietbom":
            case "chi tiết bơm":
            case "chi tiet bom":
                screen = ScadaScreenType.ChiTietBom;
                return true;
            case "loi":
            case "lỗi":
            case "fault":
                screen = ScadaScreenType.Loi;
                return true;
            case "trend":
                screen = ScadaScreenType.Trend;
                return true;
            case "bao-cao":
            case "baocao":
            case "báo cáo":
            case "bao cao":
            case "report":
                screen = ScadaScreenType.BaoCao;
                return true;
            default:
                return Enum.TryParse(raw, ignoreCase: true, out screen);
        }
    }

    public static string ToSlug(ScadaScreenType screen) => screen switch
    {
        ScadaScreenType.NguyenLy => "nguyen-ly",
        ScadaScreenType.CongNghe => "cong-nghe",
        ScadaScreenType.ChiTietBom => "chi-tiet-bom",
        ScadaScreenType.Loi => "loi",
        ScadaScreenType.Trend => "trend",
        ScadaScreenType.BaoCao => "bao-cao",
        _ => screen.ToString().ToLowerInvariant()
    };
}
