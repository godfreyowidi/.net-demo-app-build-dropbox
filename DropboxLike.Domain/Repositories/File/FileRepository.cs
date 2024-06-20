using System.Globalization;
using System.Net;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using DropboxLike.Domain.Configuration;
using DropboxLike.Domain.Data;
using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using DropboxLike.Domain.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DropboxLike.Domain.Repositories.File;

public class FileRepository : IFileRepository
{
    private readonly string? _bucketName;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IAmazonS3 _awsS3Client;

    public FileRepository(IOptions<AwsConfiguration> awsOptions,
        ApplicationDbContext applicationDbContext)
    {
        var configuration = awsOptions.Value;
        _bucketName = configuration.BucketName;
        _awsS3Client = new AmazonS3Client(configuration.AwsAccessKey, configuration.AwsSecretAccessKey,
            RegionEndpoint.GetBySystemName(configuration.Region));
        _applicationDbContext = applicationDbContext;
    }

    public async Task<OperationResult<object>> UploadFileAsync(IFormFile file, string userId, string? folderId = null)
{
    if (file == null || file.Length == 0)
    {
        return OperationResult<object>.Fail("Invalid file.", HttpStatusCode.BadRequest);
    }

    try
    {
        var user = await _applicationDbContext.AppUsers!.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return OperationResult<object>.Fail("User not found.", HttpStatusCode.NotFound);
        }

        string folderPath;
        if (string.IsNullOrEmpty(folderId))
        {
            folderId = ""; // What do I do here
            folderPath = $"user_{user.Id}/";
        }
        else
        {
            var folder = await _applicationDbContext.Folders.FirstOrDefaultAsync(f => f.FolderId == folderId && f.UserId == userId);
            if (folder == null)
            {
                return OperationResult<object>.Fail("Folder not found.", HttpStatusCode.NotFound);
            }
            folderPath = $"user_{user.Id}/{folder.FolderName}/";
        }

        using var newMemoryStream = new MemoryStream();
        var filePath = $"{folderPath}{file.FileName}";
        await file.CopyToAsync(newMemoryStream);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = newMemoryStream,
            Key = filePath,
            BucketName = _bucketName,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.NoACL
        };

        var transferUtility = new TransferUtility(_awsS3Client);
        await transferUtility.UploadAsync(uploadRequest);

        var fileModel = new FileEntity
        {
            FileKey = WebUtility.UrlEncode(uploadRequest.Key),
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length.ToString(),
            ContentType = file.ContentType,
            TimeStamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
            FolderId = folderId,
            UserId = userId
        };

        _applicationDbContext.FileModels?.Add(fileModel);
        await _applicationDbContext.SaveChangesAsync();

