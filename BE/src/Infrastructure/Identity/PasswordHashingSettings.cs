namespace Backend.Infrastructure.Identity;

/// <summary>Bound from the "PasswordHashing" section of appsettings.</summary>
public class PasswordHashingSettings
{
    public const string SectionName = "PasswordHashing";

    /// <summary>Always Argon2id for this template.</summary>
    public string Algorithm { get; set; } = "Argon2id";

    /// <summary>Memory cost in KiB. 65536 = 64 MiB — solid baseline for interactive logins.</summary>
    public int MemorySize { get; set; } = 65_536;

    /// <summary>Time cost (iterations). 3 balances security vs. login latency on typical API hosts.</summary>
    public int Iterations { get; set; } = 3;

    /// <summary>Parallelism / lanes.</summary>
    public int DegreeOfParallelism { get; set; } = 4;

    public int HashLength { get; set; } = 32;

    public int SaltLength { get; set; } = 16;
}
