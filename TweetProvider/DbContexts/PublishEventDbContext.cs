using Google.Api;
using Microsoft.EntityFrameworkCore;
using TweetProvider.DTOs;

namespace TweetProvider.DbContexts
{
    public class PublishEventDbContext : DbContext
    {
        public DbSet<PublishEventModel> PublishEventJobs { get; set; }

        public PublishEventDbContext(DbContextOptions<PublishEventDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
