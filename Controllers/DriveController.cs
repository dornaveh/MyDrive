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
    public async Task<DriveAccessMessage> PostLogin()
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

    [HttpGet("generatecache")]
    public async Task<bool> GenerateCache()
    {
        var token = await _jwtHelper.getId(Request);
        return await _backupManager.GenerateCache(token.id);
    }

    [HttpGet("checkstatus")]
    public async Task<CheckStatusResponse> CheckStatus()
    {
        var token = await _jwtHelper.getId(Request);
        return await _backupManager.Status(token.id);
    }

    [HttpGet("checkcachestatus")]
    public async Task<CacheStatusResponse> CheckCacheStatus(string cacheId)
    {
        var token = await _jwtHelper.getId(Request);
        return await _backupManager.GetCacheStatus(cacheId, token.id);
    }

    [HttpGet("backupfile")]
    public async Task BackupFile(string id)
    {
        var token = await _jwtHelper.getId(Request);
        await _backupManager.Backup(id, token.id);
    }

    [HttpGet("backupcache")]
    public async Task<bool> BackupCache(string cacheId)
    {
        var token = await _jwtHelper.getId(Request);
        return _backupManager.BackupCache(cacheId, token.id);
    }

    [HttpGet("getfiles")]
    public async Task<List<FileItem>> GetFiles(string folderId, string cacheId)
    {
        var token = await _jwtHelper.getId(Request);
        List<FileItem> ans;
        if ("realtime".Equals(cacheId))
        {
            var access = await _googleProvider.GetAccess(token.id);
            ans = await access.GetFiles(folderId);
        }
        else
        {
            ans = await _backupManager.GetFiles(token.id, cacheId, folderId);
        }
        await _backupManager.MarkActions(token.id, ans);
        return ans;
    }

    [HttpGet("getdownloadurl")]
    public async Task<SasUrl> GetDownloadUrl(string fileId)
    {
        var token = await _jwtHelper.getId(Request);
        return new SasUrl
        {
            Url = await _backupManager.GetSasUrl(token.id, fileId)
        };
    }

    [HttpGet("msal")]
    public MsalConfig GetMsalConfig() => _msalConfig;
}