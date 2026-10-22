using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Services;

namespace InvoiceApp.Tests.Services;

public class InvoiceEmailComposerTests
{
    [Fact]
    public void Compose_IncludesCompanyContactAndProfessionalAttachmentName()
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

        var email = InvoiceEmailComposer.Compose(invoice, company, company.Name, "mail@gcweproperty.co.za");

        Assert.Equal("Invoice INV-003 from BlackTech Empower", email.Subject);
        Assert.Equal("Invoice-INV-003-BlackTech-Empower.pdf", email.PdfFileName);
        Assert.Contains("Give Compassion NPC", email.HtmlBody);
        Assert.Contains(">BE</td>", email.HtmlBody);
        Assert.DoesNotContain("INV-003", email.HtmlBody);
        Assert.Contains("Please find the invoice attached for your review and payment.", email.HtmlBody);
        Assert.Contains("15 Ebor Ave, Durban, 4000", email.HtmlBody);
        Assert.Contains("0735904108", email.HtmlBody);
        Assert.Contains("mail@gcweproperty.co.za", email.HtmlBody);
        Assert.Contains("Please find the invoice attached for your review and payment.", email.Body);
        Assert.Contains("Kind regards,\nBlackTech Empower", email.Body);
    }

    [Fact]
    public void Compose_HtmlEncodesUserProvidedTextAndSanitizesFilename()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV/004 <draft>",
            InvoiceDate = DateTime.Today,
            ClientName = "A&B <Client>",
            TotalAmount = 10m
        };
        var company = new CompanySettings { Name = "A&B Services" };

        var email = InvoiceEmailComposer.Compose(invoice, company, company.Name, "billing@example.com");

        Assert.Contains("A&amp;B &lt;Client&gt;", email.HtmlBody);
        Assert.DoesNotContain("INV/004 &lt;draft&gt;", email.HtmlBody);
        Assert.Equal("Invoice-INV-004-draft-A-B-Services.pdf", email.PdfFileName);
    }
}
