using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Argos.Core.Discord;

public class DiscordClient : IDisposable {
    readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(25) };

    public void SetToken(string t) {
        http.DefaultRequestHeaders.Authorization = null;
        t = (t ?? "").Trim();
        if (t.StartsWith("Bot ")) t = t.Substring(4).Trim();
        if (t.Length > 0) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(t);
    }

    public async Task<string> WhoAmI(CancellationToken ct = default) {
        var r = await http.GetAsync("https://discord.com/api/v10/users/@me", ct);
        if (r.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("401 : token refuse par Discord (expire, ou copie partielle)");
        r.EnsureSuccessStatusCode();
        var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct)).RootElement;
        return j.TryGetProperty("username", out var u) ? u.GetString() : "?";
    }

    public async Task<JsonElement?> GetAsync(string path, CancellationToken ct = default) {
        var r = await http.GetAsync("https://discord.com/api/v10" + path, ct);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        if (r.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException("401 : token invalide");
        if (!r.IsSuccessStatusCode) throw new HttpRequestException("HTTP " + (int)r.StatusCode);
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct)).RootElement;
    }

    public Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default) => http.GetByteArrayAsync(url, ct);

    public void Dispose() => http.Dispose();
}