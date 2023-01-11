using Newtonsoft.Json;
using System.Text;

namespace MyDrive;

public class FileItem
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Id { get; set; }
    public string Description { get; set; }
    public List<FileItem> Children { get; set; }
    public long? Version { get; set; }
    public bool Binary { get; set; }
    public long? Size { get; set; }
    public double? Downloading { get; set; } = -1;
    
    [JsonIgnore]
    public bool BackedUp { get; set; }
    [JsonIgnore]
    public FileItem Parent { get; set; } = null;
    [JsonIgnore]
    public string? ParentId { get; set; } = null;
    [JsonIgnore]
    public bool IsFolder { get => this.Type == FolderType; }
    [JsonIgnore]
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

    public FileItem() { }

    public FileItem(GoogleFile file)
    {
        Id = file.Id;
        Name = file.Name;
        Type = file.MimeType;
        ParentId = file.Parents?.FirstOrDefault();
        Description = file.Description;
        Binary = file.Md5Checksum != null;
        Version = file.Version;
        Size = file.Size;
    }
}
