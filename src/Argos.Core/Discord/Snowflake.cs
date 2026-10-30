namespace Argos.Core.Discord;

public static class Snowflake {
    const long Epoch = 1420070400000L;

    public static bool TryParse(string raw, out long id) {
        id = 0;
        var d = new string(raw.Where(char.IsDigit).ToArray());
        if (d.Length < 15 || d.Length > 20) return false;
        return long.TryParse(d, out id);
    }

    public static DateTime Created(long id) =>
        DateTimeOffset.FromUnixTimeMilliseconds((id >> 22) + Epoch).ToLocalTime().DateTime;

    public static List<string> Flags(long id) {
        var created = (id >> 22) + Epoch;
        var days = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - created) / 86_400_000.0;
        var f = new List<string>();
        if (created < 1451606400000L) f.Add("compte anterieur a 2016");
        else if (created < 1577836800000L) f.Add("compte 2016-2019, identite probable");
        if (days < 30) f.Add("moins de 30 jours, compte frais");
        return f;
    }
}