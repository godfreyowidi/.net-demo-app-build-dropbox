using DropboxLike.Domain.Models;
using DropboxLike.Domain.Repositories.Share;

namespace DropboxLike.Domain.Services.Share;

public class ShareFileService : IShareFileService
{
    private readonly IFileShareRepository _FileShareRepository;

    public ShareFileService(IFileShareRepository FileShareRepository)
    {
        _FileShareRepository = FileShareRepository;
    }
    
    public Task<OperationResult<List<FileMetadata>>> GetSharedFilesByUserId(string userId)
    {
        return _FileShareRepository.GetSharedFilesByUserId(userId);
    }

    public Task<OperationResult<string>> ShareFileWithUsersAsync(IEnumerable<string> userIds, string fileId)
    {
        return _FileShareRepository.ShareFileWithUsersAsync(userIds, fileId);
    }
}