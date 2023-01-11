using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.IdentityModel.Tokens;
using System.Collections;

namespace MyDrive;

public class GoogleAccess : IAsyncEnumerable<FileItem>
{
    private const string fields = "files/name, files/id, files/mimeType, files/parents, files/description, files/version, files/size, files/md5Checksum, nextPageToken";
    private Func<Task<string?>> GetAccessToken { get; }

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
        req.Fields = fields;
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
        return files.Select(x => new FileItem(x)).ToList();
    }

    IAsyncEnumerator<FileItem> IAsyncEnumerable<FileItem>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return new SingleQueryFilesEnumerator(this);
    }

    internal async Task<bool> HasAccess()
    {
        return !string.IsNullOrEmpty(await GetAccessToken());
    }

    internal async Task<(FilesResource.GetRequest req, long size)> GetFileDownloadRequest(string fileId)
    {
        var service = await GetService();
        var req = service.Files.Get(fileId);
        req.Fields = "size";
        var file = await req.ExecuteAsync();
        return (service.Files.Get(fileId), file.Size ?? -1);
    }

    private class SingleQueryFilesEnumerator : IAsyncEnumerator<FileItem>
    {
        private GoogleAccess Access { get; }
        private Queue<FileItem> Queue = new Queue<FileItem>();
        private string? NextPageToken = null;
        private bool first = true;

        public SingleQueryFilesEnumerator(GoogleAccess ga)
        {
            Access = ga;
        }

        public FileItem Current => Queue.Peek();

        private async Task Next()
        {
            var service = await Access.GetService();
            var req = service.Files.List();
            req.Fields = fields;
            req.PageSize = 1000;
            req.PageToken = NextPageToken;
            NextPageToken = null;
            var fileList = await req.ExecuteAsync();
            if (!fileList.NextPageToken.IsNullOrEmpty())
            {
                NextPageToken = fileList.NextPageToken;
            }
            foreach (var file in fileList.Files)
            {
                Queue.Enqueue(new FileItem(file));
            }
        }

        public ValueTask DisposeAsync()
        {
            Queue.Clear();
            NextPageToken = null;
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (first)
            {
                first = false;
                await Next();
                return Queue.Count > 0;
            }
            Queue.Dequeue();
            if (Queue.Count == 0 && !string.IsNullOrEmpty(NextPageToken))
            {
                await Next();
            }
            return Queue.Count > 0;
        }
    }
}
