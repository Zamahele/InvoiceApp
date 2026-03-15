using InvoiceApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<BankingDetails> BankingDetails => Set<BankingDetails>();
    public DbSet<SavedRate> SavedRates => Set<SavedRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .HasMany(i => i.LineItems)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.VATAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.RetentionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LineItem>()
            .Property(l => l.Quantity).HasColumnType("decimal(18,4)");
        modelBuilder.Entity<LineItem>()
            .Property(l => l.Rate).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LineItem>()
            .Property(l => l.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SavedRate>()
            .Property(s => s.Rate).HasColumnType("decimal(18,2)");
    }
}
