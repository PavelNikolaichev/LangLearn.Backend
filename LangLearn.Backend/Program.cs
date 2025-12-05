using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using LangLearn.Backend.Data;
using LangLearn.Backend.Dto;
using LangLearn.Backend.Models;
using LangLearn.Backend.Services;
using LangLearn.Backend.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// added for StatusCodes

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        x => x.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("InMemoryLangLearnTestDb");
    }
    else
    {
        options.UseSqlite("Data Source=LangLearn.db");
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                                       throw new InvalidOperationException(
                                           "JWT Key is not configured in appsettings or secrets."))),
            ClockSkew = TimeSpan.FromMinutes(5) // Tolerance for clock differences
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(context.Exception.Message);
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DeckService>();
builder.Services.AddScoped<FlashcardService>();
builder.Services.AddScoped<GrammarSetService>();
builder.Services.AddScoped<GrammarService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LangLearn API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

app.UseCors("AllowAllOrigins");

// Swagger is only enabled in Development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/register", async (RegisterRequestDto requestDto, AuthService authService) =>
    {
        var result = await authService.RegisterAsync(requestDto);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result.Message);
    })
    .Produces<AuthResultDto>()
    .Produces(StatusCodes.Status400BadRequest);

app.MapPost("/auth/login", async (LoginRequestDto requestDto, AuthService authService) =>
    {
        var result = await authService.LoginAsync(requestDto);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result.Message);
    })
    .Produces<AuthResultDto>()
    .Produces(StatusCodes.Status400BadRequest);

app.MapGet("/auth", (ClaimsPrincipal user) =>
    {
        var userId = UserService.GetUserId(user);
        var email = UserService.GetUserEmail(user);

        return userId.HasValue
            ? Results.Ok(new { userId = userId.Value, email })
            : Results.Unauthorized();
    })
    .Produces<AuthResultDto>()
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPost("/auth/refresh", async (RefreshTokenRequestDto request, AuthService authService, ILogger<Program> logger) =>
    {
        // Important for frontend:
        // - Send: POST /auth/refresh with body { "token": "<expiredToken>" } and Content-Type: application/json
        // - Do NOT send an Authorization: Bearer <token> header (expired tokens won't authenticate)
        // - Success: 200 OK with { token: "...", expiresAt: "..." }
        // - Failure: 401 Unauthorized (no body)
        var result = await authService.RefreshTokenAsync(request.Token);

        logger.LogDebug("Refresh token attempted. Was refreshed: {WasRefreshed}", result.Success);

        return result.Success
            ? Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt })
            : Results.Unauthorized();
    })
    .Produces<AuthResultDto>()
    .Produces(StatusCodes.Status401Unauthorized);

