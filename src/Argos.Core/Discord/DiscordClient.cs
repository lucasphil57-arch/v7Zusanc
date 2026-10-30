using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Argos.Core.Discord;

public class DiscordClient : IDisposable {
    readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(25) };

    public void SetToken(string t) {
        http.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrWhiteSpace(t)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(t);
    }

    public async Task<JsonElement?> GetAsync(string path, CancellationToken ct = default) {
        var r = await http.GetAsync("https://discord.com/api/v10" + path, ct);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        if (r.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException("token invalide");
        r.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct)).RootElement;
    }

    public Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default) => http.GetByteArrayAsync(url, ct);

    public void Dispose() => http.Dispose();
}