using minimalAPILesson1.Data;
using minimalAPILesson1.Models;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using minimalAPILesson1.DTO;
using MiniValidation;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Numerics;
using System.Security.Claims;
using System.Text;
using minimalAPILesson1.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// --- 1. ADD JWT AUTHENTICATION SERVICES ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing")))
    };
});

// --- 2. ADD JWT AUTHORIZATION SERVICES ---
builder.Services.AddAuthorization(); // Required for authorization rules

// ... (your other services like AddDbContext, etc.)
builder.Services.AddDbContext<AppDBContext>();
TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

var app = builder.Build();

// --- 2. ADD MIDDLEWARE IN THE CORRECT ORDER ---
app.UseAuthentication(); // Must come BEFORE Authorization
app.UseAuthorization();  // Checks if the user is allowed to access the endpoint



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/", () => "Wassup");

app.MapGet("api/player/MapPlayerList", async (HttpContext httpContext, AppDBContext context) =>
{
    // 1. Check if the user is authenticated (has a valid token)
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Json(new ResponseDTO<object>(
            Message: "Unauthorized: You must be logged in to view player list.",
            StatusCode: 401,
            Data: null
        ), statusCode: 401);
    }

    var players = await context.Players.Select(p => new PlayerDTO(p.Id, p.Name, p.Email)).ToListAsync();

    return Results.Ok(new ResponseDTO<List<PlayerDTO>>(
        Message: "Players retrieved successfully",
        StatusCode: 200,
        Data: players
    ));

});

app.MapPatch("api/player/MapUpdatePlayer/{playerID}", async (AppDBContext context, int playerID, UpdatePlayerDto updatedPlayer) =>
{
    var existingPlayer = await context.Players.FindAsync(playerID);
    if (existingPlayer == null)
    {
        return Results.NotFound(new ApiResponse<Player>
        {
            Message = "Player does not exist",
            StatusCode = 404,
            Data = null
        });
    }

    updatedPlayer.Adapt(existingPlayer);

    await context.SaveChangesAsync();
    return Results.Ok(new ApiResponse<Player>
    {
        Message = "Player Updated Succesfully",
        StatusCode = 200,
        Data = existingPlayer
    });
});

app.MapPost("/api/player/MapCreatePlayer", async (AppDBContext context, Player newPlayer) =>
{

    if (!MiniValidator.TryValidate(newPlayer, out var errors))
    {
        return Results.ValidationProblem(errors);
    }

    context.Players.Add(newPlayer);
    await context.SaveChangesAsync();
    return Results.Ok(newPlayer);
});

app.MapPost("api/player/MapLogin", async (AppDBContext context, LoginDTO loginDTO, IConfiguration configuration) =>
{
    var player = await context.Players.FirstOrDefaultAsync(p => p.Email == loginDTO.Email);
    if (player == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, player.Password))
    {
        return Results.NotFound(new ResponseDTO<Player>
        (
            Message: "Invalid email or password",
            StatusCode: 404,
            Data: null
        ));
    }

    // 1. Generate the tokens using our helper methods
    var playerController = new PlayerController(context);
    var accessToken = playerController.GenerateAccessToken(player, configuration);
    var refreshToken = playerController.GenerateRefreshToken();

    // 2. Save the Refresh Token and its expiry to the database
    player.RefreshToken = refreshToken;
    player.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Valid for 7 days
    await context.SaveChangesAsync();

    // 3. Return the response
    return Results.Ok(new ResponseDTO<LoginResponseDTO>(
        Message: "Login successful",
        StatusCode: 200,
        Data: new LoginResponseDTO(
            ID: player.Id,
            ShortToken: accessToken,
            LongToken: refreshToken,
            Name: player.Name,
            Age: player.Age,
            Year: player.Year,
            Section: player.Section,
            Level: player.Level
        )
    ));
});

app.MapDelete("/api/player/MapDeletePlayer/{playerID}", async (AppDBContext context, int playerID) =>
{
    if (playerID == 0)
    {
        return Results.BadRequest(new ApiResponse<Player>
        {
            Message = "Invalid player ID",
            StatusCode = 400,
            Data = null
        });
    }

    var checkPlayer = await context.Players.FindAsync(playerID);

    if (checkPlayer == null)
    {
        return Results.NotFound(new ApiResponse<Player>
        {
            Message = "Player does not exist",
            StatusCode = 404,
            Data = null
        });
    }

    // Remove and save changes
    context.Players.Remove(checkPlayer);
    await context.SaveChangesAsync();

    return Results.Ok(new ApiResponse<Player>
    {
        Message = "Player Deleted Succesfully",
        StatusCode = 200,
        Data = null
    });
});


// Automatically apply pending EF Core migrations on startup (skipped during integration tests)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();
        dbContext.Database.Migrate();
    }
}

app.Run();

public partial class Program { }
