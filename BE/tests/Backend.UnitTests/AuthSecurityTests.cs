using Backend.Application.Interfaces.Services;
using Backend.Domain.Entities.Scada;
using Backend.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Backend.UnitTests;

public class Argon2idPasswordHasherTests
{
    private static Argon2idPasswordHasher CreateSut() =>
        new(Options.Create(new PasswordHashingSettings
        {
            MemorySize = 16_384, // lower for fast unit tests
            Iterations = 2,
            DegreeOfParallelism = 2,
            HashLength = 32,
            SaltLength = 16
        }));

    [Fact]
    public void Hash_Produces_Argon2id_Phc_String()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Password@123");

        Assert.StartsWith("$argon2id$", hash, StringComparison.Ordinal);
        Assert.Contains("m=", hash, StringComparison.Ordinal);
        Assert.Contains("t=", hash, StringComparison.Ordinal);
        Assert.Contains("p=", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_Accepts_Correct_Password()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Password@123");
        Assert.True(sut.Verify("Password@123", hash));
    }

    [Fact]
    public void Verify_Rejects_Wrong_Password()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Password@123");
        Assert.False(sut.Verify("WrongPassword@1", hash));
    }

    [Fact]
    public void Hash_Uses_Different_Salt_Each_Time()
    {
        var sut = CreateSut();
        var a = sut.Hash("Password@123");
        var b = sut.Hash("Password@123");
        Assert.NotEqual(a, b);
        Assert.True(sut.Verify("Password@123", a));
        Assert.True(sut.Verify("Password@123", b));
    }

    [Fact]
    public void Verify_Rejects_Legacy_Non_Argon2_Hash()
    {
        var sut = CreateSut();
        Assert.False(sut.Verify("Password@123", "TEST_HASH_CHANGE_ME"));
        Assert.False(sut.Verify("Password@123", "$2a$12$legacybcryptplaceholderxxxxxxxxxxxx"));
    }
}

public class ScadaTokenServiceRoleTests
{
    private static ScadaTokenService CreateSut() =>
        new(Options.Create(new JwtSettings
        {
            Issuer = "Backend.Api",
            Audience = "Backend.Client",
            SigningKey = "UNIT_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        }));

    [Theory]
    [InlineData("Admin")]
    [InlineData("Monitor")]
    [InlineData("MonitorExport")]
    [InlineData("Engineer")]
    [InlineData("Operator")]
    [InlineData("Supervisor")]
    [InlineData("Manager")]
    [InlineData("Maintenance")]
    [InlineData("Auditor")]
    [InlineData("MaintenanceManager")]
    public void AccessToken_Embeds_Dynamic_Role_From_User(string role)
    {
        var sut = CreateSut();
        var user = new ScadaUser
        {
            Id = 42,
            Username = "roleuser",
            FullName = "Role User",
            Role = role,
            IsActive = true,
            PasswordHash = "$argon2id$test"
        };

        var (token, _) = sut.CreateAccessToken(user, Guid.NewGuid().ToString("N"));
        var principal = sut.ValidateAccessToken(token);

        Assert.NotNull(principal);
        Assert.Equal(role, principal!.FindFirst(ScadaTokenService.RoleClaimType)?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
        Assert.Equal("roleuser", principal.FindFirst(ScadaTokenService.UsernameClaimType)?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value);
        Assert.Equal("42", principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value);
        Assert.False(string.IsNullOrWhiteSpace(
            principal.FindFirst(Backend.Shared.Constants.ClaimTypesExtended.SessionId)?.Value));
    }

    [Fact]
    public void AccessToken_DoesNotEmbed_Secrets()
    {
        var sut = CreateSut();
        var user = new ScadaUser
        {
            Id = 1,
            Username = "op",
            FullName = "Op",
            Role = "Operator",
            IsActive = true,
            PasswordHash = "$argon2id$secret-hash-must-not-appear"
        };

        var (token, expires) = sut.CreateAccessToken(user, "session-abc");
        Assert.True(expires > DateTimeOffset.UtcNow);
        Assert.DoesNotContain("Password", token, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(user.PasswordHash, token);
        Assert.DoesNotContain("UNIT_TEST_SIGNING_KEY", token);

        var principal = sut.ValidateAccessToken(token);
        Assert.NotNull(principal);
        Assert.DoesNotContain(principal!.Claims, c =>
            c.Type.Contains("password", StringComparison.OrdinalIgnoreCase)
            || c.Type.Contains("refresh", StringComparison.OrdinalIgnoreCase)
            || (c.Value?.Contains("$argon2id$", StringComparison.Ordinal) ?? false));
    }

    [Fact]
    public void ValidateAccessToken_Rejects_WrongSignature()
    {
        var sut = CreateSut();
        var other = new ScadaTokenService(Options.Create(new JwtSettings
        {
            Issuer = "Backend.Api",
            Audience = "Backend.Client",
            SigningKey = "DIFFERENT_SIGNING_KEY_AT_LEAST_32_CHARS!!!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        }));

        var user = new ScadaUser { Id = 1, Username = "op", FullName = "Op", Role = "Operator", IsActive = true, PasswordHash = "x" };
        var (token, _) = other.CreateAccessToken(user, "sid");
        Assert.Null(sut.ValidateAccessToken(token));
    }

    [Fact]
    public void ValidateAccessToken_Rejects_WrongIssuer()
    {
        var issuer = new ScadaTokenService(Options.Create(new JwtSettings
        {
            Issuer = "Other.Api",
            Audience = "Backend.Client",
            SigningKey = "UNIT_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        }));
        var sut = CreateSut();
        var user = new ScadaUser { Id = 1, Username = "op", FullName = "Op", Role = "Operator", IsActive = true, PasswordHash = "x" };
        var (token, _) = issuer.CreateAccessToken(user, "sid");
        Assert.Null(sut.ValidateAccessToken(token));
    }

    [Fact]
    public void ValidateAccessToken_Rejects_ExpiredToken()
    {
        var settings = new JwtSettings
        {
            Issuer = "Backend.Api",
            Audience = "Backend.Client",
            SigningKey = "UNIT_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(settings.SigningKey));
        var past = DateTime.UtcNow.AddMinutes(-10);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new System.Security.Claims.Claim("sub", "1")],
            notBefore: past.AddMinutes(-1),
            expires: past,
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

        Assert.Null(CreateSut().ValidateAccessToken(token));
    }
}

public class ConcurrentLimitInfoTests
{
    [Theory]
    [InlineData(10, 1, 9)]
    [InlineData(20, 1, 19)]
    [InlineData(50, 1, 49)]
    [InlineData(10, 0, 10)]
    [InlineData(5, 1, 4)]
    public void MaxNormalConcurrentUsers_Is_Total_Minus_Reserved(int max, int reserved, int expectedNormal)
    {
        var info = new ConcurrentLimitInfo
        {
            MaxConcurrentUsers = max,
            ReservedAdminSlots = reserved
        };
        Assert.Equal(expectedNormal, info.MaxNormalConcurrentUsers);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("admin", true)]
    [InlineData("SuperAdmin", true)]
    [InlineData("Operator", false)]
    [InlineData("Monitor", false)]
    [InlineData(null, false)]
    public void IsAdminRole_Matches_Security_Roles(string? role, bool expected)
    {
        Assert.Equal(expected, ConcurrentLicenseService.IsAdminRole(role));
    }
}
