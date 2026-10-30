using System;
using System.IO;
using System.Windows;

namespace Argos.App;

public partial class App : Application {
    public App() {
        DispatcherUnhandledException += (s, e) => { Crash(e.Exception); e.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Crash(e.ExceptionObject as Exception);
    }
    static void Crash(Exception ex) {
        try {
            var p = Path.Combine(Path.GetTempPath(), "argos-crash.log");
            File.AppendAllText(p, DateTime.Now + "\n" + ex + "\n\n");
            MessageBox.Show(ex.Message + "\n\nJournal : " + p, "Argos");
        } catch { }
    }
}
