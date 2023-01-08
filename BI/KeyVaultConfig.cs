namespace MyDrive;

public static class KeyVaultConfig
{
    public static string GetBlobStorageConnectionString(this IConfiguration config)
    {
        return config.GetValue("storage");
    }

    public static (string issuer, string audience, string metaDataAddress) GetJwtConfig(this IConfiguration config)
    {
        return (config.GetValue("jwt-issuer"), config.GetValue("jwt-audience"), config.GetValue("jwt-metadata-address"));
    }

    public static string GetGoogleClientId(this IConfiguration config)
    {
        return config.GetValue("goole-client-id");
    }

    public static string GetGoogleClientSecret(this IConfiguration config)
    {
        return config.GetValue("goole-client-secret");
    }

    public static string GetAppInsightConnectionString(this IConfiguration config)
    {
        return config.GetValue("app-insight");
    }

    public static MsalConfig GetMsalConfig(this IConfiguration config)
    {
        MsalConfig ans = new();
        var jws = config.GetJwtConfig();
        ans.ClientId = jws.audience;
        var uri = new Uri(jws.metaDataAddress);
        ans.KnownAuthorities = new List<string> { uri.Host };
        ans.Authority = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.IndexOf("/v2.0/.well-known/openid-configuration"));
        ans.Scope = config.GetValue("scope");
        return ans;
    }

    private static string GetValue(this IConfiguration config, string key)
    {
        return config.GetChildren().First(k => k.Key == key).Value;
    }
}
