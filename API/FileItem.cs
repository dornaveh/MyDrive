using System.Text;

namespace MyDrive;

public class FileItem
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public FileItem Parent { get; set; } = null;
    public bool IsFolder { get => this.Type == FolderType; }
    public string FullName
    {
        get 
        {
            if (this.Parent == null)
            {
                return Name;
            }
            return this.Parent.FullName + "/" + Name;
        }
    }

    public const string FolderType = "application/vnd.google-apps.folder";
}
