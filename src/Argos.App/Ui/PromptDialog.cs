using System.Drawing;
using System.Windows.Forms;

namespace Argos.App.Ui;

public static class PromptDialog {
    public static string Ask(IWin32Window owner, string label) {
        using var f = new Form {
            Width = 520, Height = 150, Text = label, BackColor = Color.FromArgb(8, 9, 12),
            ForeColor = Color.FromArgb(231, 234, 239), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
        };
        var t = new TextBox { Left = 12, Top = 14, Width = 480, BackColor = Color.FromArgb(17, 21, 27), ForeColor = Color.FromArgb(231, 234, 239) };
        var ok = new Button { Text = "OK", Left = 12, Top = 50, Width = 90, Height = 28, DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(88, 101, 242), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        f.Controls.Add(t); f.Controls.Add(ok); f.AcceptButton = ok;
        return f.ShowDialog(owner) == DialogResult.OK ? t.Text : null;
    }
}