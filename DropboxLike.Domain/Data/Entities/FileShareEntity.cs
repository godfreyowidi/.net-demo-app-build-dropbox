
namespace DropboxLike.Domain.Data.Entities;

public class FileShareEntity
{
    public string UserId { get; set; }
    public string FileId { get; set; }

    public UserEntity User { get; set; } = null!;
    public virtual FileEntity File { get; set; } = null!;
}