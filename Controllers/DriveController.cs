using Microsoft.AspNetCore.Mvc;

namespace MyDrive.Controllers;

[ApiController]
[Route("[controller]")]
public class DriveController : ControllerBase
{
    private readonly ILogger<DriveController> _logger;
    private readonly JwtHelper _jwtHelper;
    private readonly GoogleProvider _googleProvider;
    private readonly BackupManager _backupManager;
    private readonly MsalConfig _msalConfig;

    public DriveController(
        ILogger<DriveController> logger,
        JwtHelper jwtHelper,
        GoogleProvider googleProvider,
        BackupManager backupManager,
        MsalConfig msalConfig)
    {
        _logger = logger;
        _jwtHelper = jwtHelper;
        _googleProvider = googleProvider;
        _backupManager = backupManager;
        _msalConfig = msalConfig;
    }

    [HttpPost("setgoogledriveaccess")]
    public async Task FinishGoogleDriveAccess(DriveAccessMessage request)
    {
        var token = await _jwtHelper.getId(Request);
        await _googleProvider.SolidifyAccess(request.Code, request.Redirect, token.id);
    }

    [HttpGet("postlogin")]
    public async Task<DriveAccessMessage> GetGoogleDriveAccessUrl()
    {
        var token = await _jwtHelper.getId(Request);
        var access = await _googleProvider.GetAccess(token.id);
        if (access != null)
        {
            return new DriveAccessMessage { HasAccess = true };
        }
        var url = _googleProvider.CreateRequestAccessUrl(token.email);
        return new DriveAccessMessage
        {
            HasAccess = false,
            Redirect = url
        };
    }

    [HttpGet("listfiles")]
    public async Task ListFiles()
    {
        var token = await _jwtHelper.getId(Request);
        _backupManager.Run(await _googleProvider.GetAccess(token.id));
    }

    [HttpGet("getfiles")]
    public async Task<List<FileItem>> GetFiles(string folderId)
    {
        var token = await _jwtHelper.getId(Request);
        var access = await _googleProvider.GetAccess(token.id);
        var ans = await access.GetFiles(folderId);
        return ans;
    }

    [HttpGet("msal")]
    public MsalConfig GetMsalConfig() => _msalConfig;
}