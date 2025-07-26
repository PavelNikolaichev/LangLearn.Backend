using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LangLearn.Backend.Dto;
using LangLearn.Backend.Services;
using Xunit;

namespace LangLearn.Backend.Tests.IntegrationTests;

public class DeckIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"decktest{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        // Register
        var registerResp = await _client.PostAsJsonAsync("/auth/register", new { email, password });
        registerResp.EnsureSuccessStatusCode();
        // Login
        var loginResp = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        loginResp.EnsureSuccessStatusCode();
        var loginResult = await loginResp.Content.ReadFromJsonAsync<AuthResultDto>();
        return loginResult!.Token!;
    }

    [Fact]
    public async Task Deck_CRUD_Workflow()
    {
        // Authenticate
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Deck
        var createReq = new { name = "My Deck", description = "Test description" };
        var createResp = await _client.PostAsJsonAsync("/decks", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<DeckDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be(createReq.name);
        created.Description.Should().Be(createReq.description);
        created.Flashcards.Should().BeEmpty();

        // Get All Decks
        var listResp = await _client.GetAsync("/decks");
        listResp.EnsureSuccessStatusCode();
        var decks = await listResp.Content.ReadFromJsonAsync<List<DeckDto>>();
        decks.Should().ContainSingle(d => d.Id == created.Id);

        // Get By Id
        var getResp = await _client.GetAsync($"/decks/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var deck = await getResp.Content.ReadFromJsonAsync<DeckDto>();
        deck!.Id.Should().Be(created.Id);

        // Update Deck
        var updateReq = new { name = "Updated Deck", description = "Updated desc" };
        var patchResp = await _client.PatchAsJsonAsync($"/decks/{created.Id}", updateReq);
        patchResp.EnsureSuccessStatusCode();
        var updated = await patchResp.Content.ReadFromJsonAsync<DeckDto>();
        updated!.Name.Should().Be(updateReq.name);
        updated.Description.Should().Be(updateReq.description);

        // Delete Deck
        var deleteResp = await _client.DeleteAsync($"/decks/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm Deletion
        var afterDelete = await _client.GetAsync($"/decks/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}