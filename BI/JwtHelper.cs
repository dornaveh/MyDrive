using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;

namespace MyDrive;

public class JwtHelper
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly string issuer;
    private readonly string audience;

    public JwtHelper(IConfiguration config)
    {
        var jwtConfig = config.GetJwtConfig();
        issuer = jwtConfig.issuer;
        audience = jwtConfig.audience;
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(jwtConfig.metaDataAddress, new OpenIdConnectConfigurationRetriever());
    }

    public async Task<(string id, string email)> getId(HttpRequest req)
    {
        if (!req.Headers.ContainsKey("Authorization"))
        {
            throw new JwtException("no auth");
        }
        var fullAuth = req.Headers["Authorization"][0];
        if (!fullAuth.ToLower().StartsWith("bearer "))
        {
            throw new JwtException("no bearer");
        }
        var token = fullAuth.Substring("bearer ".Length);
        var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
        var validationParameter = new TokenValidationParameters()
        {
            RequireSignedTokens = true,
            ValidAudience = audience,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKeys = config.SigningKeys,
        };

        for (int tries = 0; tries <= 1; tries++)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, validationParameter, out var jwt);
                var id = ((JwtSecurityToken)jwt).Payload["oid"] as string;
                var emails = ((JwtSecurityToken)jwt).Payload["emails"];
                var email = JsonConvert.DeserializeObject<string[]>(emails.ToString()).First();
                return (id, email);
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                // This exception is thrown if the signature key of the JWT could not be found.
                // This could be the case when the issuer changed its signing keys, so we trigger a 
                // refresh and retry validation.
                _configurationManager.RequestRefresh();
            }
            catch (SecurityTokenException)
            {
                throw new JwtException(token);
            }
        }
        throw new JwtException("two reties");
    }
}

public class JwtException : Exception
{
    public JwtException(Exception ste) : base("JWT", ste) { }
    public JwtException(string message) : base(message) { }
}


