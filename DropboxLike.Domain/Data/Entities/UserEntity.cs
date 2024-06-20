using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DropboxLike.Domain.Data.Entities;

public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    [Required]
    [MaxLength(30)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    // Files and Folders owned by the User
    public virtual ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    public virtual ICollection<FolderEntity> Folders { get; set; } = new List<FolderEntity>();
 
    // Sharing relationships
    public virtual ICollection<FileShareEntity> FileShares { get; set; } = new List<FileShareEntity>();
    public virtual ICollection<FolderShareEntity> FolderShares { get; set; } = new List<FolderShareEntity>();
}