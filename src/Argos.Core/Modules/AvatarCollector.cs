using System.Security.Cryptography;
using Argos.Core.Discord;
using Argos.Core.Models;

namespace Argos.Core.Modules;

public class AvatarCollector : ICollector {
    public string Name => "avatar";

    public async Task<List<Entry>> CollectAsync(Entry seed, DiscordClient client, CancellationToken ct) {
        var b = await client.GetBytesAsync(seed.Value, ct);
        var url = Uri.EscapeDataString(seed.Value);
        return new List<Entry> {
            new() { Module = "avatar", Key = "sha256", Value = Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant(), Source = "empreinte" },
            new() { Module = "avatar", Key = "reverse", Source = "visuel",
                Value = $"https://lens.google.com/uploadbyurl?url={url}\nhttps://yandex.com/images/search?rpt=imageview&url={url}" }
        };
    }
}