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
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
        using (FileStream fs = File.OpenWrite(path))
        using (StreamWriter sw = new StreamWriter(fs))
        {
            await foreach (FileItem file in access)
            {
                var l = ++i + ": " + file.FullName;
                Console.WriteLine(l);
                sw.WriteLine(i);
                if (i % 100 == 0)
                {
                    sw.Flush();
                    fs.Flush();
                }
            }
        }
    }
}