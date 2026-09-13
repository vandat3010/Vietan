using Backend.Domain.Enums;

namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Maps a <see cref="Tag"/> to an operator screen. Populated from Excel metadata;
/// runtime queries never hard-code tag codes.
/// Mapped to <c>scada.tag_screen_mapping</c>.
/// </summary>
public class TagScreenMapping : ScadaEntity
{
    public long TagId { get; set; }

    public ScadaScreenType ScreenType { get; set; }

    /// <summary>True for Nguyên lý / Công nghệ / Chi tiết bơm / Lỗi.</summary>
    public bool IsRealtime { get; set; }

    /// <summary>
    /// Excel cell text: "Có" or a display/meaning label (Trend, Báo cáo, and some realtime cells).
    /// </summary>
    public string? MappingLabel { get; set; }

    public Tag Tag { get; set; } = null!;
}
