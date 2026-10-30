using Argos.Core.Discord;
using Argos.Core.Models;

namespace Argos.Core.Modules;

public class WebCollector : ICollector {
    public string Name => "web";

    public Task<List<Entry>> CollectAsync(Entry seed, DiscordClient client, CancellationToken ct) {
        var q = Uri.EscapeDataString($"\"{seed.Value}\"");
        return Task.FromResult(new List<Entry> {
            new() { Module = "web", Key = "google", Value = "https://www.google.com/search?q=" + q, Source = "web" },
            new() { Module = "web", Key = "github", Value = "https://github.com/search?q=" + q + "&type=code", Source = "code" }
        });
    }
}