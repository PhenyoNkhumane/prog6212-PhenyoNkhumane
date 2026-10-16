using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AuthorizationPipelineTests
{
    [Fact]
    public async Task BearerTokenAuthenticatesAndEnforcesRoleThroughPipeline()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        }

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "pipeline@example.test",
            password = "StrongPassword123!",
            firstName = "Taylor",
            lastName = "Runner",
            idNumberOrPassport = "TEST-ID-PIPELINE",
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var responseJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var token = responseJson.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var noTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/events");
        noTokenRequest.Content = JsonContent.Create(new { });
        var noTokenResponse = await client.SendAsync(noTokenRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, noTokenResponse.StatusCode);

        using var wrongRoleRequest = new HttpRequestMessage(HttpMethod.Post, "/api/events");
        wrongRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        wrongRoleRequest.Content = JsonContent.Create(new
        {
            title = "Pipeline event",
            date = "2027-10-10",
            venueName = "Test venue",
            city = "Test city",
            province = "Test province",
            distanceKm = 10,
            eventType = "Run",
            status = "Published"
        });
        var wrongRoleResponse = await client.SendAsync(wrongRoleRequest);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRoleResponse.StatusCode);

        const string organiserEmail = "temporary@example.test";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var organiser = ControllerTestHelper.AddOrganiser(db, organiserEmail, "StrongPassword123!");
            organiser.MustChangePassword = true;
            db.SaveChanges();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = organiserEmail,
            password = "StrongPassword123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        using var temporaryAuth = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var temporaryToken = temporaryAuth.RootElement.GetProperty("token").GetString();

        using var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        blockedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", temporaryToken);
        var blockedResponse = await client.SendAsync(blockedRequest);
        Assert.Equal(HttpStatusCode.Forbidden, blockedResponse.StatusCode);

        using var passwordRequest = new HttpRequestMessage(HttpMethod.Put, "/api/auth/password");
        passwordRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", temporaryToken);
        passwordRequest.Content = JsonContent.Create(new
        {
            currentPassword = "StrongPassword123!",
            newPassword = "ChangedPassword456!"
        });
        var passwordResponse = await client.SendAsync(passwordRequest);
        Assert.True(passwordResponse.StatusCode == HttpStatusCode.NoContent,
            await passwordResponse.Content.ReadAsStringAsync());

        var updatedLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = organiserEmail,
            password = "ChangedPassword456!"
        });
        updatedLogin.EnsureSuccessStatusCode();
        using var updatedAuth = JsonDocument.Parse(await updatedLogin.Content.ReadAsStringAsync());
        var updatedToken = updatedAuth.RootElement.GetProperty("token").GetString();
        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        allowedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", updatedToken);
        var allowedResponse = await client.SendAsync(allowedRequest);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public ApiFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Jwt:SigningKey", "pipeline-test-key-with-more-than-32-bytes");
            builder.UseSetting("Jwt:Issuer", "RaceDay.Api");
            builder.UseSetting("Jwt:Audience", "RaceDay.Client");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "pipeline-test-key-with-more-than-32-bytes",
                    ["Jwt:Issuer"] = "RaceDay.Api",
                    ["Jwt:Audience"] = "RaceDay.Client",
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
