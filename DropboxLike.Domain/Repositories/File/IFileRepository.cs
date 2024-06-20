using DropboxLike.Domain.Models;
using DropboxLike.Domain.Models.Responses;

namespace DropboxLike.Domain.Repositories.File;

public interface IFileRepository
{
  Task<OperationResult<object>> UploadFileAsync(IFormFile file, string userId, string? folderId = null);
  Task<OperationResult<Models.File>> DownloadFileAsync(string fileId, string userId);
  Task<OperationResult<object>> DeleteFileAsync(string fileId);
  Task<OperationResult<List<FileMetadata>>> ListFilesAsync(string userId);
  Task<OperationResult<FileView>> ViewFileAsync(string fileId, string userId);

}