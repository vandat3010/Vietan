using Backend.Shared.Constants;

namespace Backend.Application.DTOs.Scada;

/// <summary>Response sơ đồ nguyên lý theo trạm.</summary>
public class StationSchematicDto
{
    public StationElectricalStationDto Station { get; set; } = new();
    public SchematicLayoutDto Layout { get; set; } = new();
    public IReadOnlyList<SchematicPumpDto> Pumps { get; set; } = [];
}

public class SchematicLayoutDto
{
    public string Mba { get; set; } = "MBA 2000KVA";
    public string Msb { get; set; } = "MSB";
    public IReadOnlyList<string> Mdbs { get; set; } = ["MDB1", "MDB2", "MDB3", "MDB4", "MDB5"];
}

public class SchematicPumpDto
{
    public int Id { get; set; }
    public long DeviceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double PowerKw { get; set; }
    public string MccbCode { get; set; } = string.Empty;
    public string MotorStatus { get; set; } = ScadaStatusCodes.Unknown;
    public string KdmStatus { get; set; } = ScadaStatusCodes.Stopped;
    public string LockStatus { get; set; } = ScadaStatusCodes.Open;
    public double? I1 { get; set; }
    public double? I2 { get; set; }
    public double? I3 { get; set; }
    public double? V1 { get; set; }
    public double? V2 { get; set; }
    public double? V3 { get; set; }
    public double? CurrentA { get; set; }
    public double? RuntimeH { get; set; }
}
