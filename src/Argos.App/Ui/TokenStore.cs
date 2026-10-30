using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Argos.App.Ui;

public static class TokenStore {
    static string P => Path.Combine(AppContext.BaseDirectory, "token.bin");
    public static void Save(string t) {
        try { File.WriteAllBytes(P, ProtectedData.Protect(Encoding.UTF8.GetBytes(t), null, DataProtectionScope.CurrentUser)); } catch { }
    }
    public static string Load() {
        try {
            if (!File.Exists(P)) return "";
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(P), null, DataProtectionScope.CurrentUser));
        } catch { return ""; }
    }
}
