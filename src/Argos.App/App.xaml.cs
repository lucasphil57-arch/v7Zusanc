using System.Windows;
using System.Windows.Threading;

namespace Argos.App;

public partial class App : Application {
    public App() {
        DispatcherUnhandledException += (s, e) => {
            MessageBox.Show(e.Exception.ToString(), "Argos");
            e.Handled = true;
        };
    }
}
