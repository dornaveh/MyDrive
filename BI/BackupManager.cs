using Newtonsoft.Json;

namespace MyDrive;

public class BackupManager
{
    public void Run(GoogleAccess access)
    {
        Task.Run(async () => { await List(access); });
    }

    private async Task List(GoogleAccess access)
    {
        int i = 0;
        string path = @"e:\foo.txt";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        Dictionary<string, FileItem> dic = new();
        FileItem root = new FileItem
        {
            Name = "Root",
            Type = FileItem.FolderType,
        };
        await foreach (FileItem file in access)
        {
            dic.Add(file.Id, file);
            if (i++ % 1000 == 0)
            {
                Console.WriteLine(i);
            }
        }
        foreach (var file in dic.Values)
        {
            if (!string.IsNullOrEmpty(file.ParentId))
            {
                if (!dic.TryGetValue(file.ParentId, out FileItem? parent))
                {
                    parent = root;
                }
                parent.Children ??= new List<FileItem>();
                parent.Children.Add(file);
            }
        }
        using FileStream fs = File.OpenWrite(path);
        using StreamWriter sw = new(fs);
        var str = JsonConvert.SerializeObject(root);
        sw.WriteLine(str);
        fs.Flush();
    }
}