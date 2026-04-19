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
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RentPayment> RentPayments => Set<RentPayment>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("blacktech");

        modelBuilder.Entity<Room>()
            .Property(r => r.RentAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<RentPayment>(b =>
        {
            b.Property(p => p.AmountDue).HasColumnType("decimal(18,2)");
            b.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
            b.HasOne(p => p.Room)
             .WithMany(r => r.Payments)
             .HasForeignKey(p => p.RoomId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>()
            .HasMany(i => i.LineItems)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.VATRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.VATAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.RetentionPercentage).HasColumnType("decimal(5,2)");
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