        return OperationResult<object>.Success(new object(), HttpStatusCode.Created);
    }
    catch (AmazonS3Exception exception)
    {
        var message = $"{exception.StatusCode}: {exception.Message}";
        return OperationResult<object>.Fail(exception, message, exception.StatusCode);
    }
    catch (Exception exception)
    {
        var message = $"{HttpStatusCode.InternalServerError}: {exception.Message}";
        return OperationResult<object>.Fail(exception, message);
    }
}

    public async Task<OperationResult<Models.File>> DownloadFileAsync(string fileId, string userId)
    {
        try
        {
            var user = await _applicationDbContext.AppUsers!.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return OperationResult<Models.File>.Fail("User not found.", HttpStatusCode.NotFound);
            }

            var file = await _applicationDbContext.FileModels!.FindAsync(fileId);
            if (file == null || !file.FilePath!.StartsWith($"user_{user.Id}"))
            {
                return OperationResult<Models.File>.Fail(
                    $"{HttpStatusCode.NotFound}: File not found or not related to user.", HttpStatusCode.NotFound);
            }

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
            };

            var fileNames = new List<string>();

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _awsS3Client.ListObjectsV2Async(listRequest);

                fileNames.AddRange(listResponse.S3Objects.Select(obj => obj.Key));

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            } while (listResponse.IsTruncated);

            foreach (var obj in fileNames)
            {
                if (file?.FileKey != WebUtility.UrlEncode(obj)) continue;
                var downloadFileName = file.FileName;
                var filePath = Path.Combine(@"C:\Users\godfr\Downloads", downloadFileName!);

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = WebUtility.UrlDecode(fileId)
                };
                using var response = await _awsS3Client.GetObjectAsync(request);
                {
                    await using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await response.ResponseStream.CopyToAsync(fileStream);
                    }

                    var contentType = response.Headers.ContentType;
                    return OperationResult<Models.File>.Success(new Models.File
                    {
                        FileStream = response.ResponseStream,
                        ContentType = contentType
                    });
                }
            }

            return OperationResult<Models.File>.Fail($"{HttpStatusCode.NotFound}: File not found.",
                HttpStatusCode.NotFound);
        }
        catch (AmazonS3Exception exception)
        {
            var message = $"{exception.StatusCode}: {exception.Message}";
            return OperationResult<Models.File>.Fail(exception, message, exception.StatusCode);
        }
        catch (Exception exception)
        {
            var message = $"{HttpStatusCode.InternalServerError}: {exception.Message}";
            return OperationResult<Models.File>.Fail(exception, message);
        }
    }

    public async Task<OperationResult<FileView>> ViewFileAsync(string fileId, string userId)
    {
        try
        {
            var file = await _applicationDbContext.FileModels!.FindAsync(fileId);

            if (file == null)
            {
                throw new FileNotFoundException("File not Found");
            }

            var extractedUserId = fileId.Split('%')[0].Split('_')[1];
            var isOwner = extractedUserId == userId;

            bool isShared =
                await _applicationDbContext.SharedFiles!.AnyAsync(s => s.FileId == fileId && s.UserId == userId);

            if (!isOwner && !isShared)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this file.");
            }

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = WebUtility.UrlDecode(fileId)
            };

            using var response = await _awsS3Client.GetObjectAsync(request);
            var contentType = response.Headers.ContentType;

            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            var contentAsBytes = memoryStream.ToArray();

            string content;
            bool isBase64;

            if (IsTextBasedContent(contentType))
            {
                content = Encoding.UTF8.GetString(contentAsBytes);
                isBase64 = false;
            }
            else
            {
                content = Convert.ToBase64String(contentAsBytes);
                isBase64 = true;
            }

            return OperationResult<FileView>.Success(new FileView
            {
                Content = content,
                ContentType = contentType,
                IsBase64Encoded = isBase64
            });
        }
        catch (AmazonS3Exception exception)
        {
            var message = $"{exception.StatusCode}: {exception.Message}";
            return OperationResult<FileView>.Fail(exception, message, exception.StatusCode);
        }
        catch (Exception exception)
        {
            var message = $"{HttpStatusCode.InternalServerError}: {exception.Message}";
            return OperationResult<FileView>.Fail(exception, message);
        }
    }

    private bool IsTextBasedContent(string contentType)
    {
        var textBasedContentTypes = new List<string>
        {
            "text/plain",
            "text/csv",
            "text/html",
            "application/json",
            "application/xml"
        };

        return textBasedContentTypes.Contains(contentType);
    }

    public async Task<OperationResult<List<FileMetadata>>> ListFilesAsync(string userId)
    {
        try
        {
            var user = await _applicationDbContext.AppUsers!.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return OperationResult<List<FileMetadata>>.Fail("User not found.", HttpStatusCode.NotFound);
            }

            var files = await _applicationDbContext
                .FileModels!
                .Select(file => new FileMetadata
                {
                    FileKey = file.FileKey,
                    FileName = file.FileName,
                    FileSize = file.FileSize,
                    FilePath = $"user_{user.Id}/{file.FileName}",
                    ContentType = file.ContentType,
                    TimeStamp = file.TimeStamp
                })
                .ToListAsync();
            return OperationResult<List<FileMetadata>>.Success(files);
        }
        catch (Exception exception)
        {
            var message = $"{HttpStatusCode.InternalServerError}: {exception.Message}";
            return OperationResult<List<FileMetadata>>.Fail(exception, message);
        }
    }

    public async Task<OperationResult<object>> DeleteFileAsync(string fileId)
    {
        var results = await _applicationDbContext.FileModels!
            .FirstOrDefaultAsync(x => x.FileKey == fileId);

        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
        };
        var listResponse = await _awsS3Client.ListObjectsV2Async(listRequest);

        foreach (var obj in listResponse.S3Objects)
        {
            if (results?.FileKey != obj.Key) continue;
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = WebUtility.HtmlDecode(fileId).ToLowerInvariant()
            };

            await _awsS3Client.DeleteObjectAsync(request);

            _applicationDbContext.FileModels!.Remove(results);
            await _applicationDbContext.SaveChangesAsync();

            return OperationResult<object>.Success(new object(), HttpStatusCode.NoContent);
        }

        return OperationResult<object>.Fail($"{HttpStatusCode.NotFound}: File not found.", HttpStatusCode.NotFound);
    }
    
    public async Task<OperationResult<bool>> MoveFileAsync(string fileId, string newFolderId, string userId)
    {
        try
        {
            var fileAndFolder = await _applicationDbContext.FileModels!
                .Where(f => f.FileKey == fileId && f.UserId == userId)
                .Select(f => new
                {
                    File = f,
                    NewFolder = _applicationDbContext.Folders!
                        .FirstOrDefault(fld => fld.FolderId == newFolderId && fld.UserId == userId)
                })
                .FirstOrDefaultAsync();

            if (fileAndFolder?.File == null)
            {
                return OperationResult<bool>.Fail("File not found or access denied.", HttpStatusCode.NotFound);
            }

            if (fileAndFolder.NewFolder == null)
            {
                return OperationResult<bool>.Fail("Target folder not found or access denied.", HttpStatusCode.NotFound);
            }

            if (fileAndFolder.File.FolderId == newFolderId)
            {
                return OperationResult<bool>.Fail("File is already in the specified folder.", HttpStatusCode.BadRequest);
            }

            fileAndFolder.File.FolderId = newFolderId;
            _applicationDbContext.Update(fileAndFolder.File);
            await _applicationDbContext.SaveChangesAsync();

            return OperationResult<bool>.Success(true, HttpStatusCode.OK);
        }
        catch (DbUpdateException ex)
        {
            return OperationResult<bool>.Fail("An error occurred while moving the file: " + ex.Message, HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Fail("Unexpected error: " + ex.Message, HttpStatusCode.InternalServerError);
        }
    }
    
    public async Task<bool> UserHasAccessToFileAsync(string userId, string fileId)
    {
        var file = await _applicationDbContext.FileModels!
            .Include(f => f.SharedWithUsers)
            .Include(f => f.Folder)
            .ThenInclude(folder => folder.SharedWithUsers)
            .FirstOrDefaultAsync(f => f.FileKey == fileId);

        if (file == null) return false;

        if (file.UserId == userId) return true;

        if (file.SharedWithUsers.Any(s => s.UserId == userId)) return true;

        return file.Folder != null && 
               (file.Folder.UserId == userId || file.Folder.SharedWithUsers.Any(s => s.UserId == userId));
    }
    
    public async Task<IEnumerable<FileEntity>> GetAccessibleFilesAsync(string userId)
    {
        try
        {
            // Combine queries to get both owned and shared files in a single query
            var accessibleFiles = _applicationDbContext.FileModels!
                .Where(file => file.UserId == userId || file.SharedWithUsers.Any(sf => sf.UserId == userId))
                .Distinct();

            return await accessibleFiles.ToListAsync();
        }
        catch (Exception ex)
        {
            // Log the exception here and handle accordingly
            // Depending on your error handling strategy, you may choose to return an empty list or rethrow the exception
            throw; // or return Enumerable.Empty<FileEntity>();
        }
    }
}