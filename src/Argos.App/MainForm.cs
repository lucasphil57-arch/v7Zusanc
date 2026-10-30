using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Argos.Core.Discord;
using Argos.Core.Models;
using Argos.Core.Modules;
using Argos.Core.Pivot;
using Argos.Core.Storage;
using Argos.App.Ui;

namespace Argos.App;

public class MainForm : Form {
    static readonly Color BG = Color.FromArgb(8, 9, 12), CARD = Color.FromArgb(17, 21, 27), FG = Color.FromArgb(231, 234, 239),
        MUT = Color.FromArgb(120, 131, 143), BD = Color.FromArgb(29, 35, 43), ACC = Color.FromArgb(88, 101, 242);

    readonly DossierStore store = new(Path.Combine(AppContext.BaseDirectory, "argos.json"));
    readonly DiscordClient api = new();
    readonly Dictionary<string, ICollector> mods = new();
    List<Dossier> data;
    int cur = -1;
    bool busy;

    ListBox list; FlowLayoutPanel feed; ListBox piv; TextBox log; TextBox seed; ComboBox type; Label status;
    static readonly NodeType[] Map = { NodeType.DiscordId, NodeType.Username, NodeType.Email, NodeType.Phone, NodeType.RealName, NodeType.Address };

    public MainForm() {
        foreach (var c in new ICollector[] { new DiscordCollector(), new AvatarCollector(), new WebCollector() }) mods[c.Name] = c;
        data = store.Load();
        Text = "Argos"; Width = 1280; Height = 800; BackColor = BG; ForeColor = FG;
        Font = new Font("Segoe UI", 9f); StartPosition = FormStartPosition.CenterScreen;
        Build();
        Render();
    }

    Button B(string t, int x, Color c) {
        var b = new Button { Text = t, Left = x, Top = 8, Height = 28, Width = 78, FlatStyle = FlatStyle.Flat, BackColor = c, ForeColor = FG };
        b.FlatAppearance.BorderColor = BD;
        return b;
    }

