using Microsoft.EntityFrameworkCore;

class WalletDb : DbContext
{
    public WalletDb(DbContextOptions<WalletDb> options) : base(options) { }

    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();
}