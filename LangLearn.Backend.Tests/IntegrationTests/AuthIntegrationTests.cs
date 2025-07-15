using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LangLearn.Backend.Services;
using Xunit;

namespace LangLearn.Backend.Tests.IntegrationTests;

public class AuthIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_Login_GetAuth_Workflow()
    {
        // Arrange
        var email = $"test{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        // Act - Register
        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new { email, password });

        // Assert
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();

        // Act - Get Authenticated Info
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
        var authInfoResponse = await _client.GetAsync("/auth");

        // Assert
        authInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var info = await authInfoResponse.Content.ReadFromJsonAsync<UserInfo>();
        info.Should().NotBeNull();
        info!.Email.Should().Be(email);
        info.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_With_Existing_Email_Should_Fail()
    {
        var email = $"duptest{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        // Register first time
        var resp1 = await _client.PostAsJsonAsync("/auth/register", new { email, password });
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        // Register second time
        var resp2 = await _client.PostAsJsonAsync("/auth/register", new { email, password });
        resp2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Should_Fail()
    {
        var email = $"notfound{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var resp = await _client.PostAsJsonAsync("/auth/login", new { email, password });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class UserInfo
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
    }
}