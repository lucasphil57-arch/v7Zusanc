namespace Argos.Core.Models;

public enum NodeType { DiscordId, Username, Email, Phone, RealName, Address, Avatar, Url }

public class Entry {
    public string Module { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public NodeType Type { get; set; } = NodeType.Url;
    public string Source { get; set; } = "";
}