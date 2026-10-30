using Argos.Core.Discord;
using Argos.Core.Models;

namespace Argos.Core.Modules;

public interface ICollector {
    string Name { get; }
    Task<List<Entry>> CollectAsync(Entry seed, DiscordClient client, CancellationToken ct);
}