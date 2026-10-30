using Argos.Core.Models;
using Argos.Core.Pivot;
using Xunit;

public class PivotTests {
    [Fact] public void Id_produit_pivots() {
        var d = new Dossier();
        d.Entries.Add(new Entry { Type = NodeType.DiscordId, Value = "184405311681986560" });
        Assert.NotEmpty(PivotEngine.Suggest(d));
    }
    [Fact] public void Trie_decroissant() {
        var d = new Dossier();
        d.Entries.Add(new Entry { Type = NodeType.Phone, Value = "0600000000" });
        d.Entries.Add(new Entry { Type = NodeType.DiscordId, Value = "184405681986560" });
        var p = PivotEngine.Suggest(d);
        Assert.True(p[0].Yield >= p[^1].Yield);
    }
}