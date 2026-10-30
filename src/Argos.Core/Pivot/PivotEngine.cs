using Argos.Core.Models;

namespace Argos.Core.Pivot;

public record Pivot(int EntryIndex, string Label, string Module, string Kind, string Url, string Why, double Yield);

public static class PivotEngine {
    public static List<Pivot> Suggest(Dossier d) {
        var r = new List<Pivot>();
        for (int i = 0; i < d.Entries.Count; i++) {
            var e = d.Entries[i];
            void A(string l, string m, string k, double y, string w, string u = null) => r.Add(new Pivot(i, l, m, k, u, w, y));
            switch (e.Type) {
                case NodeType.DiscordId:
                    A("Profil Discord", "discord", "auto", .95, "pseudo, bio, comptes");
                    A("Avatar reverse", "avatar", "auto", .45, "origine photo");
                    A("ID sur le web", "web", "auto", .55, "posts, configs"); break;
                case NodeType.Username:
                    A("Recherche web", "web", "auto", .6, "google, github");
                    A("Whatsmyname", "lien", "lien", .8, "300 sites", "https://whatsmyname.app/?q=" + Uri.EscapeDataString(e.Value)); break;
                case NodeType.Email:
                    A("HIBP", "lien", "lien", .9, "fuites", "https://haveibeenpwned.com/account/" + Uri.EscapeDataString(e.Value));
                    A("Epieos", "lien", "lien", .85, "compte google", "https://epieos.com/"); break;
                case NodeType.Phone:
                    A("Truecaller", "lien", "lien", .9, "nom", "https://www.truecaller.com/search/fr/" + new string(e.Value.Where(char.IsDigit).ToArray()));
                    A("Pages Blanches", "lien", "lien", .75, "adresse", "https://www.pagesjaunes.fr/pagesblanches"); break;
                case NodeType.RealName:
                    A("Pappers", "lien", "lien", .85, "societes", "https://www.pappers.fr/recherche?q=" + Uri.EscapeDataString(e.Value));
                    A("Pages Blanches", "lien", "lien", .8, "adresse", "https://www.pagesjaunes.fr/pagesblanches"); break;
            }
        }
        return r.OrderByDescending(x => x.Yield).ToList();
    }
}