using DropboxLike.Domain.Models;
using DropboxLike.Domain.Models.Responses;
using DropboxLike.Domain.Repositories.File;

namespace DropboxLike.Domain.Services.File;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;

  public FileService(IFileRepository fileRepository)
  {
    _fileRepository = fileRepository;
  }

    public async Task<OperationResult<object>> UploadSingleFileAsync(IFormFile file, string userId, string? folderId = null)
    {
        return await _fileRepository.UploadFileAsync(file, userId, folderId);
    }

    public async Task<OperationResult<Models.File>> DownloadSingleFileAsync(string fileId, string  userId)
    {
        return await _fileRepository.DownloadFileAsync(fileId, userId);
    }

    public async Task<OperationResult<object>> DeleteSingleFileAsync(string fileId)
    {
        return await _fileRepository.DeleteFileAsync(fileId);
    }

    public async Task<OperationResult<List<FileMetadata>>> ListBucketFilesAsync(string userId)
    {
        return await _fileRepository.ListFilesAsync(userId);
    }

    public async Task<OperationResult<FileView>> ViewFileWithEditAsync(string fileId, string userId)
    {
        return await _fileRepository.ViewFileAsync(fileId, userId);
    }
}