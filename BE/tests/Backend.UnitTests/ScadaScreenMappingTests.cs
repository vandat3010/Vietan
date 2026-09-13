using Backend.Application.Realtime;
using Backend.Application.Scada;
using Backend.Domain.Enums;
using Backend.Infrastructure.Realtime;
using Backend.Infrastructure.Scada;

namespace Backend.UnitTests;

public class ScadaScreenMappingTests
{
    [Theory]
    [InlineData("nguyen-ly", ScadaScreenType.NguyenLy)]
    [InlineData("cong-nghe", ScadaScreenType.CongNghe)]
    [InlineData("chi-tiet-bom", ScadaScreenType.ChiTietBom)]
    [InlineData("loi", ScadaScreenType.Loi)]
    [InlineData("trend", ScadaScreenType.Trend)]
    [InlineData("bao-cao", ScadaScreenType.BaoCao)]
    public void TryParse_Accepts_Slugs(string slug, ScadaScreenType expected)
    {
        Assert.True(ScadaScreenMapping.TryParse(slug, out var screen));
        Assert.Equal(expected, screen);
        Assert.Equal(slug, ScadaScreenMapping.ToSlug(screen));
    }

    [Fact]
    public void Loi_Maps_Only_Co()
    {
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.Loi, "Có"));
        Assert.False(ScadaScreenMapping.IsMappedCell(ScadaScreenType.Loi, "nhiệt độ"));
        Assert.False(ScadaScreenMapping.IsMappedCell(ScadaScreenType.Loi, null));
    }

    [Fact]
    public void NguyenLy_CongNghe_ChiTietBom_Map_NonEmpty()
    {
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.NguyenLy, "Có"));
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.NguyenLy, "Công suất"));
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.CongNghe, "T.Gian"));
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.ChiTietBom, "nhiệt độ cuộn A thực tế"));
        Assert.False(ScadaScreenMapping.IsMappedCell(ScadaScreenType.ChiTietBom, "  "));
    }

    [Fact]
    public void Trend_BaoCao_Map_NonEmpty()
    {
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.Trend, "nhiệt độ cuộn A"));
        Assert.True(ScadaScreenMapping.IsMappedCell(ScadaScreenType.BaoCao, "Mức nước sông"));
        Assert.False(ScadaScreenMapping.IsMappedCell(ScadaScreenType.Trend, null));
    }

    [Fact]
    public void Realtime_Screens_Are_Four_Operator_Views()
    {
        Assert.True(ScadaScreenMapping.IsRealtime(ScadaScreenType.NguyenLy));
        Assert.True(ScadaScreenMapping.IsRealtime(ScadaScreenType.Loi));
        Assert.False(ScadaScreenMapping.IsRealtime(ScadaScreenType.Trend));
        Assert.False(ScadaScreenMapping.IsRealtime(ScadaScreenType.BaoCao));
    }
}

public class FakeRealtimeDataStoreTests
{
    [Fact]
    public async Task Set_Then_GetMany_Returns_Only_Requested_Ids()
    {
        var store = new FakeRealtimeDataStore();
        await store.SetAsync(1, new RealtimeValue { TagId = 1, Value = true, Timestamp = DateTimeOffset.UtcNow });
        await store.SetAsync(2, new RealtimeValue { TagId = 2, Value = 12.5, Timestamp = DateTimeOffset.UtcNow });
        await store.SetAsync(3, new RealtimeValue { TagId = 3, Value = 9, Timestamp = DateTimeOffset.UtcNow });

        var many = await store.GetManyAsync([1, 3, 99]);
        Assert.Equal(2, many.Count);
        Assert.True(many.ContainsKey(1));
        Assert.True(many.ContainsKey(3));
        Assert.False(many.ContainsKey(2));
        Assert.False(many.ContainsKey(99));
    }
}

public class TagDefinitionExcelImportTests
{
    [Fact]
    public void ImportWorkbook_Produces_Expected_Screen_Counts()
    {
        var path = ResolveWorkbook();
        if (path is null)
            return; // workbook not copied in some CI layouts

        var rows = TagDefinitionExcelSeedHostedService.ImportWorkbook(path);
        Assert.True(rows.Count >= 600);

        var counts = rows.SelectMany(r => r.Screens)
            .GroupBy(s => s.Screen)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(48, counts.GetValueOrDefault(ScadaScreenType.NguyenLy));
        Assert.Equal(51, counts.GetValueOrDefault(ScadaScreenType.CongNghe));
        Assert.Equal(232, counts.GetValueOrDefault(ScadaScreenType.ChiTietBom));
        Assert.Equal(50, counts.GetValueOrDefault(ScadaScreenType.Loi));
        Assert.Equal(163, counts.GetValueOrDefault(ScadaScreenType.Trend));
        Assert.Equal(173, counts.GetValueOrDefault(ScadaScreenType.BaoCao));
    }

    private static string? ResolveWorkbook()
    {
        var name = "Dinh_Nghia_Tag_Thuy_Loi_Ha_Noi.xlsx";
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", name),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", name)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data", name)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "data", name)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data", name))
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}

public class RealtimeValueGeneratorTests
{
    [Theory]
    [InlineData("bool")]
    [InlineData("int")]
    [InlineData("real")]
    public void Generate_Does_Not_Use_Tag_Codes(string dataType)
    {
        var value = RealtimeValueGenerator.Generate(dataType, 42);
        Assert.NotNull(value);
    }
}