// Deck endpoints
app.MapGet("/decks", async (DeckService deckService, ClaimsPrincipal user) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var decks = await deckService.GetUserDecksAsync(userId.Value);
        return Results.Ok(decks);
    })
    .Produces<IEnumerable<DeckDto>>()
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPost("/decks", async (DeckService deckService, ClaimsPrincipal user, Deck deck) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var createdDeck = await deckService.CreateDeckAsync(deck, userId.Value);
        return Results.Created($"/decks/{createdDeck.Id}", createdDeck);
    })
    .Produces<DeckDto>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapGet("/decks/{id}", async (DeckService deckService, ClaimsPrincipal user, Guid id) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var deck = await deckService.GetDeckByIdAsync(id, userId.Value);
        return deck != null ? Results.Ok(deck) : Results.NotFound();
    })
    .Produces<DeckDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPatch("/decks/{id}", async (DeckService deckService, ClaimsPrincipal user, Guid id, Deck deck) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var updatedDeck = await deckService.UpdateDeckAsync(id, deck, userId.Value);
        return updatedDeck != null ? Results.Ok(updatedDeck) : Results.NotFound();
    })
    .Produces<DeckDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapDelete("/decks/{id}", async (DeckService deckService, ClaimsPrincipal user, Guid id) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var deleted = await deckService.DeleteDeckAsync(id, userId.Value);
        return deleted ? Results.NoContent() : Results.NotFound();
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

// Flashcard endpoints
app.MapGet("/decks/{deckId}/flashcards", async (FlashcardService flashcardService, ClaimsPrincipal user, Guid deckId) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var flashcards = await flashcardService.GetDeckFlashcardsAsync(deckId, userId.Value);
        return Results.Ok(flashcards);
    })
    .Produces<IEnumerable<FlashcardDto>>()
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPost("/decks/{deckId}/flashcards",
        async (FlashcardService flashcardService, ClaimsPrincipal user, Guid deckId, Flashcard flashcard) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var createdFlashcard = await flashcardService.CreateFlashcardAsync(flashcard, deckId, userId.Value);
            return createdFlashcard != null
                ? Results.Created($"/decks/{deckId}/flashcards/{createdFlashcard.Id}", createdFlashcard)
                : Results.NotFound();
        })
    .Produces<FlashcardDto>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapGet("/decks/{deckId}/flashcards/{flashcardId}",
        async (FlashcardService flashcardService, ClaimsPrincipal user, Guid deckId, Guid flashcardId) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var flashcard = await flashcardService.GetFlashcardByIdAsync(flashcardId, deckId, userId.Value);
            return flashcard != null ? Results.Ok(flashcard) : Results.NotFound();
        })
    .Produces<FlashcardDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPatch("/decks/{deckId}/flashcards/{flashcardId}", async (FlashcardService flashcardService, ClaimsPrincipal user,
        Guid deckId, Guid flashcardId, Flashcard updatedFlashcard) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var flashcard =
            await flashcardService.UpdateFlashcardAsync(flashcardId, deckId, updatedFlashcard, userId.Value);
        return flashcard != null ? Results.Ok(flashcard) : Results.NotFound();
    })
    .Produces<FlashcardDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapDelete("/decks/{deckId}/flashcards/{flashcardId}",
        async (FlashcardService flashcardService, ClaimsPrincipal user, Guid deckId, Guid flashcardId) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var deleted = await flashcardService.DeleteFlashcardAsync(flashcardId, deckId, userId.Value);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

// GrammarSet endpoints
app.MapGet("/grammarsets", async (GrammarSetService grammarSetService, ClaimsPrincipal user) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var grammarSets = await grammarSetService.GetUserGrammarSetsAsync(userId.Value);
        return Results.Ok(grammarSets);
    })
    .Produces<IEnumerable<GrammarSetDto>>()
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPost("/grammarsets", async (GrammarSetService grammarSetService, ClaimsPrincipal user, GrammarSet grammarSet) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var createdGrammarSet = await grammarSetService.CreateGrammarSetAsync(grammarSet, userId.Value);
        return Results.Created($"/grammarsets/{createdGrammarSet.Id}", createdGrammarSet);
    })
    .Produces<GrammarSetDto>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapGet("/grammarsets/{id}", async (GrammarSetService grammarSetService, ClaimsPrincipal user, Guid id) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var grammarSet = await grammarSetService.GetGrammarSetByIdAsync(id, userId.Value);
        return grammarSet != null ? Results.Ok(grammarSet) : Results.NotFound();
    })
    .Produces<GrammarSetDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPatch("/grammarsets/{id}",
        async (GrammarSetService grammarSetService, ClaimsPrincipal user, Guid id, GrammarSet updatedGrammarSet) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var grammarSet = await grammarSetService.UpdateGrammarSetAsync(id, updatedGrammarSet, userId.Value);
            return grammarSet != null ? Results.Ok(grammarSet) : Results.NotFound();
        })
    .Produces<GrammarSetDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapDelete("/grammarsets/{id}", async (GrammarSetService grammarSetService, ClaimsPrincipal user, Guid id) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var deleted = await grammarSetService.DeleteGrammarSetAsync(id, userId.Value);
        return deleted ? Results.NoContent() : Results.NotFound();
    })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

// Grammar endpoints
app.MapGet("/grammarsets/{setId}/grammars", async (GrammarService grammarService, ClaimsPrincipal user, Guid setId) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var grammars = await grammarService.GetGrammarSetGrammarsAsync(setId, userId.Value);
        return Results.Ok(grammars);
    })
    .Produces<IEnumerable<GrammarDto>>()
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPost("/grammarsets/{setId}/grammars",
        async (GrammarService grammarService, ClaimsPrincipal user, Guid setId, Grammar grammar) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var createdGrammar = await grammarService.CreateGrammarAsync(grammar, setId, userId.Value);
            return createdGrammar != null
                ? Results.Created($"/grammarsets/{setId}/grammars/{createdGrammar.Id}", createdGrammar)
                : Results.NotFound();
        })
    .Produces<GrammarDto>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapGet("/grammarsets/{setId}/grammars/{grammarId}",
        async (GrammarService grammarService, ClaimsPrincipal user, Guid setId, Guid grammarId) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var grammar = await grammarService.GetGrammarByIdAsync(grammarId, setId, userId.Value);
            return grammar != null ? Results.Ok(grammar) : Results.NotFound();
        })
    .Produces<GrammarDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapPatch("/grammarsets/{setId}/grammars/{grammarId}", async (GrammarService grammarService, ClaimsPrincipal user,
        Guid setId, Guid grammarId, Grammar updatedGrammar) =>
    {
        var userId = UserService.GetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();

        var grammar = await grammarService.UpdateGrammarAsync(grammarId, setId, updatedGrammar, userId.Value);
        return grammar != null ? Results.Ok(grammar) : Results.NotFound();
    })
    .Produces<GrammarDto>()
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.MapDelete("/grammarsets/{setId}/grammars/{grammarId}",
        async (GrammarService grammarService, ClaimsPrincipal user, Guid setId, Guid grammarId) =>
        {
            var userId = UserService.GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var deleted = await grammarService.DeleteGrammarAsync(grammarId, setId, userId.Value);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.Run();

public partial class Program;