using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Full-pipeline tests: real HTTP requests, real session cookie, real [RequireRole] filters.
/// Direct controller calls cannot exercise the role attributes, so role enforcement is proved here.
/// </summary>
public class AuthorizationPipelineTests : IClassFixture<AuthorizationPipelineTests.ApiFactory>
{
    private const string StrongPassword = "StrongPassword123!";
    private readonly ApiFactory _factory;

    public AuthorizationPipelineTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string> OrganiserOnlyRoutes => new()
    {
        { "POST", "/api/events" },
        { "PUT", "/api/events/1" },
        { "DELETE", "/api/events/1" },
        { "POST", "/api/events/1/categories" },
        { "PUT", "/api/categories/1" },
        { "DELETE", "/api/categories/1" },
        { "GET", "/api/events/1/registrations" },
        { "POST", "/api/events/1/results" },
        { "PUT", "/api/results/1" },
        { "POST", "/api/disciplines" },
        { "POST", "/api/clubs" }
    };

    public static TheoryData<string, string> ParticipantOnlyRoutes => new()
    {
        { "POST", "/api/events/1/registrations" },
        { "GET", "/api/registrations/me" },
        { "GET", "/api/results/me" },
        { "GET", "/api/users/me/memberships" },
        { "POST", "/api/clubs/1/memberships" }
    };

    public static TheoryData<string, string> LoginRequiredRoutes => new()
    {
        { "GET", "/api/users/me" },
        { "PUT", "/api/users/me" },
        { "PUT", "/api/auth/password" }
    };

    [Theory]
    [MemberData(nameof(OrganiserOnlyRoutes))]
    [MemberData(nameof(ParticipantOnlyRoutes))]
    [MemberData(nameof(LoginRequiredRoutes))]
    public async Task ProtectedRoute_WithoutSession_ReturnsUnauthorized(string method, string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(OrganiserOnlyRoutes))]
    public async Task OrganiserRoute_AsParticipant_ReturnsForbidden(string method, string url)
    {
        using var client = await SignedInClientAsync(Roles.Participant);

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ParticipantOnlyRoutes))]
    public async Task ParticipantRoute_AsOrganiser_ReturnsForbidden(string method, string url)
    {
        using var client = await SignedInClientAsync(Roles.Organiser);

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_StartsSession_SoProfileIsReadable()
    {
        using var client = await SignedInClientAsync(Roles.Participant);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_StartsSessionOnNewClient()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        using (var registering = _factory.CreateClient())
        {
            var registered = await registering.PostAsJsonAsync("/api/auth/register", RegisterBody(email, Roles.Participant));
            Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        }

        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = StrongPassword });
        var profile = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_DoesNotStartSession()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        using (var registering = _factory.CreateClient())
        {
            await registering.PostAsJsonAsync("/api/auth/register", RegisterBody(email, Roles.Participant));
        }

        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword123!" });
        var profile = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsServerSideSession()
    {
        using var client = await SignedInClientAsync(Roles.Participant);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_ThroughPipeline()
    {
        using var client = await SignedInClientAsync(Roles.Organiser);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Pipeline event",
            date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd"),
            venueName = "Test venue",
            city = "Test city",
            province = "Test province",
            distanceKm = 10,
            eventType = "Run",
            status = "Published"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<HttpClient> SignedInClientAsync(string role)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody($"{Guid.NewGuid():N}@example.test", role));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return client;
    }

    private static object RegisterBody(string email, string role) => new
    {
        email,
        password = StrongPassword,
        firstName = "Test",
        lastName = role,
        idNumberOrPassport = "TEST-ID-PIPELINE",
        role
    };

    private static HttpRequestMessage Request(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        return request;
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public ApiFactory()
        {
            _connection.Open();
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Identity:EncryptionKey"] = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8="
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
