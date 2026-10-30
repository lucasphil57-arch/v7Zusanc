using System.Text.Json;
using System.Text.RegularExpressions;
using Argos.Core.Discord;
using Argos.Core.Models;

namespace Argos.Core.Modules;

public class DiscordCollector : ICollector {
    public string Name => "discord";
    static readonly Regex Mail = new(@"[\w.+-]+@[\w-]+\.[\w.]+", RegexOptions.Compiled);
    static readonly Regex Tel = new(@"(?:\+\d{1,3}[\s.\-]?)?(?:\d[\s.\-]?){9,13}\d", RegexOptions.Compiled);

    public async Task<List<Entry>> CollectAsync(Entry seed, DiscordClient client, CancellationToken ct) {
        var res = new List<Entry>();
        if (!Snowflake.TryParse(seed.Value, out var id)) return res;

        res.Add(new Entry { Module = "identite", Key = "creation", Source = "calcul local", Value = Snowflake.Created(id).ToString("G") });
        foreach (var f in Snowflake.Flags(id))
            res.Add(new Entry { Module = "identite", Key = "alerte", Source = "heuristique", Value = f });

        var u = await client.GetAsync($"/users/{id}", ct);
        if (u is null) throw new InvalidOperationException("compte inconnu");

        if (u.Value.TryGetProperty("username", out var n))
            res.Add(new Entry { Module = "discord", Key = "pseudo", Value = n.GetString(), Type = NodeType.Username, Source = "api" });
        if (u.Value.TryGetProperty("avatar", out var av) && av.ValueKind == JsonValueKind.String)
            res.Add(new Entry { Module = "discord", Key = "avatar", Type = NodeType.Avatar, Source = "api",
                Value = $"https://cdn.discordapp.com/avatars/{id}/{av.GetString()}.png?size=1024" });

        var p = await client.GetAsync($"/users/{id}/profile?with_mutual_guilds=true", ct);
        if (p is null) return res;

        if (p.Value.TryGetProperty("user_profile", out var up) && up.TryGetProperty("bio", out var bio) && bio.ValueKind == JsonValueKind.String) {
            var t = bio.GetString();
            foreach (Match m in Mail.Matches(t))
                res.Add(new Entry { Module = "discord", Key = "email", Value = m.Value, Type = NodeType.Email, Source = "bio" });
            foreach (Match m in Tel.Matches(t))
                res.Add(new Entry { Module = "discord", Key = "telephone", Value = m.Value.Trim(), Type = NodeType.Phone, Source = "bio" });
        }
        if (p.Value.TryGetProperty("connected_accounts", out var ca))
            foreach (var a in ca.EnumerateArray()) {
                var ty = a.GetProperty("type").GetString();
                if (a.TryGetProperty("name", out var nm) && ty != "email")
                    res.Add(new Entry { Module = "compte " + ty, Key = ty, Value = nm.GetString(), Type = NodeType.Username, Source = "compte lie" });
            }
        return res;
    }
}