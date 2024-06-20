using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DropboxLike.Domain.Configuration;
using DropboxLike.Domain.Data;
using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DropboxLike.Domain.Repositories.User;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IAmazonS3 _awsS3Client;
    private readonly string? _bucketName;


    public UserRepository(IOptions<AwsConfiguration> options, ApplicationDbContext applicationDbContext)
    {
        var configuration = options.Value;
        _applicationDbContext = applicationDbContext;
        _bucketName = configuration.BucketName;
        _awsS3Client = new AmazonS3Client(configuration.AwsAccessKey, configuration.AwsSecretAccessKey,
            RegionEndpoint.GetBySystemName(configuration.Region));
    }

    public async Task<OperationResult<string>> RegisterUserAsync(string email, string password)
    {
        try
        {
            if (_applicationDbContext.AppUsers != null)
            {
                var existingUser = await _applicationDbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    return OperationResult<string>.Fail("User already exist");
                }
            }

            var hashedPassword = HashPassword(password);

            var newUser = new UserEntity
            {
                Email = email,
                Password = hashedPassword,
            };

            _applicationDbContext.AppUsers?.Add(newUser);
            await _applicationDbContext.SaveChangesAsync();

            try
            {
                await CreateS3BucketFolder(newUser.Id!.ToString());
                
                return OperationResult<string>.Success(newUser.Id!.ToString(), HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _applicationDbContext.AppUsers?.Remove(newUser);
                await _applicationDbContext.SaveChangesAsync();
            }

            return OperationResult<string>.Success(newUser.Id!.ToString(), HttpStatusCode.Created);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Fail(ex.Message, HttpStatusCode.InternalServerError);
        }

    }

    public async Task<List<string>> GetUserIdByEmailAddressAsync(List<string> emails)
    {
        var validUserIds = new List<string>();
        var invalidEmails = new List<string>();
        var unregisteredEmails = new List<string>();

        foreach (var email in emails)
        {
            if (!IsEmailValid(email))
            {
                invalidEmails.Add(email);
                continue;
            }

            var user = await _applicationDbContext.AppUsers!.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                validUserIds.Add((user.Id!));
            }
            else
            {
                unregisteredEmails.Add(email);
            }

        }
        
        List<string> errorMessages = new List<string>();

        if (invalidEmails.Any() && unregisteredEmails.Any())
        {
            errorMessages.Add( $"{string.Join(", ", invalidEmails)} not a valid email, " +
                   $"{string.Join(", ", unregisteredEmails)} not a registered user");
        }
        
        if (invalidEmails.Any())
        {
            errorMessages.Add( $"{string.Join(", ", invalidEmails)} not a valid email.");
        }
        
        if (unregisteredEmails.Any())
        {
            errorMessages.Add($"{string.Join(", ", unregisteredEmails)} not a registered user");
        }

        if (errorMessages.Any())
        {
            return errorMessages;
        }

        return validUserIds;
    }

    private bool IsEmailValid(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        bool isValid = Regex.IsMatch(email, pattern);

        return isValid;
    }
    
    private async Task CreateS3BucketFolder(string userId)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"user_{userId}/",
            ContentBody = string.Empty,
            CannedACL = S3CannedACL.NoACL
        };
        await _awsS3Client.PutObjectAsync(request);
    }
    private static string HashPassword(string password)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return hashedPassword;
        }
    }
}