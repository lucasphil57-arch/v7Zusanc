using System.Text.Json;
using Argos.Core.Models;

namespace Argos.Core.Storage;

public class DossierStore {
    readonly string path;
    public DossierStore(string path) => this.path = path;

    public List<Dossier> Load() {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<List<Dossier>>(File.ReadAllText(path)) ?? new() : new(); }
        catch { return new(); }
    }
    public void Save(List<Dossier> d) { try { File.WriteAllText(path, JsonSerializer.Serialize(d)); } catch { } }
}