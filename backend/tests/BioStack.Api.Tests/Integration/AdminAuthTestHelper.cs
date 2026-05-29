namespace BioStack.Api.Tests.Integration;

using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Contracts.Requests;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static class AdminAuthTestHelper
{
    public static async Task SignInAsAdminAsync(
        HttpClient client,
        WebApplicationFactory<Program> factory,
        string email,
        string redirectPath = "/admin")
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", redirectPath));

        using var inboxDoc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var magicLink = inboxDoc.RootElement
            .EnumerateArray()
            .FirstOrDefault(message =>
                string.Equals(
                    message.GetProperty("contact").GetString(),
                    email,
                    StringComparison.OrdinalIgnoreCase))
            .GetProperty("link")
            .GetString();

        if (string.IsNullOrWhiteSpace(magicLink))
        {
            throw new InvalidOperationException($"Expected magic link for '{email}' in /dev/auth/inbox.");
        }

        var verifyUri = new Uri(magicLink);
        await client.GetAsync($"{verifyUri.AbsolutePath}{verifyUri.Query}");

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            userId = await db.AppUsers
                .Where(user => user.Email == email)
                .Select(user => user.Id)
                .SingleAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                throw new InvalidOperationException($"Expected user '{email}' to exist.");
            }

            user.Role = UserRole.Admin;
            await db.SaveChangesAsync();
        }

        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", redirectPath));

        using var refreshedInboxDoc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var refreshedMagicLink = refreshedInboxDoc.RootElement
            .EnumerateArray()
            .FirstOrDefault(message =>
                string.Equals(
                    message.GetProperty("contact").GetString(),
                    email,
                    StringComparison.OrdinalIgnoreCase))
            .GetProperty("link")
            .GetString();

        if (string.IsNullOrWhiteSpace(refreshedMagicLink))
        {
            throw new InvalidOperationException($"Expected refreshed magic link for '{email}' in /dev/auth/inbox.");
        }

        var refreshedVerifyUri = new Uri(refreshedMagicLink);
        await client.GetAsync($"{refreshedVerifyUri.AbsolutePath}{refreshedVerifyUri.Query}");
    }
}
