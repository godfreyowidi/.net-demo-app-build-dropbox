using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;

namespace DropboxLike.Domain.Repositories.Folder;

public interface IFolderRepository
{
    Task<OperationResult<FolderEntity>> CreateFolderAsync(string folderName, string userId);
}