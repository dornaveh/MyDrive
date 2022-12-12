using Microsoft.AspNetCore.Mvc;

namespace MyDrive.Controllers;

[ApiController]
[Route("[controller]")]
public class DriveController : ControllerBase
{
    private readonly ILogger<DriveController> _logger;
    private readonly JwtHelper _jwtHelper;
    private readonly GoogleProvider _googleProvider;

    public DriveController(ILogger<DriveController> logger, JwtHelper jwtHelper, GoogleProvider googleProvider)
    {
        _logger = logger;
        _jwtHelper = jwtHelper;
        _googleProvider = googleProvider;
    }

    [HttpPost("setgoogledriveaccess")]
    public async Task FinishGoogleDriveAccess(DriveAccessMessage request)
    {
        var token = await _jwtHelper.getId(Request);
        await _googleProvider.SolidifyAccess(request.Code, request.Redirect, token.id);
    }

    [HttpGet("getgoogledriveaccessurl")]
    public async Task<DriveAccessMessage> GetGoogleDriveAccessUrl()
    {
        var token = await _jwtHelper.getId(Request);
        var url = _googleProvider.CreateRequestAccessUrl(token.email);
        return new DriveAccessMessage { Redirect = url };
    }

    [HttpGet("hasaccess")]
    public async Task<bool> HasAccess()
    {
        var token = await _jwtHelper.getId(Request);
        var access = await _googleProvider.GetAccessToken(token.id);
        return access != null;
    }

    public class DriveAccessMessage
    {
        public string Code { get; set; } = "";
        public string Redirect { get; set; } = "";
    }
}

