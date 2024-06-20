using DropboxLike.Domain.Models;
using DropboxLike.Domain.Models.Responses;

namespace DropboxLike.Domain.Services.File;

public interface IFileService
{
    Task<OperationResult<object>> UploadSingleFileAsync(IFormFile file, string userId, string? folderId = null);
    Task<OperationResult<Models.File>> DownloadSingleFileAsync(string fileId, string userId);
    Task<OperationResult<object>> DeleteSingleFileAsync(string fileId);
    Task<OperationResult<List<FileMetadata>>> ListBucketFilesAsync(string userId);
    Task<OperationResult<FileView>> ViewFileWithEditAsync(string fileId, string userId);
}