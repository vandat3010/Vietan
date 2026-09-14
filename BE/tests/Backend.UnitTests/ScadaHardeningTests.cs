using Backend.Application.Common;
using Backend.Api.Controllers.Scada;
using Backend.Api.Realtime;
using Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Backend.UnitTests;

public class PasswordComplexityTests
{
    [Theory]
    [InlineData("Abcd1234!", true)]
    [InlineData("short1!", false)]
    [InlineData("alllowercase1!", false)]
    [InlineData("ALLUPPERCASE1!", false)]
    [InlineData("NoDigits!!!!", false)]
    [InlineData("NoSpecial12", false)]
    public void TryValidate_DefaultPolicy(string password, bool expected)
    {
        var ok = PasswordComplexity.TryValidate(
            password,
            PasswordComplexity.DefaultMinLength,
            PasswordComplexity.DefaultMaxLength,
            requireUppercase: true,
            requireLowercase: true,
            requireDigit: true,
            requireSpecial: true,
            out _);
        Assert.Equal(expected, ok);
    }
}

public class ScadaAuthorizationSurfaceTests
{
    [Theory]
    [InlineData(typeof(ScadaUsersController), ScadaRoles.Admin)]
    [InlineData(typeof(AppSettingsController), ScadaRoles.Admin)]
    [InlineData(typeof(MqttConfigsController), ScadaRoles.Admin)]
    [InlineData(typeof(CommunicationConfigsController), ScadaRoles.Admin)]
    [InlineData(typeof(HistoryProfilesController), ScadaRoles.Admin)]
    [InlineData(typeof(TagHistoryConfigsController), ScadaRoles.Admin)]
    public void SensitiveControllers_RequireAdminRole(Type controllerType, string expectedRole)
    {
        var attr = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal(expectedRole, attr.Roles);
    }

    [Theory]
    [InlineData(typeof(StationsController))]
    [InlineData(typeof(PlcsController))]
    [InlineData(typeof(DevicesController))]
    [InlineData(typeof(TagsController))]
    [InlineData(typeof(SessionPolicyController))]
    [InlineData(typeof(AlarmHistoriesController))]
    [InlineData(typeof(EventLogsController))]
    [InlineData(typeof(ScadaRealtimeHub))]
    public void OperationalControllers_RequireAuthentication(Type type)
    {
        Assert.Contains(
            type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>(),
            a => a.Roles is null || a.Roles.Length == 0 || a.Roles.Contains(ScadaRoles.Admin, StringComparison.Ordinal));
    }

    [Fact]
    public void SessionPolicy_Put_IsAdminOnly()
    {
        var method = typeof(SessionPolicyController).GetMethod(nameof(SessionPolicyController.Update))!;
        var attr = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal(ScadaRoles.Admin, attr.Roles);
    }

    [Fact]
    public void Stations_Put_IsAdminOnly()
    {
        var method = typeof(StationsController).GetMethod(nameof(StationsController.Update))!;
        var attr = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal(ScadaRoles.Admin, attr.Roles);
    }

    [Fact]
    public void AuditActionNames_UserAndStationActions_Exist()
    {
        Assert.Equal("CreateUser", AuditActionNames.CreateUser);
        Assert.Equal("UpdateUser", AuditActionNames.UpdateUser);
        Assert.Equal("DeactivateUser", AuditActionNames.DeactivateUser);
        Assert.Equal("UpdateStation", AuditActionNames.UpdateStation);
        Assert.Equal("Export", AuditActionNames.Export);
    }
}
