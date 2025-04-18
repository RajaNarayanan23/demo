using Microsoft.EntityFrameworkCore;
using OnlineVotingSystem.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserImage> UserImages { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Election> Elections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly define the primary key for UserImage
        modelBuilder.Entity<UserImage>()
            .HasKey(ui => ui.ImageID);

        // Define foreign key relationship for UserImage (if applicable)
        modelBuilder.Entity<UserImage>()
            .HasOne(ui => ui.User)
            .WithMany() // Adjust if necessary
            .HasForeignKey(ui => ui.UserID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
