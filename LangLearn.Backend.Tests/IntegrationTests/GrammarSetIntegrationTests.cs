using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LangLearn.Backend.Dtos;
using LangLearn.Backend.Services;
using Xunit;

namespace LangLearn.Backend.Tests.IntegrationTests
{
    public class GrammarSetIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public GrammarSetIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var email = $"grammarset{Guid.NewGuid()}@example.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/auth/register", new { email, password });
            var loginResp = await _client.PostAsJsonAsync("/auth/login", new { email, password });
            loginResp.EnsureSuccessStatusCode();
            var loginResult = await loginResp.Content.ReadFromJsonAsync<AuthResult>();
            return loginResult!.Token!;
        }

        [Fact]
        public async Task GrammarSet_CRUD_Workflow()
        {
            // Authenticate
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create GrammarSet
            var createReq = new { name = "My Set", description = "Test grammar set" };
            var createResp = await _client.PostAsJsonAsync("/grammarsets", createReq);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResp.Content.ReadFromJsonAsync<GrammarSetDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be(createReq.name);
            created.Description.Should().Be(createReq.description);
            created.Grammars.Should().BeEmpty();

            // Get All
            var listResp = await _client.GetAsync("/grammarsets");
            listResp.EnsureSuccessStatusCode();
            var sets = await listResp.Content.ReadFromJsonAsync<List<GrammarSetDto>>();
            sets.Should().ContainSingle(s => s.Id == created.Id);

            // Get By Id
            var getResp = await _client.GetAsync($"/grammarsets/{created.Id}");
            getResp.EnsureSuccessStatusCode();
            var set = await getResp.Content.ReadFromJsonAsync<GrammarSetDto>();
            set!.Id.Should().Be(created.Id);

            // Update
            var updateReq = new { name = "Updated Set", description = "Updated desc" };
            var patchResp = await _client.PatchAsJsonAsync($"/grammarsets/{created.Id}", updateReq);
            patchResp.EnsureSuccessStatusCode();
            var updated = await patchResp.Content.ReadFromJsonAsync<GrammarSetDto>();
            updated!.Name.Should().Be(updateReq.name);
            updated.Description.Should().Be(updateReq.description);

            // Delete
            var deleteResp = await _client.DeleteAsync($"/grammarsets/{created.Id}");
            deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Confirm deletion
            var afterDelete = await _client.GetAsync($"/grammarsets/{created.Id}");
            afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}