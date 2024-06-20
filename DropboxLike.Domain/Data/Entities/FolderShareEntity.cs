namespace DropboxLike.Domain.Data.Entities;

public class FolderShareEntity
{
    public string UserId { get; set; }
    public string FolderId { get; set; }

    public UserEntity User { get; set; } = null!;
    public virtual FolderEntity Folder { get; set; } = null!;
}
