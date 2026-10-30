using System;
using System.Windows.Forms;

namespace Argos.App;

static class Program {
    [STAThread]
    static void Main() {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (s, e) => MessageBox.Show(e.Exception.ToString(), "Argos");
        Application.Run(new MainForm());
    }
}