using Microsoft.EntityFrameworkCore;
using Prepapp.Domain.Questions;
using Prepapp.Domain.Review;

namespace Prepapp.Infrastructure.Data;

public class PrepDbContext(DbContextOptions<PrepDbContext> options) : DbContext(options)
{
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(q =>
        {
            q.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            q.Property(x => x.Topic).HasMaxLength(100);
            q.Property(x => x.Source).HasMaxLength(200);
            q.PrimitiveCollection(x => x.KeyPoints); // JSON column
            q.HasIndex(x => new { x.Category, x.Topic });
            q.HasMany(x => x.ReviewCards)
                .WithOne(c => c.Question)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReviewCard>(c =>
        {
            c.Property(x => x.LastRating).HasConversion<string>().HasMaxLength(8);
            c.HasIndex(x => x.DueAt);
        });
    }
}
