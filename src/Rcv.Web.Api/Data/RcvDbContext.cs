using Microsoft.EntityFrameworkCore;
using Rcv.Web.Api.Data.Entities;

namespace Rcv.Web.Api.Data;

/// <summary>
/// Database context for the Ranked Choice Voting application
/// </summary>
public class RcvDbContext : DbContext
{
    public RcvDbContext(DbContextOptions<RcvDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            // Unique constraint on ExternalId + Provider combination
            entity.HasIndex(u => new { u.ExternalId, u.Provider })
                  .IsUnique()
                  .HasDatabaseName("IX_Users_ExternalId_Provider");

            // Index for faster lookups
            entity.HasIndex(u => new { u.ExternalId, u.Provider })
                  .HasDatabaseName("IX_Users_Lookup");

            // Set default value for CreatedAt
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            entity.HasMany(u => u.CreatedPolls)
                  .WithOne(p => p.Creator)
                  .HasForeignKey(p => p.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Votes)
                  .WithOne(v => v.Voter)
                  .HasForeignKey(v => v.VoterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Poll configuration
        modelBuilder.Entity<Poll>(entity =>
        {
            entity.HasKey(p => p.Id);

            // Indexes for common queries
            entity.HasIndex(p => p.CreatorId)
                  .HasDatabaseName("IX_Polls_CreatorId");

            entity.HasIndex(p => p.Status)
                  .HasDatabaseName("IX_Polls_Status");

            entity.HasIndex(p => p.CreatedAt)
                  .IsDescending()
                  .HasDatabaseName("IX_Polls_CreatedAt");

            // Set default values
            entity.Property(p => p.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.Status)
                  .HasDefaultValue("Active");

            entity.Property(p => p.IsResultsPublic)
                  .HasDefaultValue(true);

            entity.Property(p => p.IsVotingPublic)
                  .HasDefaultValue(false);

            // Relationships
            entity.HasOne(p => p.Creator)
                  .WithMany(u => u.CreatedPolls)
                  .HasForeignKey(p => p.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Options)
                  .WithOne(o => o.Poll)
                  .HasForeignKey(o => o.PollId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Votes)
                  .WithOne(v => v.Poll)
                  .HasForeignKey(v => v.PollId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PollOption configuration
        modelBuilder.Entity<PollOption>(entity =>
        {
            entity.HasKey(o => o.Id);

            // Index for fetching options by poll
            entity.HasIndex(o => o.PollId)
                  .HasDatabaseName("IX_PollOptions_PollId");

            // Set default value
            entity.Property(o => o.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Relationship
            entity.HasOne(o => o.Poll)
                  .WithMany(p => p.Options)
                  .HasForeignKey(o => o.PollId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Vote configuration
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(v => v.Id);

            // Unique constraint: one vote per user per poll
            entity.HasIndex(v => new { v.PollId, v.VoterId })
                  .IsUnique()
                  .HasDatabaseName("UQ_Votes_PollId_VoterId");

            // Index for fetching votes by poll
            entity.HasIndex(v => v.PollId)
                  .HasDatabaseName("IX_Votes_PollId");

            // Index for fetching votes by voter
            entity.HasIndex(v => v.VoterId)
                  .HasDatabaseName("IX_Votes_VoterId");

            // Configure JSON column for RankedChoices
            // EF Core 9.0 with SQL Server automatically maps List<Guid> to native JSON type
            entity.Property(v => v.RankedChoices)
                  .HasColumnType("nvarchar(max)") // SQL Server uses nvarchar(max) for JSON
                  .IsRequired();

            // Set default value
            entity.Property(v => v.CastAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            entity.HasOne(v => v.Poll)
                  .WithMany(p => p.Votes)
                  .HasForeignKey(v => v.PollId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Voter)
                  .WithMany(u => u.Votes)
                  .HasForeignKey(v => v.VoterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
