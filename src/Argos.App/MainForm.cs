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
    static readonly Color BG = Color.FromArgb(10, 11, 14), PANEL = Color.FromArgb(14, 16, 20),
        CARD = Color.FromArgb(20, 23, 29), FG = Color.FromArgb(232, 235, 240), MUT = Color.FromArgb(122, 132, 145),
        DIM = Color.FromArgb(88, 97, 110), BD = Color.FromArgb(32, 37, 45), ACC = Color.FromArgb(88, 101, 242),
        OK = Color.FromArgb(34, 197, 94), WARN = Color.FromArgb(245, 158, 11);

    readonly DossierStore store = new(Path.Combine(AppContext.BaseDirectory, "argos.json"));
    readonly DiscordClient api = new();
    readonly Dictionary<string, ICollector> mods = new();
    List<Dossier> data;
    int cur = -1;
    bool busy;

    ListBox list; FlowLayoutPanel feed; ListBox piv; TextBox log; TextBox seed; ComboBox type; Label status; Label empty; Label tokState;
    static readonly NodeType[] Map = { NodeType.DiscordId, NodeType.Username, NodeType.Email, NodeType.Phone, NodeType.RealName, NodeType.Address };

    public MainForm() {
        foreach (var c in new ICollector[] { new DiscordCollector(), new AvatarCollector(), new WebCollector() }) mods[c.Name] = c;
        data = store.Load();
        Text = "Argos"; Width = 1320; Height = 840; MinimumSize = new Size(900, 600);
        BackColor = BG; ForeColor = FG; Font = new Font("Segoe UI", 9.5f);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Build();
        Render();
    }

    Button B(string t, int x, Color bg, Color fg, int w = 92) {
        var b = new Button { Text = t, Left = x, Top = 12, Height = 32, Width = w, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = fg, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = bg == ACC ? 0 : 1;
        b.FlatAppearance.BorderColor = BD;
        b.FlatAppearance.MouseOverBackColor = bg == ACC ? Color.FromArgb(104, 116, 250) : Color.FromArgb(28, 32, 40);
        return b;
    }

    void Build() {
        var head = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = PANEL, Padding = new Padding(16, 0, 0, 0) };
        head.Paint += (s, e) => e.Graphics.DrawLine(new Pen(BD), 0, 57, head.Width, 57);
        var title = new Label { Text = "ARGOS", Left = 18, Top = 19, AutoSize = true, ForeColor = ACC, Font = new Font("Segoe UI", 13f, FontStyle.Bold) };
        type = new ComboBox { Left = 100, Top = 18, Width = 132, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CARD, ForeColor = FG, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f) };
        type.Items.AddRange(new object[] { "ID Discord", "Pseudo", "Email", "Telephone", "Nom", "Adresse" });
        type.SelectedIndex = 0;
        seed = new TextBox { Left = 240, Top = 18, Width = 400, BackColor = CARD, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10f) };
        seed.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddSeed(); } };
        var go = B("Lancer", 648, ACC, Color.White);
        go.Click += (s, e) => AddSeed();
        var ex = B("Export", 746, CARD, FG, 78);
        ex.Click += (s, e) => Export();
        var tk = B("Token", 830, CARD, FG, 74);
        tk.Click += (s, e) => SetToken();
        tokState = new Label { Left = 912, Top = 22, AutoSize = true, ForeColor = DIM, Font = new Font("Segoe UI", 8.5f) };
        head.Controls.AddRange(new Control[] { title, type, seed, go, ex, tk, tokState });

        var left = new Panel { Dock = DockStyle.Left, Width = 216, BackColor = PANEL, Padding = new Padding(10, 10, 10, 10) };
        var nd = B("Nouveau dossier", 0, CARD, FG, 196);
        nd.Dock = DockStyle.Top; nd.Height = 30; nd.Font = new Font("Segoe UI", 9f);
        nd.Click += (s, e) => NewCase();
        var spacer = new Panel { Dock = DockStyle.Top, Height = 10 };
        list = new ListBox { Dock = DockStyle.Fill, BackColor = PANEL, ForeColor = FG, BorderStyle = BorderStyle.None, ItemHeight = 30, Font = new Font("Segoe UI", 9.5f) };
        list.DrawMode = DrawMode.OwnerDrawFixed;
        list.DrawItem += (s, e) => {
            if (e.Index < 0) return;
            var sel = e.Index == list.SelectedIndex;
            using var b = new SolidBrush(sel ? ACC : PANEL);
            e.Graphics.FillRectangle(b, e.Bounds);
            using var f = new SolidBrush(sel ? Color.White : FG);
            e.Graphics.DrawString(list.Items[e.Index].ToString(), new Font("Segoe UI", 9.5f), f, e.Bounds.X + 8, e.Bounds.Y + 7);
        };
        list.SelectedIndexChanged += (s, e) => { if (list.SelectedIndex >= 0) { cur = list.SelectedIndex; Render(); } };
        left.Controls.Add(list); left.Controls.Add(spacer); left.Controls.Add(nd);

        var right = new Panel { Dock = DockStyle.Right, Width = 310, BackColor = PANEL, Padding = new Padding(12, 10, 12, 10) };
        var pl = new Label { Text = "PIVOTS", Dock = DockStyle.Top, ForeColor = DIM, Height = 22, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
        piv = new ListBox { Dock = DockStyle.Fill, BackColor = PANEL, ForeColor = FG, BorderStyle = BorderStyle.None, ItemHeight = 44, DrawMode = DrawMode.OwnerDrawFixed };
        piv.DrawItem += DrawPivot;
        piv.DoubleClick += (s, e) => RunPivot();
        log = new TextBox { Dock = DockStyle.Bottom, Height = 168, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = BG, ForeColor = MUT, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8f) };
        right.Controls.Add(piv); right.Controls.Add(log); right.Controls.Add(pl);

        feed = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Padding = new Padding(14), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        empty = new Label { Text = "Aucune entite.\nSaisis un identifiant puis Lancer.", ForeColor = DIM, AutoSize = true, Font = new Font("Segoe UI", 10f) };

        status = new Label { Dock = DockStyle.Bottom, Height = 26, ForeColor = MUT, BackColor = PANEL, Padding = new Padding(14, 6, 0, 0), Text = "pret" };

        Controls.Add(feed); Controls.Add(right); Controls.Add(left); Controls.Add(head); Controls.Add(status);
    }

    void Say(string m) {
        log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + m + Environment.NewLine);
        status.Text = m;
        if (m.Contains("erreur")) status.ForeColor = WARN; else status.ForeColor = MUT;
    }

    void NewCase() {
        var n = PromptDialog.Ask(this, "Nom du dossier");
        if (string.IsNullOrWhiteSpace(n)) return;
        data.Add(new Dossier { Name = n });
        cur = data.Count - 1;
        store.Save(data);
        Render();
    }

    async void SetToken() {
        var t = PromptDialog.Ask(this, "Token utilisateur Discord");
        if (string.IsNullOrWhiteSpace(t)) return;
        api.SetToken(t);
        tokState.Text = "verification...";
        tokState.ForeColor = DIM;
        try {
            var who = await api.WhoAmI();
            tokState.Text = "connecte : " + who;
            tokState.ForeColor = OK;
            Say("token valide (" + who + ")");
        } catch (Exception ex) {
            tokState.Text = "token invalide";
            tokState.ForeColor = Color.FromArgb(239, 68, 68);
            Say("token erreur : " + ex.Message);
        }
    }

    void Render() {
        if (busy) return;
        busy = true;
        try {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var d in data) list.Items.Add(d.Name + "   " + d.Entries.Count);
            if (cur >= 0 && cur < list.Items.Count) list.SelectedIndex = cur;
            list.EndUpdate();

            feed.SuspendLayout();
            feed.Controls.Clear();
            piv.Items.Clear();
            if (cur < 0 || cur >= data.Count) { feed.ResumeLayout(); return; }

            if (data[cur].Entries.Count == 0) feed.Controls.Add(empty);
            foreach (var e in data[cur].Entries) Card(e);
            foreach (var p in PivotEngine.Suggest(data[cur])) piv.Items.Add(p);
            feed.ResumeLayout();
            status.Text = data[cur].Entries.Count + " entites";
        } finally { busy = false; }
    }

    void Card(Entry e) {
        var p = new Panel { Width = Math.Max(260, feed.ClientSize.Width - 42), BackColor = CARD, Padding = new Padding(14, 10, 14, 10), Margin = new Padding(0, 0, 0, 8) };
        var accent = e.Type switch {
            NodeType.DiscordId => ACC, NodeType.Email => WARN, NodeType.Phone => OK,
            NodeType.RealName => Color.FromArgb(239, 68, 68), NodeType.Username => Color.FromArgb(139, 92, 246), _ => DIM
        };
        p.Paint += (s, ev) => {
            using var pen = new SolidBrush(accent);
            ev.Graphics.FillRectangle(pen, 0, 0, 3, p.Height);
        };
        var h = new Label { Text = (e.Module + "  /  " + e.Key).ToLower(), ForeColor = DIM, Dock = DockStyle.Top, Height = 16, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold) };
        var f = new Font("Consolas", 9f);
        var v = new Label {
            Text = e.Value, ForeColor = FG, Dock = DockStyle.Top, AutoSize = false, Font = f,
            Height = Math.Max(20, TextRenderer.MeasureText(e.Value, f, new Size(p.Width - 34, 0), TextFormatFlags.WordBreak).Height)
        };
        var m = new Label { Text = e.Source, ForeColor = DIM, Dock = DockStyle.Top, Height = 15, Font = new Font("Segoe UI", 7.5f) };
        if (e.Value.StartsWith("http")) { v.Cursor = Cursors.Hand; v.ForeColor = Color.FromArgb(139, 155, 255); v.Click += (s, ev) => Open(e.Value); }
        p.Controls.Add(v); p.Controls.Add(m); p.Controls.Add(h);
        feed.Controls.Add(p);
    }

    static void Open(string u) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(u) { UseShellExecute = true }); } catch { } }

    void DrawPivot(object s, DrawItemEventArgs e) {
        if (e.Index < 0) return;
        var p = piv.Items[e.Index] as Pivot;
        using var bg = new SolidBrush(e.Index == piv.SelectedIndex ? Color.FromArgb(26, 30, 38) : PANEL);
        e.Graphics.FillRectangle(bg, e.Bounds);
        var bar = p.Yield > .8 ? OK : p.Yield > .5 ? WARN : DIM;
        using var barB = new SolidBrush(bar);
        e.Graphics.FillRectangle(barB, e.Bounds.X, e.Bounds.Y + 6, 2, e.Bounds.Height - 12);
        e.Graphics.DrawString(p.Label, new Font("Segoe UI", 9.5f, FontStyle.Bold), new SolidBrush(FG), e.Bounds.X + 10, e.Bounds.Y + 6);
        e.Graphics.DrawString((int)(p.Yield * 100) + "%   " + p.Why, new Font("Segoe UI", 7.5f), new SolidBrush(DIM), e.Bounds.X + 10, e.Bounds.Y + 25);
    }

    void RunPivot() {
        if (piv.SelectedItem is not Pivot p) return;
        if (p.Kind == "lien") { Open(p.Url); return; }
        if (!mods.TryGetValue(p.Module, out var c)) return;
        var e = data[cur].Entries[p.EntryIndex];
        Say(p.Module + " demarre");
        _ = Task.Run(async () => {
            try {
                var r = await c.CollectAsync(e, api, CancellationToken.None);
                Invoke(() => {
                    foreach (var n in r)
                        if (!data[cur].Entries.Any(x => x.Module == n.Module && x.Key == n.Key && x.Value == n.Value))
                            data[cur].Entries.Add(n);
                    store.Save(data);
                    Render();
                });
            } catch (Exception x) { Invoke(() => Say(p.Module + " erreur : " + x.Message)); }
        });
    }

    void AddSeed() {
        var v = seed.Text.Trim();
        if (v.Length == 0) return;
        if (cur < 0) { data.Add(new Dossier { Name = "dossier " + DateTime.Now.ToString("dd/MM") }); cur = data.Count - 1; }
        var t = Map[type.SelectedIndex];
        if (!data[cur].Entries.Any(x => x.Type == t && x.Value == v))
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