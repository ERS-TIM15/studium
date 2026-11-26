using System.Net.Http.Headers;
using System.Security.Cryptography;
using Infrastructure.Demo;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

/// <summary>
/// Pomoćne metode za integracione testove: prijavljeni klijent sa sesijom (US-7.5).
/// Sesije se zadaju kroz isti DI kontejner koji koristi aplikacija, kao što to
/// Development okruženje radi pri pokretanju, pa testovi ne zaobilaze autentifikaciju.
/// </summary>
internal static class SessionClients
{
    public static HttpClient CreateForStudent(WebApplicationFactory<Program> factory) =>
        CreateFor(factory, DemoData.DemoStudentId);

    public static HttpClient CreateForTeacher(WebApplicationFactory<Program> factory) =>
        CreateFor(factory, DemoData.DemoTeacherId);

    public static HttpClient CreateForAssistant(WebApplicationFactory<Program> factory) =>
        CreateFor(factory, DemoData.DemoAssistantId);

    public static HttpClient CreateFor(WebApplicationFactory<Program> factory, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var sessions = factory.Services.GetRequiredService<InMemorySessionReader>();
        var accounts = factory.Services.GetRequiredService<InMemoryAccountStatusReader>();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        accounts.Activate(userId);
        sessions.Register(token, userId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
