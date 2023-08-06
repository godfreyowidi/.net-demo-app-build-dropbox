﻿using DropboxLike.Domain.Data;
using DropboxLike.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DropboxLike.Domain.Repositories.Share;

public class ShareFileRepository : IShareFileRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public ShareFileRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task<OperationResult<List<FileMetadata>>> GetSharedFilesByUserId(string userId)
    {
        var sharedFiles = await _applicationDbContext.SharedFiles!
            .Where(file => file.UserId == userId)
            .Join(
                _applicationDbContext.FileModels,
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