using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using minimalAPILesson1.Models;
using System.Text;
using System.Text.Json;

namespace minimalAPILesson1.Tests.IntegrationTests
{
    public class PlayerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PlayerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // This spins up your actual API in memory!
            _client = factory.CreateClient(); //NOTE: this method from WebApplicationFactory creates a real HttpClient that can send requests to your API endpoints.
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