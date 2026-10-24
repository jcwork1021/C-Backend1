using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using minimalAPILesson1.Data;
using minimalAPILesson1.Models;
using System;
using System.Text;
using System.Text.Json;

namespace minimalAPILesson1.Tests.IntegrationTests
{
    public class PlayerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PlayerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // 1. Remove the real SQL Server DbContext registration coming from Program.cs
                    var descriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<AppDBContext>) ||
                             d.ServiceType == typeof(AppDBContext) ||
                             d.ServiceType.FullName?.Contains("DbContextOptions") == true).ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // 2. Register InMemory cleanly using AppDBContext with a unique database name
                    services.AddDbContext<AppDBContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            }).CreateClient();
        }

        [Fact]
        public async Task CreatePlayer_WithValidData_ReturnsOk()
        {
            // 1. Arrange: Create the object you want to send
            var newPlayer = new Player
            {
                Name = "TestHero25",
                Username = "TestHero",
                Email = "hero@test25.com",
                Password = "SecurePassword123"
            };

            // Serialize the C# object into a JSON string content
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(newPlayer),
                Encoding.UTF8,
                "application/json"
            );

            // 2. Act: Send a virtual POST request to your endpoint
            var response = await _client.PostAsync("/api/player/CreatePlayer", jsonContent, TestContext.Current.CancellationToken);

            // 3. Assert: Verify the response code
            response.EnsureSuccessStatusCode(); // Expects 200 OK
        }

        [Fact]
        public async Task GetAllPlayers_WithoutToken_ReturnsUnauthorized()
        {
            // Act: Send request without a token
            var response = await _client.GetAsync("/api/player/GetAllPlayers", TestContext.Current.CancellationToken);

            // Assert: Verify the server returned a 401 Unauthorized
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllPlayers_WithGeneratedToken_ReturnsSuccessStatusCode()
        {
            // 1. Generate a valid token programmatically on the fly
            var token = TestTokenHelper.GenerateJwtToken(
                secretKey: "THIS_IS_A_VERY_LONG_AND_SECURE_SUPER_SECRET_KEY_12345!",
                issuer: "MyGameApi",
                audience: "MyGameClients"
            );

            // 2. Attach the token to the client header
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // 3. Act & Assert
            var response = await _client.GetAsync("/api/player/GetAllPlayers", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode(); // Will pass with 200 OK
        }
    }
}