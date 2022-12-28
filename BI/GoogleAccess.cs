using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.IdentityModel.Tokens;

namespace MyDrive;

public class GoogleAccess
{
    private readonly string _accessToken;
    public GoogleAccess(string accessToken)
    {
        this._accessToken = accessToken;
    }

    private DriveService GetService()
    {
        return new DriveService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(_accessToken)
        });
    }

    public async Task<List<FileItem>> GetFiles(string folder)
    {
        var service = GetService();
        var req = service.Files.List();
        req.Q = string.Format("'{0}' in parents", folder);
        req.Fields = "files/name, files/id, files/mimeType, nextPageToken";
        req.PageSize = 500;
        var files = new List<GoogleFile>();
        var fileList = await req.ExecuteAsync();
        while (fileList != null)
        {
            foreach (var file in fileList.Files)
            {
                files.Add(file);
            }
            if (!fileList.NextPageToken.IsNullOrEmpty())
            {
                req.PageToken = fileList.NextPageToken;
                fileList = await req.ExecuteAsync();
            }
            else
            {
                fileList = null;
            }
        }
        return files.Select(x=>new FileItem
        {
            Id = x.Id,
            Name = x.Name,
            Type = x.MimeType
        }).ToList();
    }
}
