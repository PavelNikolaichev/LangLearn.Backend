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

namespace LangLearn.Backend.Tests.IntegrationTests
{
    public class GrammarIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public GrammarIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var email = $"grammar{Guid.NewGuid()}@example.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/auth/register", new { email, password });
            var loginResp = await _client.PostAsJsonAsync("/auth/login", new { email, password });
            loginResp.EnsureSuccessStatusCode();
            var loginResult = await loginResp.Content.ReadFromJsonAsync<AuthResultDto>();
            return loginResult!.Token!;
        }

        [Fact]
        public async Task Grammar_CRUD_Workflow()
        {
            // Authenticate
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create GrammarSet
            var setReq = new { name = "TestSet", description = "For grammar tests" };
            var setResp = await _client.PostAsJsonAsync("/grammarsets", setReq);
            setResp.EnsureSuccessStatusCode();
            var setDto = await setResp.Content.ReadFromJsonAsync<GrammarSetDto>();

            // Create Grammar
            var gramReq = new { name = "Present Simple", description = "Usage of present simple tense" };
            var createResp = await _client.PostAsJsonAsync($"/grammarsets/{setDto.Id}/grammars", gramReq);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResp.Content.ReadFromJsonAsync<GrammarDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be(gramReq.name);
            created.Description.Should().Be(gramReq.description);

            // Get All Grammar
            var listResp = await _client.GetAsync($"/grammarsets/{setDto.Id}/grammars");
            listResp.EnsureSuccessStatusCode();
            var all = await listResp.Content.ReadFromJsonAsync<List<GrammarDto>>();
            all.Should().ContainSingle(g => g.Id == created.Id);

            // Get By Id
            var getResp = await _client.GetAsync($"/grammarsets/{setDto.Id}/grammars/{created.Id}");
            getResp.EnsureSuccessStatusCode();
            var getDto = await getResp.Content.ReadFromJsonAsync<GrammarDto>();
            getDto!.Id.Should().Be(created.Id);

            // Update Grammar
            var updateReq = new { name = "Past Simple", description = "Usage of past simple tense" };
            var patchResp = await _client.PatchAsJsonAsync($"/grammarsets/{setDto.Id}/grammars/{created.Id}", updateReq);
            patchResp.EnsureSuccessStatusCode();
            var updated = await patchResp.Content.ReadFromJsonAsync<GrammarDto>();
            updated!.Name.Should().Be(updateReq.name);
            updated.Description.Should().Be(updateReq.description);

            // Delete Grammar
            var deleteResp = await _client.DeleteAsync($"/grammarsets/{setDto.Id}/grammars/{created.Id}");
            deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Confirm Deletion
            var afterDelete = await _client.GetAsync($"/grammarsets/{setDto.Id}/grammars/{created.Id}");
            afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
