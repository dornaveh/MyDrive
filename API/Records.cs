namespace MyDrive;

public record MsalConfig
{
    public string ClientId { get; set; }
    public string Authority { get; set; }
    public List<string> KnownAuthorities { get; set; }
    public string Scope { get; set; }
}

public record DriveAccessMessage
{
    public string Code { get; set; } = "";
    public string Redirect { get; set; } = "";
    public bool HasAccess { get; set; } = false;
}

public record CheckStatusResponse
{
    public int CacheGenerationStatus { get; set; }
    public List<long> CacheTimeStamps { get; set; }
}

public static class Epoch
{
    private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToEpoch(this DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - epoch).TotalMilliseconds;
    }

    public static DateTime FromEpoch(this long utc)
    {
        return epoch + TimeSpan.FromMilliseconds(utc);
    }
}