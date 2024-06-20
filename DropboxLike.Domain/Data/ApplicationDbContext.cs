using DropboxLike.Domain.Data.Entities;
using DropboxLike.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DropboxLike.Domain.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<FileEntity>? FileModels { get; set; }
    public DbSet<UserEntity>? AppUsers { get; set; }
    public DbSet<FileShareEntity>? SharedFiles { get; set; }
    public DbSet<FolderEntity>? Folders { get; set; }
    public DbSet<FolderShareEntity>? SharedFolders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Existing configurations
        ConfigureUserEntity(modelBuilder);
        ConfigureShareEntity(modelBuilder);
        ConfigureFileEntity(modelBuilder);
        ConfigureFolderShareEntity(modelBuilder);

        // New folder configurations
        ConfigureFolderEntity(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }

    private static void ConfigureShareEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileShareEntity>()
            .HasKey(share => new { share.UserId, share.FileId });

        modelBuilder.Entity<FileShareEntity>()
            .HasOne(share => share.User)
            .WithMany(user => user.FileShares)
            .HasForeignKey(share => share.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileShareEntity>()
            .HasOne(share => share.File)
            .WithMany(file => file.SharedWithUsers)
            .HasForeignKey(share => share.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFolderShareEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FolderShareEntity>()
            .HasKey(share => new { share.UserId, share.FolderId });

        modelBuilder.Entity<FolderShareEntity>()
            .HasOne(share => share.User)
            .WithMany(user => user.FolderShares)
            .HasForeignKey(share => share.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FolderShareEntity>()
            .HasOne(share => share.Folder)
            .WithMany(folder => folder.SharedWithUsers)
            .HasForeignKey(share => share.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureFileEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>()
            .HasIndex(f => new { f.FileName })
            .IsUnique();

        modelBuilder.Entity<FileEntity>()
            .HasOne(file => file.User)
            .WithMany(user => user.Files)
            .HasForeignKey(file => file.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileEntity>()
            .HasOne(file => file.Folder)
            .WithMany(folder => folder.Files)
            .HasForeignKey(file => file.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureFolderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FolderEntity>()
            .HasOne(folder => folder.User)
            .WithMany(user => user.Folders)
            .HasForeignKey(folder => folder.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A parent-child relationship within folders
        modelBuilder.Entity<FolderEntity>()
            .HasOne(folder => folder.ParentFolder)
            .WithMany(parent => parent.ChildFolders)
            .HasForeignKey(folder => folder.ParentFolderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}