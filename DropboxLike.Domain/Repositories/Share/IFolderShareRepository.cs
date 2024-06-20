using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using DropboxLike.Domain.Models.Requests;

namespace DropboxLike.Domain.Repositories.Share;

public interface IFolderShareRepository
{
    Task<OperationResult<string>> ShareFolderWithUserAsync(string folderId, string userId, string shareWithUserId);
}