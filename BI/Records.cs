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