using System.Text;
using DropboxLike.Domain.Services.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropboxLike.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : BaseController
{
  private readonly IFileService _fileService;

  public FileController(IFileService fileService)
  {
    _fileService = fileService;
  }
  
  [HttpPost]
  [Route("Upload")]
  public async Task<IActionResult> UploadFileAsync(IFormFile file, string? folderId = null)
  {
    var userId = GetUserIdFromClaim();

    var response = await _fileService.UploadSingleFileAsync(file, userId, folderId);

    return StatusCode(response.StatusCode);
  }

  [HttpGet]
  [Route("Download/{fileId}")]
  public async Task<IActionResult> DownloadFileAsync(string fileId)
  {
    var userId = GetUserIdFromClaim();
    
    var response = await _fileService.DownloadSingleFileAsync(fileId, userId);

    if (!response.IsSuccessful)
    {
      var message = $"Failed to download file with ID {fileId} due to '{response.FailureMessage ?? "<>"}'";
      return StatusCode(response.StatusCode, message);
    }
    var file = response.Value;
    return new FileStreamResult(file.FileStream, file.ContentType);
  }

  [HttpGet]
  [Route("List")]
  public async Task<IActionResult> ListFilesAsync()
  {
    var userId = GetUserIdFromClaim();
    
    var response = await _fileService.ListBucketFilesAsync(userId);
    
    return Ok(response.Value);
  }

  [HttpDelete]
  [Route("Delete/{fileId}")]
  public async Task<IActionResult> DeleteFileAsync(string fileId)
  {
    var userId = GetUserIdFromClaim();
    
    var response = await _fileService.DeleteSingleFileAsync(fileId);
    if (response.IsSuccessful) return NoContent();
    var message = $"Failed to delete file with ID {fileId} due to '{response.FailureMessage ?? "<>"}'";
    return StatusCode(response.StatusCode, message);
  }
  
  [HttpGet]
  [Route("View/{fileId}")]
  public async Task<IActionResult> ViewFileAsync(string fileId)
  {
    var userId = GetUserIdFromClaim();
    var result = await _fileService.ViewFileWithEditAsync(fileId, userId);

    if (!result.IsSuccessful)
    {
      if (!string.IsNullOrEmpty(result.FailureMessage))
      {
        return StatusCode((int)result.StatusCode, result.FailureMessage);
      }

      return BadRequest(result.FailureMessage);
    }

    var fileView = result.Value;
    
    byte[] bytes;
    if (!fileView.IsBase64Encoded)
    {
      bytes = Encoding.UTF8.GetBytes(fileView.Content);
    }
    else
    {
      bytes = Convert.FromBase64String(fileView.Content);
    }
        
    return File(bytes, fileView.ContentType);
  }

}