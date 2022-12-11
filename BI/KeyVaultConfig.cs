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

    private static string GetValue(this IConfiguration config, string key)
    {
        return config.GetChildren().First(k => k.Key == key).Value;
    }
}
