using InvoiceApp.Core;
using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Identity;
using InvoiceApp.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentCompanyProvider? _currentCompany;

    // Resolved lazily on each access, not cached in the constructor: the context can
    // be created before the request is authenticated (e.g. Identity cookie validation
    // resolves it during AuthenticationMiddleware), at which point the CompanyId claim
    // isn't set yet. Reading live ensures SaveChanges stamping and query filters see
    // the signed-in tenant. Null when there is no signed-in tenant (startup migrations,
    // tests) — in that case query filters are bypassed and inserts are left unstamped.
    private int? _companyId => _currentCompany?.CompanyId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentCompanyProvider? currentCompany = null)
        : base(options)
    {
        _currentCompany = currentCompany;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<BankingDetails> BankingDetails => Set<BankingDetails>();
    public DbSet<SavedRate> SavedRates => Set<SavedRate>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RentPayment> RentPayments => Set<RentPayment>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // configures Identity tables

        modelBuilder.HasDefaultSchema("blacktech");

        // One login account belongs to one company.
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .Property(r => r.RentAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Room>()
            .HasOne(r => r.Property)
            .WithMany(p => p.Rooms)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Avoid a second cascade path to Room: Company -> Property -> Room is SET NULL,
        // so Company -> Room must not also cascade. SQL Server rejects multiple cascade paths.
        modelBuilder.Entity<Room>()
            .HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentPayment>(b =>
        {
            b.Property(p => p.AmountDue).HasColumnType("decimal(18,2)");
            b.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
            b.HasOne(p => p.Room)
             .WithMany(r => r.Payments)
             .HasForeignKey(p => p.RoomId)
             .OnDelete(DeleteBehavior.Cascade);
            // Avoid a second cascade path to RentPayment (Company -> Room -> RentPayment
            // already covers cleanup); SQL Server rejects multiple cascade paths.
            b.HasOne(p => p.Company)
             .WithMany()
             .HasForeignKey(p => p.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
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

        // Tenant isolation: every owned entity is filtered to the current company.
        // When _companyId is null (migrations/tests) the filter passes everything.
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<CompanySettings>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<BankingDetails>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<SavedRate>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<Property>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<Room>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<RentPayment>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        modelBuilder.Entity<EmailSettings>().HasQueryFilter(e => !_companyId.HasValue || e.CompanyId == _companyId);
        // LineItem isn't tenant-owned directly; match its parent Invoice's filter so the
        // required Invoice->LineItem relationship stays consistent under filtering.
        modelBuilder.Entity<LineItem>().HasQueryFilter(l => !_companyId.HasValue || l.Invoice.CompanyId == _companyId);
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Assign new tenant-owned rows to the current company. No-op when there is no
    // current company, or when a row already carries an explicit CompanyId.
    private void StampTenant()
    {
        if (!_companyId.HasValue) return;

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CompanyId == 0)
                entry.Entity.CompanyId = _companyId.Value;
        }
    }
}
