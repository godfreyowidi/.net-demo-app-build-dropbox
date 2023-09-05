using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;

namespace DropboxLike.Domain.Services.User;

public interface IUserService
{
    Task<OperationResult<string>> RegisterUserAsync(string email, string password);
    Task<OperationResult<string>> GetUserIdByEmailAddressAsync(string email);
}