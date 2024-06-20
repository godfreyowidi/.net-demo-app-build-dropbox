using System.Security.Claims;
using System.Text;
using DropboxLike.Domain.Configuration;
using DropboxLike.Domain.Data;
using DropboxLike.Domain.Repositories.File;
using DropboxLike.Domain.Repositories.Share;
using DropboxLike.Domain.Repositories.User;
using DropboxLike.Domain.Services.File;
using DropboxLike.Domain.Services.Share;
using DropboxLike.Domain.Services.Token;
using DropboxLike.Domain.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Add configuration.
builder.Services.Configure<AwsConfiguration>(options =>
{
    options.BucketName = builder.Configuration.GetSection("Aws:BucketName").Get<string>() ?? string.Empty;
    options.AwsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY") ?? string.Empty;
    options.AwsSecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? string.Empty;
    options.Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? string.Empty;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptionsAction: sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    }));

builder.Services.AddSignalRCore();

// 2. Add lowest layer components, namely repositories.
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFileShareRepository, FileShareRepository>();

// 3. Add higher layer components, namely services.
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IShareFileService, ShareFileService>();

// 4. Add even higher layer components, namely controllers and the related authorization and authentication.
builder.Services.AddScoped<ITokenManager, TokenManager>();

// 5. Add authentication as JWT validation logic.
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(Convert.ToBase64String(Encoding.UTF8.GetBytes("rekfjdhabdjekkrnabrisnakelsntjsn")))),
        ValidateLifetime = true,
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidIssuer = "localhost",
        ValidAudience = "DropboxLike",
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bearer", policy =>
    {
        policy.RequireClaim(ClaimTypes.Email);
        policy.RequireClaim(ClaimTypes.NameIdentifier);
    });
    options.DefaultPolicy = options.GetPolicy("Bearer")!;
});

// 5. Add controllers.
builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();