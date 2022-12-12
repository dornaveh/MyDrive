using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Text;

namespace MyDrive;

public class StorageAccess
{
    private readonly string _connection;
    private readonly string _filename;
    private readonly string _authId;

    public StorageAccess(string connection, string filename, string authId)
    {
        _connection = connection;
        _filename = filename;
        _authId = authId;
    }

    private async Task<ShareDirectoryClient> GetDirectory()
    {
        ShareClient share = new ShareClient(_connection, "mydrive");
        var dir = share.GetRootDirectoryClient();
        dir = dir.GetSubdirectoryClient(this._authId);
        await dir.CreateIfNotExistsAsync();
        return dir;
    }

    private async Task<ShareFileClient> GetFileClient()
    {
        var dir = await GetDirectory();
        return dir.GetFileClient(_filename);
    }

    public async Task<Stream> Open(long from, int length)
    {
        var file = await GetFileClient();
        var opt = new ShareFileOpenReadOptions(false)
        {
            BufferSize = length,
            Position = from,
        };
        return await file.OpenReadAsync(opt);
    }

    public async Task<string> ReadFile()
    {
        var file = await GetFileClient();
        using Stream str = await file.OpenReadAsync();
        using StreamReader sr = new StreamReader(str);
        return await sr.ReadToEndAsync();
    }

    internal async Task<Stream> CreateUploadStream(long size)
    {
        var file = await GetFileClient();
        var opt = new ShareFileOpenWriteOptions()
        {
            MaxSize = size,
        };
        return await file.OpenWriteAsync(true, 0, opt);
    }

    internal async Task<List<string>> GetFiles()
    {
        var res = new List<string>();
        var dir = await GetDirectory();
        await foreach (var file in dir.GetFilesAndDirectoriesAsync())
        {
            res.Add(file.Name);
        }
        return res;
    }

    internal async Task Delete()
    {
        var file = await GetFileClient();
        await file.DeleteIfExistsAsync();
    }

    internal async Task Rename(string newFilename)
    {
        var file = await GetFileClient();
        await file.RenameAsync((await GetDirectory()).GetFileClient(newFilename).Path);
    }
}
