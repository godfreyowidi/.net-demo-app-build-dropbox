using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models.Requests;
using DropboxLike.Domain.Services.Token;
using DropboxLike.Domain.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace DropboxLike.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticateController : ControllerBase
{
    private readonly ITokenManager _tokenManager;
    private readonly IUserService _userService;

    public AuthenticateController(ITokenManager tokenManager, IUserService userService)
    {
        _tokenManager = tokenManager;
        _userService = userService;
    }
    
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> Authenticate([FromBody] UserCredentials request)
    {
        var result = await _tokenManager.Authenticate(request.Email, request.Password);
        if (result.IsSuccessful)
        {
            return Ok(new { Token = _tokenManager.NewToken(request.Email, result.Value) });
        }

        if (result.FailureMessage != null) ModelState.AddModelError("Unauthorized", result.FailureMessage);
        return Unauthorized(ModelState);
    }
    
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> RegisterUser([FromBody] UserCredentials request)
    {
        var result = await _userService.RegisterUserAsync(request.Email!, request.Password!);

        if (result.IsSuccessful)
        {
            var userId = result.Value;
            return Ok(new {Id = userId});
        }

        return StatusCode(result.StatusCode, result.FailureMessage);
    }
}