    void Build() {
        var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BG };
        type = new ComboBox { Left = 8, Top = 10, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CARD, ForeColor = FG, FlatStyle = FlatStyle.Flat };
        type.Items.AddRange(new object[] { "ID Discord", "Pseudo", "Email", "Telephone", "Nom", "Adresse" });
        type.SelectedIndex = 0;
        seed = new TextBox { Left = 144, Top = 10, Width = 400, BackColor = CARD, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
        seed.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddSeed(); } };
        var go = B("Lancer", 552, ACC); go.Click += (s, e) => AddSeed();
        var ex = B("Export", 636, CARD); ex.Click += (s, e) => Export();
        var tk = B("Token", 716, CARD); tk.Click += (s, e) => SetToken();
        top.Controls.AddRange(new Control[] { type, seed, go, ex, tk });

        var left = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = BG, Padding = new Padding(6) };
        var nd = B("Nouveau dossier", 0, CARD); nd.Dock = DockStyle.Top; nd.Width = 180;
        nd.Click += (s, e) => NewCase();
        list = new ListBox { Dock = DockStyle.Fill, BackColor = CARD, ForeColor = FG, BorderStyle = BorderStyle.None, ItemHeight = 26 };
        list.SelectedIndexChanged += (s, e) => { if (list.SelectedIndex >= 0) { cur = list.SelectedIndex; Render(); } };
        left.Controls.Add(list); left.Controls.Add(nd);

        var right = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = BG, Padding = new Padding(6) };
        var pl = new Label { Text = "pivots", Dock = DockStyle.Top, ForeColor = MUT, Height = 18 };
        piv = new ListBox { Dock = DockStyle.Fill, BackColor = CARD, ForeColor = FG, BorderStyle = BorderStyle.None, ItemHeight = 34, DrawMode = DrawMode.OwnerDrawFixed };
        piv.DrawItem += DrawPivot;
        piv.DoubleClick += (s, e) => RunPivot();
        log = new TextBox { Dock = DockStyle.Bottom, Height = 160, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = CARD, ForeColor = MUT, BorderStyle = BorderStyle.None };
        right.Controls.Add(piv); right.Controls.Add(log); right.Controls.Add(pl);

        feed = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Padding = new Padding(10), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        status = new Label { Dock = DockStyle.Bottom, Height = 22, ForeColor = MUT, Text = "  pret" };

        Controls.Add(feed); Controls.Add(right); Controls.Add(left); Controls.Add(top); Controls.Add(status);
    }

    void Say(string m) { log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + m + Environment.NewLine); status.Text = "  " + m; }

    void NewCase() {
        var n = PromptDialog.Ask(this, "Nom du dossier");
        if (string.IsNullOrWhiteSpace(n)) return;
        data.Add(new Dossier { Name = n });
        cur = data.Count - 1;
        store.Save(data);
        Render();
    }

    void SetToken() {
        var t = PromptDialog.Ask(this, "Token utilisateur Discord");
        if (string.IsNullOrWhiteSpace(t)) return;
        api.SetToken(t.Trim());
        Say("token enregistre");
    }

    void Render() {
        if (busy) return;
        busy = true;
        try {
            list.Items.Clear();
            foreach (var d in data) list.Items.Add(d.Name + "  (" + d.Entries.Count + ")");
            if (cur >= 0 && cur < list.Items.Count) list.SelectedIndex = cur;
            feed.Controls.Clear();
            piv.Items.Clear();
            if (cur < 0 || cur >= data.Count) return;
            foreach (var e in data[cur].Entries) Card(e);
            foreach (var p in PivotEngine.Suggest(data[cur])) piv.Items.Add(p);
            status.Text = "  " + data[cur].Entries.Count + " entites";
        } finally { busy = false; }
    }

    void Card(Entry e) {
        var p = new Panel { Width = Math.Max(240, feed.ClientSize.Width - 40), BackColor = CARD, Padding = new Padding(10), Margin = new Padding(0, 0, 0, 6) };
        var h = new Label { Text = (e.Module + " / " + e.Key).ToLower(), ForeColor = MUT, Dock = DockStyle.Top, Height = 16 };
        var f = new Font("Consolas", 8.5f);
        var v = new Label { Text = e.Value, ForeColor = FG, Dock = DockStyle.Top, AutoSize = false, Font = f,
            Height = Math.Max(18, TextRenderer.MeasureText(e.Value, f, new Size(p.Width - 30, 0), TextFormatFlags.WordBreak).Height) };
        var m = new Label { Text = e.Source, ForeColor = MUT, Dock = DockStyle.Top, Height = 14, Font = new Font("Segoe UI", 7.5f) };
        if (e.Value.StartsWith("http")) { v.Cursor = Cursors.Hand; v.Click += (s, ev) => Open(e.Value); }
        p.Controls.Add(v); p.Controls.Add(m); p.Controls.Add(h);
        feed.Controls.Add(p);
    }

    static void Open(string u) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(u) { UseShellExecute = true }); } catch { } }

    void DrawPivot(object s, DrawItemEventArgs e) {
        if (e.Index < 0) return;
        var p = piv.Items[e.Index] as Pivot;
        using var b = new SolidBrush(CARD);
        e.Graphics.FillRectangle(b, e.Bounds);
        e.Graphics.DrawString(p.Label, new Font("Segoe UI", 9f, FontStyle.Bold), new SolidBrush(FG), e.Bounds.X + 6, e.Bounds.Y + 3);
        e.Graphics.DrawString((int)(p.Yield * 100) + "%  " + p.Why, new Font("Segoe UI", 7.5f), new SolidBrush(MUT), e.Bounds.X + 6, e.Bounds.Y + 18);
    }

    void RunPivot() {
        if (piv.SelectedItem is not Pivot p) return;
        if (p.Kind == "lien") { Open(p.Url); return; }
        if (!mods.TryGetValue(p.Module, out var c)) return;
        var e = data[cur].Entries[p.Index];
        Say(p.Module + " demarre");
        _ = Task.Run(async () => {
            try {
                var r = await c.CollectAsync(e, api, CancellationToken.None);
                Invoke(() => { data[cur].Entries.AddRange(r); store.Save(data); Render(); });
            } catch (Exception x) { Invoke(() => Say(p.Module + " erreur : " + x.Message)); }
        });
    }

    void AddSeed() {
        var v = seed.Text.Trim();
        if (v.Length == 0) return;
        if (cur < 0) { data.Add(new Dossier { Name = "dossier " + DateTime.Now.ToString("dd/MM") }); cur = data.Count - 1; }
        var t = Map[type.SelectedIndex];
        data[cur].Entries.Add(new Entry { Module = "graine", Key = t.ToString(), Value = v, Type = t, Source = "graine" });
        seed.Text = "";
        store.Save(data);
        Render();
        if (t == NodeType.DiscordId && piv.Items.Count > 0) { piv.SelectedIndex = 0; RunPivot(); }
    }

    void Export() {
        if (cur < 0) return;
        using var s = new SaveFileDialog { Filter = "Markdown|*.md", FileName = "dossier.md" };
        if (s.ShowDialog() != DialogResult.OK) return;
        var sb = new System.Text.StringBuilder("# " + data[cur].Name + "\n\n");
        foreach (var e in data[cur].Entries) sb.AppendLine("- [" + e.Module + "] " + e.Key + " : " + e.Value);
        File.WriteAllText(s.FileName, sb.ToString());
        Say("export ok");
    }
}