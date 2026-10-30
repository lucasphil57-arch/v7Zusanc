using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Argos.Core.Discord;
using Argos.Core.Models;
using Argos.Core.Modules;
using Argos.Core.Pivot;
using Argos.Core.Storage;
using Argos.App.Ui;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace Argos.App;

public class Card {
    public string Header { get; set; }
    public string Value { get; set; }
    public string Source { get; set; }
    public Brush Accent { get; set; }
}

public class PivotRow {
    public Pivot P { get; set; }
    public string Label => P.Label;
    public string Sub => ((int)(P.Yield * 100)) + "%   " + P.Why;
}

public partial class MainWindow : Window {
    readonly DossierStore store = new(Path.Combine(AppContext.BaseDirectory, "argos.json"));
    readonly DiscordClient api = new();
    readonly Dictionary<string, ICollector> mods = new();
    readonly ObservableCollection<string> caseLabels = new();
    readonly ObservableCollection<Card> cards = new();
    readonly ObservableCollection<PivotRow> pivotRows = new();
    List<Dossier> data;
    int cur = -1;
    bool syncing;
    bool hasToken;
    static readonly NodeType[] Map = { NodeType.DiscordId, NodeType.Username, NodeType.Email, NodeType.Phone, NodeType.RealName, NodeType.Address };
    static readonly Brush B_DIM = new SolidColorBrush(Color.FromRgb(88, 97, 110));
    static readonly Brush B_ACC = new SolidColorBrush(Color.FromRgb(88, 101, 242));
    static readonly Brush B_WARN = new SolidColorBrush(Color.FromRgb(245, 158, 11));
    static readonly Brush B_OK = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    static readonly Brush B_RED = new SolidColorBrush(Color.FromRgb(239, 68, 68));
    static readonly Brush B_VIO = new SolidColorBrush(Color.FromRgb(139, 92, 246));

    public MainWindow() {
        InitializeComponent();
        foreach (var c in new ICollector[] { new DiscordCollector(), new AvatarCollector(), new WebCollector() }) mods[c.Name] = c;
        data = store.Load();
        CaseList.ItemsSource = caseLabels;
        Feed.ItemsSource = cards;
        PivotList.ItemsSource = pivotRows;
        Loaded += async (s, e) => {
            var saved = TokenStore.Load();
            if (saved.Length > 0) { api.SetToken(saved); await Verify(saved); }
            Render();
        };
    }

