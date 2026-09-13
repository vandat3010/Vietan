using Backend.Api.Middlewares;
using Backend.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backend.UnitTests;

/// <summary>
/// BE 1.3a — whitelist is metadata-based (not URL substring), so business
/// endpoints cannot be bypassed by crafted paths. These tests assert the exempt
/// decision only; the DB check runs in the middleware pipeline.
/// </summary>
public class MustChangePasswordMiddlewareTests
{
    private static Endpoint EndpointWith(params object[] metadata) =>
        new(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test");

    [Fact]
    public void NullEndpoint_Is_Exempt()
    {
        Assert.True(MustChangePasswordMiddleware.IsExempt(null));
    }

    [Fact]
    public void AnonymousEndpoint_Is_Exempt()
    {
        var endpoint = EndpointWith(new AllowAnonymousAttribute());
        Assert.True(MustChangePasswordMiddleware.IsExempt(endpoint));
    }

    [Fact]
    public void WhitelistedEndpoint_Is_Exempt()
    {
        var endpoint = EndpointWith(new AllowWhenPasswordChangeRequiredAttribute());
        Assert.True(MustChangePasswordMiddleware.IsExempt(endpoint));
    }

    [Fact]
    public void BusinessEndpoint_Without_Metadata_Is_Not_Exempt()
    {
        var endpoint = EndpointWith(); // no metadata → blocked when flag is true
        Assert.False(MustChangePasswordMiddleware.IsExempt(endpoint));
    }

    [Fact]
    public void BusinessEndpoint_With_Unrelated_Metadata_Is_Not_Exempt()
    {
        // e.g. an endpoint whose route contains "password" but is not opted in.
        var endpoint = EndpointWith(new HttpMethodMetadata(["POST"]));
        Assert.False(MustChangePasswordMiddleware.IsExempt(endpoint));
    }

    [Fact]
    public void Auth_DTOs_Expose_MustChangePassword()
    {
        Assert.NotNull(typeof(AuthTokenResponse).GetProperty(nameof(AuthTokenResponse.MustChangePassword)));
        Assert.NotNull(typeof(CurrentUserResponse).GetProperty(nameof(CurrentUserResponse.MustChangePassword)));
    }
}
