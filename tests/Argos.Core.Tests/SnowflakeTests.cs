using Argos.Core.Discord;
using Xunit;

public class SnowflakeTests {
    [Fact] public void Rejette_court() => Assert.False(Snowflake.TryParse("12", out _));
    [Fact] public void Accepte_valide() {
        Assert.True(Snowflake.TryParse("184405311681986560", out var id));
        Assert.True(Snowflake.Created(id).Year >= 2015);
    }
    [Fact] public void Detecte_compte_recent() {
        Snowflake.TryParse(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), out var id);
        Assert.True(Snowflake.Flags(id).Count > 0);
    }
}