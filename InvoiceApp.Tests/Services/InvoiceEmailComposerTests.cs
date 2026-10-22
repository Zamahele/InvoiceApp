using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Services;

namespace InvoiceApp.Tests.Services;

public class InvoiceEmailComposerTests
{
    [Fact]
    public void Compose_CreatesPlainTextEmailWithCompanyPhoneAndProfessionalAttachmentName()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-003",
            InvoiceDate = new DateTime(2026, 7, 1),
            ClientName = "Give Compassion NPC",
            TotalAmount = 390m
        };
        var company = new CompanySettings
        {
            Name = "BlackTech Empower",
            AddressLine1 = "15 Ebor Ave",
            City = "Durban",
            PostalCode = "4000",
            Phone = "0735904108"
        };

        var email = InvoiceEmailComposer.Compose(invoice, company, company.Name);

        Assert.Equal("Invoice INV-003 from BlackTech Empower", email.Subject);
        Assert.Equal("Invoice-INV-003-BlackTech-Empower.pdf", email.PdfFileName);
        Assert.DoesNotContain("INV-003", email.Body);
        Assert.DoesNotContain("<html", email.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Please find the invoice attached for your review and payment.", email.Body);
        Assert.EndsWith("Kind regards,\nBlackTech Empower\n0735904108", email.Body);
    }

    [Fact]
    public void Compose_PlainTextUsesClientNameAndSanitizesFilename()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV/004 <draft>",
            InvoiceDate = DateTime.Today,
            ClientName = "A&B <Client>",
            TotalAmount = 10m
        };
        var company = new CompanySettings { Name = "A&B Services" };

        var email = InvoiceEmailComposer.Compose(invoice, company, company.Name);

        Assert.Contains("Good day A&B <Client>", email.Body);
        Assert.Equal("Invoice-INV-004-draft-A-B-Services.pdf", email.PdfFileName);
    }
}
