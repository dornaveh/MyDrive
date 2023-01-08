using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.IdentityModel.Tokens;
using System.Collections;

namespace MyDrive;

public class GoogleAccess : IAsyncEnumerable<FileItem>
{

    private Func<Task<string?>> GetAccessToken { get; }
    private (string? token, DateTime expiration) _accessToken { get; set; } = ("", DateTime.MinValue);

    public GoogleAccess(Func<Task<string?>> getAccessToken)
    {
        GetAccessToken = getAccessToken;
    }

    private async Task<DriveService> GetService()
    {
        return new DriveService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(await GetAccessToken())
        });
    }

    public async Task<List<FileItem>> GetFiles(string folder)
    {
        var service = await GetService();
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
        return files.Select(x => new FileItem
        {
            Id = x.Id,
            Name = x.Name,
            Type = x.MimeType
        }).ToList();
    }

    IAsyncEnumerator<FileItem> IAsyncEnumerable<FileItem>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return new FilesEnumerator(this);
    }

    internal async Task<bool> HasAccess()
    {
        return !string.IsNullOrEmpty(await GetAccessToken());
    }

    private class FilesEnumerator : IAsyncEnumerator<FileItem>
    {
        private GoogleAccess Access { get; }
        private Queue<FileItem> Queue = new Queue<FileItem>();

        public FilesEnumerator(GoogleAccess ga)
        {
            Access = ga;
            Queue.Enqueue(new FileItem { Id = "root", Type = FileItem.FolderType });
        }

        public FileItem Current => Queue.Peek();

        public async ValueTask<bool> MoveNextAsync()
        {
            if (Current.IsFolder)
            {
                var sub = await this.Access.GetFiles(Current.Id);
                foreach (var file in sub)
                {
                    file.Parent = Current;
                }
                foreach (var file in sub.Where(x => !x.IsFolder))
                {
                    Queue.Enqueue(file);
                }
                foreach (var file in sub.Where(x => x.IsFolder))
                {
                    Queue.Enqueue(file);
                }
            }
            Queue.Dequeue();
            return Queue.Count > 0;
        }

        public ValueTask DisposeAsync()
        {
            Queue.Clear();
            return ValueTask.CompletedTask;
        }
    }
}
