using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudBook.API.Data.Database
{
    public class IdentityStore : IdentityDbContext
    {
        public IdentityStore(DbContextOptions<IdentityStore> options) : base(options)
        {
            // Database.EnsureDeleted (); // For testing purposes
            Database.EnsureCreated();
        }
        
        public new DbSet<User> Users { get; set; }
        public DbSet<UserRelation> UserRelations { get; set; }
        public DbSet<PurchaseLog> PurchaseLogs { get; set; }
        // public DbSet<ItemLog> ItemLogs { get; set; }
        // public DbSet<Item> Items { get; set; }
        public DbSet<LogRelation> LogRelations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<NGram> NGrams{ get; set; }
    }
}