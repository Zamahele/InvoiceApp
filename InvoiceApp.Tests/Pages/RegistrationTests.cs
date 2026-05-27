using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceApp.Tests.Pages;

public class RegistrationTests
{
    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Registering_Creates_Company_And_Linked_User()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Mirrors RegisterModel.OnPostAsync: company first, then the account that owns it.
        var company = new Company { Name = "Acme", InvoicingEnabled = true, RentTrackingEnabled = false };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var user = new ApplicationUser { UserName = "owner@acme.test", Email = "owner@acme.test", CompanyId = company.Id };
        var result = await userManager.CreateAsync(user, "Password1");

        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));

        var savedUser = await db.Users.SingleAsync();
        Assert.Equal(company.Id, savedUser.CompanyId);
        Assert.Equal(1, await db.Companies.CountAsync());
        Assert.True(company.InvoicingEnabled);
        Assert.False(company.RentTrackingEnabled);
    }

    [Fact]
    public async Task Password_Is_Hashed_Not_Stored_In_Plaintext()
    {
        using var sp = BuildProvider(Guid.NewGuid().ToString());
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var company = new Company { Name = "Acme", InvoicingEnabled = true };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var user = new ApplicationUser { UserName = "u@a.test", Email = "u@a.test", CompanyId = company.Id };
        await userManager.CreateAsync(user, "Password1");

        var saved = await db.Users.SingleAsync();
        Assert.NotNull(saved.PasswordHash);
        Assert.DoesNotContain("Password1", saved.PasswordHash);
    }
}
