namespace Argos.Core.Models;

public class Dossier {
    public string Name { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.Now;
    public List<Entry> Entries { get; set; } = new();
}