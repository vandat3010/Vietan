using Backend.Application.DTOs.Auth;
using Backend.Domain.Entities.Scada;

namespace Backend.UnitTests;

/// <summary>
/// Phase 0 — data model normalization of <c>scada.users</c>.
/// These tests are schema/DTO shape checks and never touch a database.
/// </summary>
public class ScadaUserPhase0Tests
{
    [Theory]
    [InlineData("Email", typeof(string))]
    [InlineData("Unit", typeof(string))]
    [InlineData("Level", typeof(int?))]
    [InlineData("Department", typeof(string))]
    [InlineData("Position", typeof(string))]
    [InlineData("Description", typeof(string))]
    [InlineData("CreatedBy", typeof(string))]
    [InlineData("UpdatedBy", typeof(string))]
    [InlineData("MustChangePassword", typeof(bool))]
    [InlineData("PasswordUpdatedAt", typeof(DateTimeOffset?))]
    [InlineData("FailedLoginCount", typeof(int))]
    [InlineData("LockoutUntil", typeof(DateTimeOffset?))]
    public void ScadaUser_Has_Phase0_Property_With_Expected_Type(string name, Type expected)
    {
        var prop = typeof(ScadaUser).GetProperty(name);
        Assert.NotNull(prop);
        Assert.Equal(expected, prop!.PropertyType);
    }

    [Fact]
    public void Level_Is_Nullable_Int_Not_Enum_Not_String()
    {
        var prop = typeof(ScadaUser).GetProperty(nameof(ScadaUser.Level))!;
        Assert.Equal(typeof(int?), prop.PropertyType);
        Assert.False(prop.PropertyType.IsEnum);
    }

    [Fact]
    public void CurrentUserResponse_Exposes_Profile_But_Not_Credentials()
    {
        var names = typeof(CurrentUserResponse).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("Email", names);
        Assert.Contains("Unit", names);
        Assert.Contains("Level", names);
        Assert.Contains("Department", names);
        Assert.Contains("Position", names);
        Assert.Contains("Description", names);

        AssertNoSensitiveFields(names);
    }

    [Fact]
    public void AuthTokenResponse_Exposes_Profile_But_Not_Credentials_Nor_Lockout()
    {
        var names = typeof(AuthTokenResponse).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("Email", names);
        Assert.Contains("Unit", names);
        Assert.Contains("Level", names);
        Assert.Contains("Department", names);
        Assert.Contains("Position", names);
        Assert.Contains("Description", names);

        AssertNoSensitiveFields(names);
        Assert.DoesNotContain("FailedLoginCount", names);
        Assert.DoesNotContain("LockoutUntil", names);
    }

    private static void AssertNoSensitiveFields(string[] names)
    {
        Assert.DoesNotContain("PasswordHash", names);
        Assert.DoesNotContain("Password", names);
    }
}
