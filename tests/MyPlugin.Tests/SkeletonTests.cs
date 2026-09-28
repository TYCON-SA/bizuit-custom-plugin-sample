using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyPlugin.Features.Me;
using Xunit;

namespace MyPlugin.Tests;

/// <summary>
/// Tests for the SKELETON — the plugin entry class and the `/me` endpoint.
///
/// 🔴 WHY THESE MATTER MORE THAN THE EXAMPLE TESTS. When you start a new plugin from this
/// template you delete the example features (Items, Products, AuditLogs) and their tests, because
/// their migrations would create example tables in a real database. What is left is exactly what
/// these tests cover. Without them a brand-new plugin starts at 0% coverage, and a coverage gate
/// then blocks every pull request until the first feature exists — a product that cannot receive
/// even the fix it needs to build. (Learned on a real onboarding, 2026-09-28.)
///
/// So: if you delete the example features, KEEP THIS FILE.
/// </summary>
public class SkeletonTests
{
    private static IConfiguration Config(params (string key, string value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.key, v.value)))
            .Build();

    private static BizuitUserContext TestUser(params string[] roles) => new()
    {
        Username = "jane.doe",
        TenantId = "default",
        IsAuthenticated = true,
        Roles = roles.Length == 0 ? new List<string> { "Registered Users" } : roles.ToList()
    };

    private static HttpContext TestRequest()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/api/me";
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("example.invalid");
        return ctx;
    }

    // ── The class the Backend Host loads ────────────────────────────────────────────────────

    [Fact]
    public void PluginInfo_matches_plugin_json()
    {
        var info = new MyPluginPlugin().Info;

        // The name is what builds the public route `/plugins/<name>/<version>/...`.
        // If it drifts from plugin.json the host will not find the plugin, and the error only
        // shows up at install time — not at compile time.
        Assert.False(string.IsNullOrWhiteSpace(info.Name));
        Assert.False(string.IsNullOrWhiteSpace(info.Version));
        Assert.False(string.IsNullOrWhiteSpace(info.Description));
    }

    [Fact]
    public void EntryClass_is_named_as_the_manifest_says()
    {
        Assert.Equal("MyPlugin.MyPluginPlugin", typeof(MyPluginPlugin).FullName);
        Assert.True(typeof(IBackendPlugin).IsAssignableFrom(typeof(MyPluginPlugin)));
    }

    [Fact]
    public void ConfigureServices_survives_a_host_with_no_connection_string()
    {
        var services = new ServiceCollection();

        new MyPluginPlugin().ConfigureServices(services, Config());

        Assert.NotNull(services);
    }

    [Fact]
    public void ConfigureServices_registers_the_plugin_logger()
    {
        var services = new ServiceCollection();

        new MyPluginPlugin().ConfigureServices(services, Config(
            ("ConnectionStrings:Default", "Server=localhost;Database=sample;Integrated Security=True;")));

        Assert.Contains(services, s => s.ServiceType == typeof(Shared.PluginLogger));
    }

    [Fact]
    public void ConfigureServices_registers_the_dashboard_client_only_when_the_host_provides_a_url()
    {
        var withoutUrl = new ServiceCollection();
        new MyPluginPlugin().ConfigureServices(withoutUrl, Config(
            ("ConnectionStrings:Default", "Server=localhost;Database=sample;Integrated Security=True;")));

        var withUrl = new ServiceCollection();
        new MyPluginPlugin().ConfigureServices(withUrl, Config(
            ("ConnectionStrings:Default", "Server=localhost;Database=sample;Integrated Security=True;"),
            ("System:DashboardApiUrl", "http://localhost:5000/api")));

        Assert.True(withUrl.Count > withoutUrl.Count);
    }

    [Fact]
    public void OnUnloading_does_not_throw()
    {
        // The host calls this on hot reload and on shutdown; throwing here leaves the plugin
        // half unloaded.
        new MyPluginPlugin().OnUnloading();
    }

    // ── The `/me` endpoint, which is how you verify the plugin installed correctly ──────────

    [Fact]
    public void MeEndpoints_Map_registers_me()
    {
        var conventions = new Mock<IEndpointConventionBuilder>();
        var endpoints = new Mock<IPluginEndpointBuilder>();
        endpoints.Setup(e => e.MapGet(It.IsAny<string>(), It.IsAny<Delegate>()))
                 .Returns(conventions.Object);

        MeEndpoints.Map(endpoints.Object);

        endpoints.Verify(e => e.MapGet("me", It.IsAny<Delegate>()), Times.Once);
    }

    [Fact]
    public async Task GetMe_returns_the_authenticated_user_and_its_roles()
    {
        var result = await MeEndpoints.GetMe(TestUser("Administrators", "Gestores"), TestRequest());

        var body = BodyOf(result);
        Assert.Equal("jane.doe", Read(body, "username"));
        Assert.Equal(true, Read(body, "isAuthenticated"));
        Assert.Equal(2, Read(body, "rolesCount"));
    }

    [Fact]
    public async Task GetMe_answers_the_role_questions_according_to_the_roles_held()
    {
        var result = await MeEndpoints.GetMe(TestUser("Administrators"), TestRequest());

        var checks = Read(BodyOf(result), "roleChecks")!;
        Assert.Equal(true, Read(checks, "hasAdministrators"));
        Assert.Equal(false, Read(checks, "hasGestores"));
        // Holds one of the two, not both: `any` yes, `all` no.
        Assert.Equal(true, Read(checks, "hasAnyAdmin"));
        Assert.Equal(false, Read(checks, "hasAllAdmins"));
    }

    [Fact]
    public async Task GetMe_includes_the_request_context()
    {
        var result = await MeEndpoints.GetMe(TestUser(), TestRequest());

        var http = Read(BodyOf(result), "httpInfo")!;
        Assert.Equal("GET", Read(http, "method"));
        Assert.Equal("/api/me", Read(http, "path"));
    }

    [Fact]
    public async Task GetMe_does_not_repeat_the_authorization_header_among_the_others()
    {
        var request = TestRequest();
        request.Request.Headers.Authorization = "Bearer a-token-that-should-not-travel-twice";

        var result = await MeEndpoints.GetMe(TestUser(), request);

        var http = Read(BodyOf(result), "httpInfo")!;
        var all = (IDictionary<string, string>)Read(http, "allHeaders")!;
        Assert.DoesNotContain(all.Keys, k => k.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
    }

    // The endpoint returns an anonymous type inside Ok(...): read it by reflection.
    private static object BodyOf(IResult result)
    {
        var prop = result.GetType().GetProperty("Value");
        Assert.True(prop is not null, "the response carries no body");
        var value = prop!.GetValue(result);
        Assert.NotNull(value);
        return value!;
    }

    private static object? Read(object obj, string property)
    {
        var prop = obj.GetType().GetProperty(property);
        Assert.True(prop is not null, $"the body does not carry '{property}'");
        return prop!.GetValue(obj);
    }
}
