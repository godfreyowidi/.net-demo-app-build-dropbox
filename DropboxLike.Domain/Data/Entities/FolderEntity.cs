using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DropboxLike.Domain.Data.Entities;

public class FolderEntity
{
    [Key]
    public string? FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public string UserId { get; set; }
    public virtual UserEntity User { get; set; }
    
    // Folder hierarchy
    public string? ParentFolderId { get; set; }
    public virtual FolderEntity ParentFolder { get; set; } = null!;
    public virtual ICollection<FolderEntity> ChildFolders { get; set; } = new List<FolderEntity>();

    // Files in the Folder
    public virtual ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();

    // Shared information
    public virtual ICollection<FolderShareEntity> SharedWithUsers { get; set; } = new List<FolderShareEntity>();
}