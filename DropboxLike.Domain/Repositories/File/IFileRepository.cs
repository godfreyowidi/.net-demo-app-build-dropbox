using DropboxLike.Domain.Models;

namespace DropboxLike.Domain.Repositories.File;

public interface IFileRepository
{
  Task<OperationResult<object>> UploadFileAsync(IFormFile file, string userId);
  Task<OperationResult<Models.File>> DownloadFileAsync(string fileId);
  Task<OperationResult<object>> DeleteFileAsync(string fileId);
  Task<OperationResult<List<FileMetadata>>> ListFilesAsync();
}