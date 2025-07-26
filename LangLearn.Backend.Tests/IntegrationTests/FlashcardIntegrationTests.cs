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

public class FlashcardIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"flashcard{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        await _client.PostAsJsonAsync("/auth/register", new { email, password }).ConfigureAwait(false);
        var loginResp = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        loginResp.EnsureSuccessStatusCode();
        var loginResult = await loginResp.Content.ReadFromJsonAsync<AuthResultDto>();
        return loginResult!.Token!;
    }

    [Fact]
    public async Task Flashcard_CRUD_Workflow()
    {
        // Authenticate
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Deck for Flashcard tests
        var deckReq = new { name = "FlashDeck", description = "Deck for flashcards" };
        var deckResp = await _client.PostAsJsonAsync("/decks", deckReq);
        deckResp.EnsureSuccessStatusCode();
        var deck = await deckResp.Content.ReadFromJsonAsync<DeckDto>();

        // Create Flashcard
        var fcReq = new { front = "Hello", back = "Bonjour", notes = "Greeting" };
        var createResp = await _client.PostAsJsonAsync($"/decks/{deck!.Id}/flashcards", fcReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<FlashcardDto>();
        created.Should().NotBeNull();
        created!.Front.Should().Be(fcReq.front);
        created.Back.Should().Be(fcReq.back);
        created.Notes.Should().Be(fcReq.notes);

        // Get All Flashcards
        var listResp = await _client.GetAsync($"/decks/{deck.Id}/flashcards");
        listResp.EnsureSuccessStatusCode();
        var flashcards = await listResp.Content.ReadFromJsonAsync<List<FlashcardDto>>();
        flashcards.Should().ContainSingle(f => f.Id == created.Id);

        // Get By Id
        var getResp = await _client.GetAsync($"/decks/{deck.Id}/flashcards/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fc = await getResp.Content.ReadFromJsonAsync<FlashcardDto>();
        fc!.Id.Should().Be(created.Id);

        // Update Flashcard
        var updateReq = new { front = "Hi", back = "Salut", notes = "Short greeting" };
        var patchResp = await _client.PatchAsJsonAsync($"/decks/{deck.Id}/flashcards/{created.Id}", updateReq);
        patchResp.EnsureSuccessStatusCode();
        var updated = await patchResp.Content.ReadFromJsonAsync<FlashcardDto>();
        updated!.Front.Should().Be(updateReq.front);
        updated.Back.Should().Be(updateReq.back);
        updated.Notes.Should().Be(updateReq.notes);

        // Delete Flashcard
        var deleteResp = await _client.DeleteAsync($"/decks/{deck.Id}/flashcards/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm Deletion
        var afterDelete = await _client.GetAsync($"/decks/{deck.Id}/flashcards/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}