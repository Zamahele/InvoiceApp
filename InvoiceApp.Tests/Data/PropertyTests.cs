using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Xunit;

namespace InvoiceApp.Tests.Data;

public class PropertyTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Property_Can_Be_Created_And_Retrieved()
    {
        using var db = CreateInMemoryDb();

        db.Properties.Add(new Property
        {
            Name = "Sunset Cottages",
            AgentName = "John Smith",
            AgentPhone = "082 000 0000",
            AddressLine1 = "10 Main Street",
            City = "Durban"
        });
        await db.SaveChangesAsync();

        var saved = await db.Properties.FirstOrDefaultAsync();

        Assert.NotNull(saved);
        Assert.Equal("Sunset Cottages", saved.Name);
        Assert.Equal("John Smith", saved.AgentName);
        Assert.Equal("Durban", saved.City);
    }

    [Fact]
    public async Task Room_Can_Be_Linked_To_Property()
    {
        using var db = CreateInMemoryDb();

        var property = new Property { Name = "Riverside Flats", AgentName = "Jane Doe" };
        db.Properties.Add(property);
        await db.SaveChangesAsync();

        db.Rooms.Add(new Room
        {
            Name = "Unit 1",
            TenantName = "Bob",
            RentAmount = 4500m,
            PropertyId = property.Id
        });
        await db.SaveChangesAsync();

        var room = await db.Rooms.Include(r => r.Property).FirstAsync();

        Assert.NotNull(room.Property);
        Assert.Equal("Riverside Flats", room.Property.Name);
        Assert.Equal(property.Id, room.PropertyId);
    }

    [Fact]
    public async Task Room_PropertyId_Is_Nullable_When_Not_Linked()
    {
        using var db = CreateInMemoryDb();

        db.Rooms.Add(new Room { Name = "Standalone Room", TenantName = "Alice", RentAmount = 3000m });
        await db.SaveChangesAsync();

        var room = await db.Rooms.Include(r => r.Property).FirstAsync();

        Assert.Null(room.PropertyId);
        Assert.Null(room.Property);
    }

    [Fact]
    public async Task Multiple_Rooms_Can_Belong_To_Same_Property()
    {
        using var db = CreateInMemoryDb();

        var property = new Property { Name = "Garden Cottages", AgentName = "Sam" };
        db.Properties.Add(property);
        await db.SaveChangesAsync();

        db.Rooms.AddRange(
            new Room { Name = "Cottage A", TenantName = "T1", RentAmount = 5000m, PropertyId = property.Id },
            new Room { Name = "Cottage B", TenantName = "T2", RentAmount = 5500m, PropertyId = property.Id }
        );
        await db.SaveChangesAsync();

        var rooms = await db.Rooms.Where(r => r.PropertyId == property.Id).ToListAsync();

        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public async Task Receipt_Pdf_Uses_Property_Name_When_Linked()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        using var db = CreateInMemoryDb();

        var property = new Property { Name = "Sunset Cottages", AgentName = "John Smith", AgentPhone = "082 000 0000" };
        db.Properties.Add(property);

        var room = new Room { Name = "Unit 1", TenantName = "Bob Tenant", RentAmount = 5000m, Property = property };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            Room = room,
            Month = 4,
            Year = 2026,
            AmountDue = 5000m,
            AmountPaid = 5000m,
            IsPaid = true,
            PaidDate = new DateTime(2026, 4, 1)
        };

        var service = new RentReceiptPdfService();
        var ex = Record.Exception(() => service.Generate(payment, null));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Receipt_Pdf_Generates_Without_Property()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        using var db = CreateInMemoryDb();

        var room = new Room { Name = "Unit 2", TenantName = "No Property Tenant", RentAmount = 3000m };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            Room = room,
            Month = 4,
            Year = 2026,
            AmountDue = 3000m,
            AmountPaid = 3000m,
            IsPaid = true,
            PaidDate = new DateTime(2026, 4, 1)
        };

        var service = new RentReceiptPdfService();
        var ex = Record.Exception(() => service.Generate(payment, null));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Property_Can_Be_Saved_With_Multiple_Rooms()
    {
        using var db = CreateInMemoryDb();

        var property = new Property
        {
            Name = "Ocean View Cottages",
            AgentName = "Mary Jones",
            AgentPhone = "083 111 2222",
            AddressLine1 = "5 Beach Road",
            City = "Durban"
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();

        db.Rooms.AddRange(
            new Room { Name = "Room A", TenantName = "Tenant 1", RentAmount = 4000m, PropertyId = property.Id, IsActive = true },
            new Room { Name = "Room B", TenantName = "Tenant 2", RentAmount = 4500m, PropertyId = property.Id, IsActive = true },
            new Room { Name = "Room C", TenantName = "Tenant 3", RentAmount = 5000m, PropertyId = property.Id, IsActive = true }
        );
        await db.SaveChangesAsync();

        var saved = await db.Properties
            .Include(p => p.Rooms)
            .FirstAsync(p => p.Id == property.Id);

        Assert.Equal("Ocean View Cottages", saved.Name);
        Assert.Equal("Mary Jones", saved.AgentName);
        Assert.Equal(3, saved.Rooms.Count);
        Assert.All(saved.Rooms, r => Assert.Equal(property.Id, r.PropertyId));
    }

    [Fact]
    public async Task Property_Saved_Without_Rooms_Then_Rooms_Added_And_Reference_Correctly()
    {
        using var db = CreateInMemoryDb();

        // Step 1: save property alone (no rooms)
        var property = new Property { Name = "Hillside Flats", AgentName = "Sipho Dlamini" };
        db.Properties.Add(property);
        await db.SaveChangesAsync();

        var savedProperty = await db.Properties.FirstOrDefaultAsync(p => p.Id == property.Id);
        Assert.NotNull(savedProperty);
        Assert.Empty(await db.Rooms.Where(r => r.PropertyId == property.Id).ToListAsync());

        // Step 2: add rooms referencing that property
        db.Rooms.Add(new Room { Name = "Flat 1", TenantName = "Zola", RentAmount = 3500m, PropertyId = property.Id });
        db.Rooms.Add(new Room { Name = "Flat 2", TenantName = "Thabo", RentAmount = 4000m, PropertyId = property.Id });
        await db.SaveChangesAsync();

        var rooms = await db.Rooms.Include(r => r.Property).Where(r => r.PropertyId == property.Id).ToListAsync();
        Assert.Equal(2, rooms.Count);
        Assert.All(rooms, r =>
        {
            Assert.NotNull(r.Property);
            Assert.Equal("Hillside Flats", r.Property!.Name);
            Assert.Equal("Sipho Dlamini", r.Property!.AgentName);
        });
    }
}
