using System.Net;
using Azure;
using DropboxLike.Domain.Data;
using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DropboxLike.Domain.Repositories.Share;

public class FileShareRepository : IFileShareRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public FileShareRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task<OperationResult<string>> ShareFileWithUsersAsync(IEnumerable<string> userIds, string fileId)
    {
        try
        {
            foreach (var userId in userIds)
            {
                var existingSharedFile = await _applicationDbContext.SharedFiles!
                    .FirstOrDefaultAsync(sf => sf.UserId == userId && sf.FileId == fileId);

                if (existingSharedFile != null)
                {
                    return OperationResult<string>.Fail("File is already shared with the user.", HttpStatusCode.Conflict);
                }

                var sharedFile = new FileShareEntity
                {
                    UserId = userId,
                    FileId = fileId,
                };
                _applicationDbContext.SharedFiles!.Add(sharedFile);
            }
            await _applicationDbContext.SaveChangesAsync();
            return OperationResult<string>.Success("File share successfully.");
        }
        catch (Exception)
        {
            return OperationResult<string>.Fail("An error occurred while sharing the file with a user. Try again later.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<List<FileMetadata>>> GetSharedFilesByUserId(string userId)
    {
        var sharedFiles = await _applicationDbContext.SharedFiles!
            .Where(file => file.UserId == userId)
            .Join(
                _applicationDbContext.FileModels!,
                sharedFile => sharedFile.FileId,
                file => file.FileKey,
                (sharedFile, file) => file)
            .Select(file => new FileMetadata
            {
                FileKey = file.FileKey,
                FileName = file.FileName,
                FileSize = file.FileSize,
                FilePath = file.FilePath,
                ContentType = file.ContentType,
                TimeStamp = file.TimeStamp
            })
            .ToListAsync();
        return OperationResult<List<FileMetadata>>.Success(sharedFiles);
    }
}