using System.Net;
using DropboxLike.Domain.Data;
using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DropboxLike.Domain.Repositories.Folder;

public class FolderRepository : IFolderRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public FolderRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task<OperationResult<FolderEntity>> CreateFolderAsync(string folderName, string userId)
    {
        var existingFolder = await _applicationDbContext.Folders
                                  .FirstOrDefaultAsync(f => f.FolderName == folderName && f.UserId == userId);
    
        if (existingFolder != null)
        {
            string uniqueFolderName;
            int counter = 1;
            do
            {
                uniqueFolderName = $"{folderName}({counter++})";
                existingFolder = await _applicationDbContext.Folders
                                     .FirstOrDefaultAsync(f => f.FolderName == uniqueFolderName && f.UserId == userId);
            } while (existingFolder != null);
    
            folderName = uniqueFolderName;
        }
    
        var newFolder = new FolderEntity
        {
            FolderId = Guid.NewGuid().ToString(),
            FolderName = folderName,
            UserId = userId
        };
    
        try
        {
            _applicationDbContext.Folders!.Add(newFolder);
            await _applicationDbContext.SaveChangesAsync();
    
            return OperationResult<FolderEntity>.Success(newFolder);
        }
        catch (Exception ex)
        {
            return OperationResult<FolderEntity>.Fail(ex.Message);
        }
    }
    
     public async Task<OperationResult<FolderEntity>> GetFolderAsync(string folderId, string userId)
    {
        var folder = await _applicationDbContext.Folders!
            .FirstOrDefaultAsync(f => f.FolderId == folderId && f.UserId == userId);

        if (folder == null)
        {
            return OperationResult<FolderEntity>.Fail("Folder not found.", HttpStatusCode.NotFound);
        }

        return OperationResult<FolderEntity>.Success(folder);
    }

    public async Task<OperationResult<bool>> UpdateFolderAsync(string folderId, string newFolderName, string userId)
    {
        var folder = await _applicationDbContext.Folders!
            .FirstOrDefaultAsync(f => f.FolderId == folderId && f.UserId == userId);

        if (folder == null)
        {
            return OperationResult<bool>.Fail("Folder not found.", HttpStatusCode.NotFound);
        }

        folder.FolderName = newFolderName;
        _applicationDbContext.Folders!.Update(folder);
        await _applicationDbContext.SaveChangesAsync();

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> DeleteFolderAsync(string folderId, string userId)
    {
        var folder = await _applicationDbContext.Folders!
            .FirstOrDefaultAsync(f => f.FolderId == folderId && f.UserId == userId);

        if (folder == null)
        {
            return OperationResult<bool>.Fail("Folder not found.", HttpStatusCode.NotFound);
        }

        _applicationDbContext.Folders?.Remove(folder);
        await _applicationDbContext.SaveChangesAsync();

        return OperationResult<bool>.Success(true);
    }

    public async Task<bool> UserHasAccessToFolderAsync(string userId, string folderId)
    {
        try
        {
            var folder = await _applicationDbContext.Folders!
                .Include(f => f.SharedWithUsers)
                .FirstOrDefaultAsync(f => f.FolderId == folderId);

            if (folder == null)
            {
                return false;
            }

            if (folder.UserId == userId)
            {
                return true;
            }

            return folder.SharedWithUsers.Any(s => s.UserId == userId);
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<IEnumerable<FolderEntity>> GetAccessibleFoldersAsync(string userId)
    {
        try
        {
            // Combine queries to get both owned and shared folders in a single query
            var accessibleFolders = _applicationDbContext.Folders!
                .Where(folder => folder.UserId == userId || folder.SharedWithUsers.Any(sf => sf.UserId == userId))
                .Distinct();

            return await accessibleFolders.ToListAsync();
        }
        catch (Exception ex)
        {
            // Log the exception here and handle accordingly
            // Depending on your error handling strategy, you may choose to return an empty list or rethrow the exception
            throw; // or return Enumerable.Empty<FolderEntity>();
        }
    }
}