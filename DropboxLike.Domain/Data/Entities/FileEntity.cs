
using System.ComponentModel.DataAnnotations;

namespace DropboxLike.Domain.Data.Entities;

public class FileEntity
{
  [Key]
  public string FileKey { get; set; }
  public string FileName { get; set; } = string.Empty;
  public string? FileSize { get; set; }
  public string? FilePath { get; set; }
  public string? ContentType { get; set; }
  public string? TimeStamp { get; set; }
    
  // Reference to the User
  public string UserId { get; set; }
  public virtual UserEntity User { get; set; }

  // Reference to the Folder
  public string? FolderId { get; set; }
  public virtual FolderEntity? Folder { get; set; }

  // Shared information
  public virtual ICollection<FileShareEntity> SharedWithUsers { get; set; } = new List<FileShareEntity>();
}
