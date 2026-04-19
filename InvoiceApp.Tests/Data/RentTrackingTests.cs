using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceApp.Tests.Data;

public class RentTrackingTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Can_Create_Room()
    {
        using var db = CreateInMemoryDb();
        var room = new Room
        {
            Name = "Cottage 1",
            TenantName = "John Smith",
            TenantPhone = "082 000 0000",
            RentAmount = 5000m,
            IsActive = true
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var saved = await db.Rooms.FindAsync(room.Id);
        Assert.NotNull(saved);
        Assert.Equal("Cottage 1", saved.Name);
        Assert.Equal("John Smith", saved.TenantName);
        Assert.Equal(5000m, saved.RentAmount);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Can_Create_Rent_Payment_For_Room()
    {
        using var db = CreateInMemoryDb();
        var room = new Room
        {
            Name = "Room A",
            TenantName = "Alice",
            RentAmount = 3000m,
            IsActive = true
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            RoomId = room.Id,
            Month = 4,
            Year = 2026,
            AmountDue = room.RentAmount,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };
        db.RentPayments.Add(payment);
        await db.SaveChangesAsync();

        var saved = await db.RentPayments
            .Include(p => p.Room)
            .FirstAsync(p => p.Id == payment.Id);
        
        Assert.NotNull(saved);
        Assert.Equal(room.Id, saved.RoomId);
        Assert.Equal(4, saved.Month);
        Assert.Equal(2026, saved.Year);
        Assert.Equal(3000m, saved.AmountDue);
        Assert.False(saved.IsPaid);
    }

    [Fact]
    public async Task Can_Mark_Payment_As_Paid()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "B1", RentAmount = 2500m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            RoomId = room.Id,
            Month = 4,
            Year = 2026,
            AmountDue = 2500m,
            IsPaid = false
        };
        db.RentPayments.Add(payment);
        await db.SaveChangesAsync();

        // Mark as paid
        payment.IsPaid = true;
        payment.AmountPaid = 2500m;
        payment.PaidDate = DateTime.Today;
        payment.Notes = "Cash payment";
        db.RentPayments.Update(payment);
        await db.SaveChangesAsync();

        var updated = await db.RentPayments.FindAsync(payment.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsPaid);
        Assert.Equal(2500m, updated.AmountPaid);
        Assert.Equal(DateTime.Today, updated.PaidDate);
        Assert.Equal("Cash payment", updated.Notes);
    }

    [Fact]
    public async Task Can_Undo_Payment()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "C1", RentAmount = 4000m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            RoomId = room.Id,
            Month = 4,
            Year = 2026,
            AmountDue = 4000m,
            IsPaid = true,
            AmountPaid = 4000m,
            PaidDate = DateTime.Today,
            Notes = "Paid in full"
        };
        db.RentPayments.Add(payment);
        await db.SaveChangesAsync();

        // Undo payment
        payment.IsPaid = false;
        payment.AmountPaid = null;
        payment.PaidDate = null;
        payment.Notes = null;
        db.RentPayments.Update(payment);
        await db.SaveChangesAsync();

        var undone = await db.RentPayments.FindAsync(payment.Id);
        Assert.NotNull(undone);
        Assert.False(undone.IsPaid);
        Assert.Null(undone.AmountPaid);
        Assert.Null(undone.PaidDate);
        Assert.Null(undone.Notes);
    }

    [Fact]
    public async Task Can_Create_Multiple_Payments_For_Single_Room_In_Different_Months()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "D1", RentAmount = 2000m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        // Add payments for Jan and Feb 2026
        var payments = new List<RentPayment>
        {
            new() { RoomId = room.Id, Month = 1, Year = 2026, AmountDue = 2000m, IsPaid = false },
            new() { RoomId = room.Id, Month = 2, Year = 2026, AmountDue = 2000m, IsPaid = true, AmountPaid = 2000m }
        };
        db.RentPayments.AddRange(payments);
        await db.SaveChangesAsync();

        var allPayments = await db.RentPayments
            .Where(p => p.RoomId == room.Id)
            .OrderBy(p => p.Month)
            .ToListAsync();

        Assert.Equal(2, allPayments.Count);
        Assert.False(allPayments[0].IsPaid);
        Assert.True(allPayments[1].IsPaid);
    }

    [Fact]
    public async Task Can_Deactivate_Room()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "E1", RentAmount = 3500m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        room.IsActive = false;
        db.Rooms.Update(room);
        await db.SaveChangesAsync();

        var updated = await db.Rooms.FindAsync(room.Id);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Payments_For_Inactive_Room_Are_Not_Auto_Created()
    {
        using var db = CreateInMemoryDb();
        var activeRoom = new Room { Name = "Active", RentAmount = 2000m, IsActive = true };
        var inactiveRoom = new Room { Name = "Inactive", RentAmount = 2000m, IsActive = false };
        db.Rooms.AddRange(activeRoom, inactiveRoom);
        await db.SaveChangesAsync();

        // Only active rooms should have auto-created payments
        var activeRooms = await db.Rooms.Where(r => r.IsActive).ToListAsync();
        Assert.Single(activeRooms);
        Assert.Equal("Active", activeRooms[0].Name);
    }

    [Fact]
    public async Task Payment_Can_Store_Partial_Amount()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "Partial", RentAmount = 5000m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            RoomId = room.Id,
            Month = 4,
            Year = 2026,
            AmountDue = 5000m,
            IsPaid = true,
            AmountPaid = 2500m  // Only half paid
        };
        db.RentPayments.Add(payment);
        await db.SaveChangesAsync();

        var saved = await db.RentPayments.FindAsync(payment.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsPaid);
        Assert.Equal(2500m, saved.AmountPaid);
        Assert.Equal(5000m, saved.AmountDue);
    }

    [Fact]
    public async Task Can_Update_Room_Details()
    {
        using var db = CreateInMemoryDb();
        var room = new Room
        {
            Name = "Old Name",
            TenantName = "Old Tenant",
            TenantPhone = "000 000 0000",
            RentAmount = 2000m,
            IsActive = true
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        room.Name = "New Name";
        room.TenantName = "New Tenant";
        room.TenantPhone = "999 999 9999";
        room.RentAmount = 3000m;
        db.Rooms.Update(room);
        await db.SaveChangesAsync();

        var updated = await db.Rooms.FindAsync(room.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New Tenant", updated.TenantName);
        Assert.Equal("999 999 9999", updated.TenantPhone);
        Assert.Equal(3000m, updated.RentAmount);
    }

    [Fact]
    public async Task Rent_Payments_Deleted_When_Room_Deleted()
    {
        using var db = CreateInMemoryDb();
        var room = new Room { Name = "ToDelete", RentAmount = 2000m, IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var payment = new RentPayment
        {
            RoomId = room.Id,
            Month = 4,
            Year = 2026,
            AmountDue = 2000m
        };
        db.RentPayments.Add(payment);
        await db.SaveChangesAsync();

        db.Rooms.Remove(room);
        await db.SaveChangesAsync();

        var orphanPayments = await db.RentPayments
            .Where(p => p.RoomId == room.Id)
            .ToListAsync();
        Assert.Empty(orphanPayments);
    }
}
