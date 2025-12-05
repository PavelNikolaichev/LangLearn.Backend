using System.Collections.Generic;
using System.Linq;
using LangLearn.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LangLearn.Backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use test environment so Program.cs picks InMemory provider
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Add in-memory configuration for JWT key (must be at least 256 bits for HS256)
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "test_secret_key_minimum_32_characters_required_for_hs256_algorithm!"
            });
        });
        builder.ConfigureServices(services =>
        {
            // Ensure the in-memory test database is clean
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }
}