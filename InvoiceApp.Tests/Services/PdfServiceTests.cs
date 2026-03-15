using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace InvoiceApp.Tests.Services;

public class PdfServiceTests
{
    public PdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private Invoice MakeInvoice() => new()
    {
        InvoiceNumber = "INV-001",
        InvoiceDate = DateTime.Today,
        ClientName = "Test Client",
        ClientAddressLine1 = "10 Test Street",
        ClientCity = "Testville",
        ClientPostalCode = "1234",
        SubTotal = 63_743.60m,
        TotalAmount = 63_743.60m,
        LineItems = new List<LineItem>
        {
            new() { Description = "Paving", Unit = "m2", Quantity = 519.53m, Rate = 120m, Amount = 62_343.60m, SortOrder = 0 },
            new() { Description = "Caps & Concrete", Unit = "m", Quantity = 20m, Rate = 70m, Amount = 1_400m, SortOrder = 1 }
        }
    };

    [Fact]
    public void Generate_Returns_NonEmpty_Bytes()
    {
        var service = new InvoicePdfService();
        var pdf = service.Generate(MakeInvoice(), null, null);
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public void Generate_Returns_Valid_PDF_Header()
    {
        var service = new InvoicePdfService();
        var pdf = service.Generate(MakeInvoice(), null, null);
        // PDF files start with %PDF
        var header = System.Text.Encoding.ASCII.GetString(pdf, 0, 4);
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public void Generate_With_Company_Details_Does_Not_Throw()
    {
        var company = new CompanySettings
        {
            Id = 1, Name = "Lifestar Builders (PTY) LTD",
            AddressLine1 = "Embahnlonini Old Main Road",
            AddressLine2 = "Emosadale, Estcourt 3310",
            Phone = "066 378 7872"
        };
        var banking = new BankingDetails
        {
            Id = 1, BankName = "Capitec Bank",
            AccountType = "Savings", AccountNumber = "1762812922"
        };
        var service = new InvoicePdfService();
        var exception = Record.Exception(() => service.Generate(MakeInvoice(), company, banking));
        Assert.Null(exception);
    }

    [Fact]
    public void Generate_With_VAT_Does_Not_Throw()
    {
        var invoice = MakeInvoice();
        invoice.VATEnabled = true;
        invoice.VATRate = 15;
        invoice.VATAmount = Math.Round(invoice.SubTotal * 15 / 100, 2);
        invoice.TotalAmount = invoice.SubTotal + invoice.VATAmount;

        var service = new InvoicePdfService();
        var exception = Record.Exception(() => service.Generate(invoice, null, null));
        Assert.Null(exception);
    }

    [Fact]
    public void Generate_With_Retention_Does_Not_Throw()
    {
        var invoice = MakeInvoice();
        invoice.RetentionEnabled = true;
        invoice.RetentionType = "Deposit";
        invoice.RetentionPercentage = 30;
        invoice.RetentionAmount = Math.Round(invoice.SubTotal * 30 / 100, 2);
        invoice.TotalAmount = invoice.SubTotal - invoice.RetentionAmount;

        var service = new InvoicePdfService();
        var exception = Record.Exception(() => service.Generate(invoice, null, null));
        Assert.Null(exception);
    }
}
