using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InTheLoop.Api.Models;

namespace InTheLoop.Api.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FamilyMember>()
            .HasKey(fm => new { fm.UserId, fm.FamilyId });

        builder.Entity<FamilyMember>()
            .HasOne(fm => fm.User)
            .WithMany()
            .HasForeignKey(fm => fm.UserId);

        builder.Entity<FamilyMember>()
            .HasOne(fm => fm.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(fm => fm.FamilyId);

        builder.Entity<Post>()
            .HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId);

        builder.Entity<Post>()
            .HasOne(p => p.Family)
            .WithMany(f => f.Posts)
            .HasForeignKey(p => p.FamilyId);

        builder.Entity<Photo>()
            .HasOne(ph => ph.Post)
            .WithMany(p => p.Photos)
            .HasForeignKey(ph => ph.PostId);

        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId);

        builder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany()
            .HasForeignKey(m => m.RecipientId);

        builder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId);

        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId);

        builder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
