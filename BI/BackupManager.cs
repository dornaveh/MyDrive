using Newtonsoft.Json;
namespace MyDrive;

public class BackupManager
{
    private readonly StorageProvider _storageProvider;
    private readonly GoogleProvider _googleProvider;

    private readonly Dictionary<string, GetRootJob> _getRoots = new();
    private readonly List<(string userId, string cacheId, FileItem root)> _rootCache = new();
    private readonly List<BackupJob> jobs = new();

    public BackupManager(StorageProvider storageProvider, GoogleProvider googleProvider)
    {
        _storageProvider = storageProvider;
        _googleProvider = googleProvider;
    }

    public async Task<bool> GenerateCache(string userId)
    {
        if (_getRoots.ContainsKey(userId))
        {
            return false;
        }
        var ga = await _googleProvider.GetAccess(userId).ConfigureAwait(false);
        if (ga == null)
        {
            return false;
        }
        var gt = _getRoots[userId] = new GetRootJob(ga);
        _ = Task.Run(async () =>
        {
            try
            {
                var filename = DateTime.UtcNow.ToEpoch() + ".cache";
                var root = await gt.Get();
                var cache = JsonConvert.SerializeObject(root);
                var storage = _storageProvider.GetAccess(userId);
                await storage.Save(filename, cache);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _getRoots.Remove(userId);
            }
        });
        return true;
    }

    public async Task<CheckStatusResponse> Status(string userId)
    {
        var files = await _storageProvider.GetAccess(userId).ListFiles();
        return new CheckStatusResponse
        {
            CacheGenerationStatus = _getRoots.TryGetValue(userId, out var task) ? task.Count : -1,
            CacheTimeStamps = files.Where(x => x.EndsWith(".cache")).Select(name =>
            {
                return long.Parse(name.Substring(0, name.Length - ".cache".Length));
            }).ToList()
        };
    }

    public async Task<List<FileItem>> GetFiles(string userId, string cacheId, string folderId)
    {
        FileItem root = _rootCache.FirstOrDefault(x => x.userId.Equals(userId) && x.cacheId.Equals(cacheId)).root;
        if (root == null)
        {
            var rootjson = await _storageProvider.GetAccess(userId).ReadFile(cacheId + ".cache");
            root = JsonConvert.DeserializeObject<FileItem>(rootjson);
            if (root != null)
            {
                _rootCache.Add((userId, cacheId, root));
                while (_rootCache.Count > 1000)
                {
                    _rootCache.RemoveAt(0);
                }
            }
        }
        if (root != null)
        {
            var q = new Queue<FileItem>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                if (q.Peek().Id.Equals(folderId))
                {
                    return q.Peek().Children.Select(c => new FileItem
                    {
                        Binary = c.Binary,
                        Description = c.Description,
                        Name = c.Name,
                        Id = c.Id,
                        ParentId = c.ParentId,
                        Size = c.Size,
                        Type = c.Type,
                        Version = c.Version,
                    }).ToList();
                }
                q.Peek()?.Children?.ForEach(c => { q.Enqueue(c); });
                q.Dequeue();
            }
        }
        return null;
    }

    public async Task Backup(string fileId, string userId)
    {
        var ga = await _googleProvider.GetAccess(userId).ConfigureAwait(false);
        var bj = new BackupJob(userId, fileId, _storageProvider.GetAccess(userId), ga);
        jobs.Add(bj);
        _ = bj.Backup(x => jobs.Remove(x));
    }

    public async Task MarkDownloaded(string userId, List<FileItem> files)
    {
        var stored = await this._storageProvider.GetAccess(userId).ListFiles();
        var filesDic = files.ToDictionary(x => x.Id);
        foreach ( var name in stored) {
            if (name.EndsWith(".file")) {
                var id = name.Substring(0, name.Length - ".file".Length);
                if (filesDic.ContainsKey(id))
                {
                    filesDic[id].BackedUp = true;
                }
            }
        }
        foreach ( var job in jobs) { 
            if (job.UserId == userId)
            {
                if (filesDic.ContainsKey(job.FileId))
                {
                    filesDic[job.FileId].Downloading = job.Percentage;
                }
            }
        }
    }

    private class GetRootJob
    {
        private readonly GoogleAccess _access;
        private readonly Dictionary<string, FileItem> _dic = new();

        public GetRootJob(GoogleAccess access)
        {
            _access = access;
        }

        public int Count => _dic.Count;

        public async Task<FileItem> Get()
        {
            FileItem root = new()
            {
                Name = "Root",
                Id = "root",
                Type = FileItem.FolderType,
            };
            await foreach (FileItem file in _access)
            {
                _dic.Add(file.Id, file);
            }
            foreach (var file in _dic.Values)
            {
                if (!string.IsNullOrEmpty(file.ParentId))
                {
                    if (!_dic.TryGetValue(file.ParentId, out FileItem? parent))
                    {
                        parent = root;
                    }
                    parent.Children ??= new List<FileItem>();
                    parent.Children.Add(file);
                }
            }
            return root;
        }
    }
    public class BackupJob
    {
        public string UserId { get; }
        public string FileId { get; }
        public long FileSize { get; private set; } = 0;
        public long Downloaded { get; private set; } = 0;
        public double Percentage
        {
            get
            {
                if (FileSize > 0)
                {
                    return (double)Downloaded / FileSize;
                }
                return 0;
            }
        }
        private StorageAccess StorageAccess { get; }
        private GoogleAccess GoogleAccess { get; }

        public BackupJob(string userId, string fileId, StorageAccess storageAccess, GoogleAccess googleAccess)
        {
            UserId = userId;
            FileId = fileId;
            StorageAccess = storageAccess;
            GoogleAccess = googleAccess;
        }

        public async Task Backup(Action<BackupJob> onDone)
        {
            var temp = FileId + ".temp";
            var final = FileId + ".file";
            var req = await GoogleAccess.GetFileDownloadRequest(FileId);
            FileSize = req.size;
            using var upload = await StorageAccess.CreateUploadStream(temp, req.size);
            req.req.MediaDownloader.ProgressChanged += async x =>
            {
                Downloaded = x.BytesDownloaded;
                if (x.Status == Google.Apis.Download.DownloadStatus.Completed && x.BytesDownloaded == req.size)
                {
                    await StorageAccess.Rename(temp, final);
                    onDone(this);
                }
                if (x.Status == Google.Apis.Download.DownloadStatus.Failed)
                {
                    onDone(this);
                }
            };
            await req.req.DownloadAsync(upload);
        }
    }
}