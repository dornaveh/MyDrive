using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace MyDrive;

public class StorageProvider
{
    private readonly IConfiguration _config;

    public StorageProvider(IConfiguration config)
    {
        _config = config;
    }

    public StorageAccess GetAccess(string userId)
    {
        return new StorageAccess(_config.GetBlobStorageConnectionString(), userId);
    }
}

public class StorageAccess
{
    private readonly string _connection;
    private readonly string _authId;

    public StorageAccess(string connection, string authId)
    {
        _connection = connection;
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

    private async Task<ShareDirectoryClient> GetBackupDirectory()
    {
        ShareClient share = new ShareClient(_connection, "mydrive");
        var dir = share.GetRootDirectoryClient();
        dir = dir.GetSubdirectoryClient(this._authId);
        await dir.CreateIfNotExistsAsync();
        dir = dir.GetSubdirectoryClient("backup");
        await dir.CreateIfNotExistsAsync();
        return dir;
    }

    private async Task<ShareFileClient> GetFileClient(string filename)
    {
        var dir = await GetDirectory();
        return dir.GetFileClient(filename);
    }

    /*public async Task<Stream> Open(string filename, long from, int length)
    {
        var file = await GetFileClient(filename);
        var opt = new ShareFileOpenReadOptions(false)
        {
            BufferSize = length,
            Position = from,
        };
        return await file.OpenReadAsync(opt);
    }*/

    public async Task<List<string>> ListFiles()
    {
        var str = await this.ReadFile("backup.drop");
        var backup = str != null ? JsonConvert.DeserializeObject<HashSet<string>>(str) : null;
        if (backup == null)
        {
            var list = new HashSet<string>();
            Azure.AsyncPageable<ShareFileItem> backupFolderFiles = (await GetBackupDirectory()).GetFilesAndDirectoriesAsync();
            await foreach (var file in backupFolderFiles)
            {
                list.Add(file.Name);
            }
            await Save("backup.drop", JsonConvert.SerializeObject(str));
            backup = list;
        }
        var dir = await GetDirectory();
        Azure.AsyncPageable<ShareFileItem> files = dir.GetFilesAndDirectoriesAsync();
        var ans = new List<string>(backup);
        var toBackup = new List<string>();
        await foreach (var file in files)
        {
            ans.Add(file.Name);
            if (file.Name.EndsWith(".file"))
            {
                toBackup.Add(file.Name);
                backup.Add(file.Name);
            }
        }
        await Save("backup.drop", JsonConvert.SerializeObject(backup));
        foreach (var tb in toBackup)
        {
            await MoveToBackup(tb);
        }
        return ans;
    }

    public async Task Save(string filename, string value)
    {
        await Delete(filename);
        if (value == null)
        {
            return;
        }
        using var mem = new MemoryStream();
        using DeflateStream gzip = new DeflateStream(mem, CompressionMode.Compress);
        using var tempWriter = new StreamWriter(gzip, Encoding.UTF8);
        await tempWriter.WriteAsync(value);
        await tempWriter.FlushAsync();
        var arr = mem.ToArray();
        using var uploader = await CreateUploadStream(filename, arr.LongLength);
        await uploader.WriteAsync(arr, 0, arr.Length);
        await uploader.FlushAsync();
    }

    public async Task<string?> ReadFile(string filename)
    {
        try
        {
            var file = await GetFileClient(filename);
            using Stream inputStream = await file.OpenReadAsync();
            using DeflateStream gzip = new DeflateStream(inputStream, CompressionMode.Decompress);
            using StreamReader sr = new StreamReader(gzip, Encoding.UTF8);
            return await sr.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }

    internal async Task<Stream> CreateUploadStream(string filename, long size)
    {
        var file = await GetFileClient(filename);
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

    internal async Task Delete(string filename)
    {
        var file = await GetFileClient(filename);
        await file.DeleteIfExistsAsync();
    }

    internal async Task MoveToBackup(string filename)
    {
        var file = await GetFileClient(filename);
        if (await file.ExistsAsync())
        {
            var target = (await GetBackupDirectory()).GetFileClient(filename).Path;
            await file.RenameAsync(target, new ShareFileRenameOptions
            {
                ReplaceIfExists = true
            });
        }
    }

    internal async Task Rename(string from, string to)
    {
        var file = await GetFileClient(from);
        await file.RenameAsync((await GetDirectory()).GetFileClient(to).Path);
    }
}
