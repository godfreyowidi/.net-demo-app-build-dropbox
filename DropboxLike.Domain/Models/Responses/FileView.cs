namespace DropboxLike.Domain.Models.Responses;

public class FileView
{
    public string Content { get; set; }
    public string ContentType { get; set; }
    public bool IsBase64Encoded { get; set; }
}