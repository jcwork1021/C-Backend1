using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using minimalAPIlesson1;
using minimalAPILesson1.Controllers;
using minimalAPILesson1.Data;
using minimalAPILesson1.DTO;
using minimalAPILesson1.Models;
using System;
using System.Security.Claims;
using Xunit;

namespace minimalAPIlesson1.Tests
{

    public class PlayerControllerTests
    {
        // Reusable helper to build a fresh, isolated InMemory database for any test
        private AppDBContext GetDatabaseContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new AppDBContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task GetAllPlayers_ReturnsOk_WhenPlayersExist()
        {
            // Use a unique database name per test so tests don't interfere with each other
            using var context = GetDatabaseContext("TestDb_GetAll_UniqueName");

            // Seed test data into the database
            context.Players.Add(new Player { Id = 1, Name = "Alice", Email = "alice@test.com" });
            context.Players.Add(new Player { Id = 2, Name = "Bob", Email = "bob@test.com" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new PlayerController(context);

            // Mock user authentication (since your controller checks User.Identity.IsAuthenticated)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                    new Claim(ClaimTypes.Name, "TestUser")
                }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
            };

            // 2. Act: Call the method
            var result = await controller.GetAllPlayers();

            // 3. Assert: Verify the result
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Check your ResponseDTO or returned data structure here based on your implementation
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task RegisterPlayer_ReturnsBadRequest_WhenModelIsInvalid()
        {
            using var context = GetDatabaseContext("TestDb_RegisterPlayer_InvalidModel");
            var controller = new PlayerController(context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.Name, "TestUser")
    }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
            };

            // Provide a dummy password so BCrypt doesn't throw a null exception
            var invalidPlayer = new Player { Name = "", Email = "invalidemail", Password = "123" };

            // Manually trigger model validation error
            controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await controller.CreatePlayer(invalidPlayer);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}