    void Say(string m) {
        LogBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + m + Environment.NewLine);
        LogBox.ScrollToEnd();
        StatusText.Text = m;
    }

    async Task Verify(string t) {
        try {
            var who = await api.WhoAmI();
            TokenState.Text = "connecte : " + who;
            TokenState.Foreground = B_OK;
            hasToken = true;
            TokenStore.Save(t);
        } catch (Exception ex) {
            TokenState.Text = "invalide";
            MessageBox.Show("Token refuse par Discord." + Environment.NewLine + ex.Message, "Argos");
            TokenState.Foreground = B_RED;
            Say("token erreur : " + ex.Message);
        }
    }

    static string Extract(string raw) {
        var m = Regex.Match(raw ?? "", @"[\w\-]{20,}\.[\w\-]{5,}\.[\w\-]{20,}");
        return m.Success ? m.Value : (raw ?? "").Trim();
    }

    void OnPaste(object s, RoutedEventArgs e) {
        var t = Extract(Clipboard.GetText());
        if (t.Length < 20) { MessageBox.Show("Presse-papiers vide ou token incomplet.", "Argos"); return; }
        TokenBox.Password = t;
        OnToken(s, e);
    }

    async void OnToken(object s, RoutedEventArgs e) {
        var t = Extract(TokenBox.Password);
        if (t.Length == 0) return;
        api.SetToken(t);
        TokenState.Text = "verification... (" + t.Length + " car.)";
        await Verify(t);
        TokenBox.Clear();
    }

    void OnTokenKey(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { OnToken(s, e); e.Handled = true; } }

    void OnNewCase(object s, RoutedEventArgs e) {
        data.Add(new Dossier { Name = "dossier " + DateTime.Now.ToString("dd/MM HH:mm") });
        cur = data.Count - 1;
        store.Save(data);
        Render();
    }

    void OnCaseChanged(object s, SelectionChangedEventArgs e) {
        if (syncing) return;
        if (CaseList.SelectedIndex >= 0) { cur = CaseList.SelectedIndex; Render(); }
    }

    void Render() {
        syncing = true;
        caseLabels.Clear();
        foreach (var d in data) caseLabels.Add(d.Name + "   " + d.Entries.Count);
        if (cur >= 0 && cur < caseLabels.Count) CaseList.SelectedIndex = cur;
        syncing = false;
        cards.Clear();
        pivotRows.Clear();
        EmptyText.Visibility = Visibility.Visible;
        if (cur < 0 || cur >= data.Count) return;
        if (data[cur].Entries.Count == 0) return;
        EmptyText.Visibility = Visibility.Collapsed;
        foreach (var en in data[cur].Entries)
            cards.Add(new Card { Header = (en.Module + "  /  " + en.Key).ToLower(), Value = en.Value, Source = en.Source, Accent = AccentOf(en.Type) });
        foreach (var p in PivotEngine.Suggest(data[cur]))
            pivotRows.Add(new PivotRow { P = p });
        StatusText.Text = data[cur].Entries.Count + " entites";
    }

    static Brush AccentOf(NodeType t) => t switch {
        NodeType.DiscordId => B_ACC,
        NodeType.Email => B_WARN,
        NodeType.Phone => B_OK,
        NodeType.RealName => B_RED,
        NodeType.Username => B_VIO,
        _ => B_DIM
    };

    void OnSeedKey(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { AddSeed(); e.Handled = true; } }
    void OnSeed(object s, RoutedEventArgs e) => AddSeed();

    void AddSeed() {
        var v = SeedBox.Text.Trim();
        if (v.Length == 0) return;
        if (cur < 0) { data.Add(new Dossier { Name = "dossier " + DateTime.Now.ToString("dd/MM HH:mm") }); cur = data.Count - 1; }
        var t = Map[TypeBox.SelectedIndex];
        if (!data[cur].Entries.Any(x => x.Type == t && x.Value == v))
            data[cur].Entries.Add(new Entry { Module = "graine", Key = t.ToString(), Value = v, Type = t, Source = "graine" });
        SeedBox.Clear();
        store.Save(data);
        Render();
        if (t == NodeType.DiscordId && pivotRows.Count > 0) { PivotList.SelectedIndex = 0; RunPivot(); }
    }

    void OnPivot(object s, MouseButtonEventArgs e) => RunPivot();

    void RunPivot() {
        if (PivotList.SelectedItem is not PivotRow row) return;
        var p = row.P;
        if (p.Kind == "lien") { Open(p.Url); return; }
        if (!hasToken) { Say("token absent : colle-le en haut a droite puis Verifier"); return; }
        if (!mods.TryGetValue(p.Module, out var collector)) return;
        var entry = data[cur].Entries[p.EntryIndex];
        Say(p.Module + " demarre");
        _ = Task.Run(async () => {
            try {
                var found = await collector.CollectAsync(entry, api, CancellationToken.None);
                Dispatcher.Invoke(() => {
                    int added = 0;
                    foreach (var n in found)
                        if (!data[cur].Entries.Any(x => x.Module == n.Module && x.Key == n.Key && x.Value == n.Value)) { data[cur].Entries.Add(n); added++; }
                    store.Save(data);
                    Render();
                    Say(p.Module + " termine, " + added + " entite(s)");
                });
            } catch (Exception ex) { Dispatcher.Invoke(() => Say(p.Module + " erreur : " + ex.Message)); }
        });
    }

    static void Open(string u) {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(u) { UseShellExecute = true }); } catch { }
    }

    void OnExport(object s, RoutedEventArgs e) {
        if (cur < 0) return;
        var dlg = new SaveFileDialog { Filter = "Markdown|*.md", FileName = "dossier.md" };
        if (dlg.ShowDialog() != true) return;
        var sb = new System.Text.StringBuilder("# " + data[cur].Name + "\n\n");
        foreach (var en in data[cur].Entries) sb.AppendLine("- [" + en.Module + "] " + en.Key + " : " + en.Value);
        File.WriteAllText(dlg.FileName, sb.ToString());
        Say("export ok");
    }
}





