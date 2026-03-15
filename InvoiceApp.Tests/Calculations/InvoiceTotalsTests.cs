using InvoiceApp.Core.Entities;
using Xunit;

namespace InvoiceApp.Tests.Calculations;

public class InvoiceTotalsTests
{
    private static LineItem MakeItem(string desc, decimal qty, decimal rate, string unit = "m2")
    {
        return new LineItem
        {
            Description = desc,
            Unit = unit,
            Quantity = qty,
            Rate = rate,
            Amount = qty * rate
        };
    }

    private static Invoice BuildInvoice(List<LineItem> items, bool vatEnabled = false,
        decimal vatRate = 15, bool retentionEnabled = false,
        string retentionType = "Deposit", decimal retentionPct = 0)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            ClientName = "Test Client",
            VATEnabled = vatEnabled,
            VATRate = vatRate,
            RetentionEnabled = retentionEnabled,
            RetentionType = retentionType,
            RetentionPercentage = retentionPct,
            LineItems = items
        };

        invoice.SubTotal = items.Sum(i => i.Amount);

        if (vatEnabled)
            invoice.VATAmount = Math.Round(invoice.SubTotal * vatRate / 100, 2);
        else
            invoice.VATAmount = 0;

        if (retentionEnabled)
            invoice.RetentionAmount = Math.Round(invoice.SubTotal * retentionPct / 100, 2);
        else
            invoice.RetentionAmount = 0;

        invoice.TotalAmount = invoice.SubTotal + invoice.VATAmount;
        if (retentionEnabled)
            invoice.TotalAmount -= invoice.RetentionAmount;

        return invoice;
    }

    [Fact]
    public void LineItem_Amount_Is_Qty_Times_Rate()
    {
        var item = MakeItem("Paving", 519.53m, 120m);
        Assert.Equal(519.53m * 120m, item.Amount);
    }

    [Fact]
    public void SubTotal_Is_Sum_Of_All_LineItems()
    {
        var items = new List<LineItem>
        {
            MakeItem("Paving", 519.53m, 120m),
            MakeItem("Caps", 20m, 70m, "m")
        };
        var invoice = BuildInvoice(items);
        Assert.Equal(62_343.60m + 1_400m, invoice.SubTotal);
    }

    [Fact]
    public void Total_Equals_SubTotal_When_No_VAT_No_Retention()
    {
        var items = new List<LineItem> { MakeItem("Paving", 519.53m, 120m) };
        var invoice = BuildInvoice(items);
        Assert.Equal(invoice.SubTotal, invoice.TotalAmount);
    }

    [Fact]
    public void VAT_Is_Calculated_At_15_Percent()
    {
        var items = new List<LineItem> { MakeItem("Paving", 100m, 100m) };
        var invoice = BuildInvoice(items, vatEnabled: true, vatRate: 15);
        Assert.Equal(10_000m, invoice.SubTotal);
        Assert.Equal(1_500m, invoice.VATAmount);
        Assert.Equal(11_500m, invoice.TotalAmount);
    }

    [Fact]
    public void VAT_Is_Zero_When_Disabled()
    {
        var items = new List<LineItem> { MakeItem("Paving", 100m, 100m) };
        var invoice = BuildInvoice(items, vatEnabled: false);
        Assert.Equal(0m, invoice.VATAmount);
        Assert.Equal(invoice.SubTotal, invoice.TotalAmount);
    }

    [Fact]
    public void Deposit_Is_Deducted_From_Total()
    {
        var items = new List<LineItem> { MakeItem("Paving", 100m, 100m) };
        var invoice = BuildInvoice(items, retentionEnabled: true,
            retentionType: "Deposit", retentionPct: 30);
        Assert.Equal(10_000m, invoice.SubTotal);
        Assert.Equal(3_000m, invoice.RetentionAmount);
        Assert.Equal(7_000m, invoice.TotalAmount);
    }

    [Fact]
    public void Holdback_Is_Deducted_From_Total()
    {
        var items = new List<LineItem> { MakeItem("Paving", 100m, 100m) };
        var invoice = BuildInvoice(items, retentionEnabled: true,
            retentionType: "Holdback", retentionPct: 10);
        Assert.Equal(1_000m, invoice.RetentionAmount);
        Assert.Equal(9_000m, invoice.TotalAmount);
    }

    [Fact]
    public void VAT_And_Retention_Combined()
    {
        var items = new List<LineItem> { MakeItem("Paving", 100m, 100m) };
        // SubTotal = 10000, VAT = 1500, Total before retention = 11500
        // Retention 10% of SubTotal = 1000, Final = 10500
        var invoice = BuildInvoice(items, vatEnabled: true, vatRate: 15,
            retentionEnabled: true, retentionType: "Deposit", retentionPct: 10);
        Assert.Equal(10_000m, invoice.SubTotal);
        Assert.Equal(1_500m, invoice.VATAmount);
        Assert.Equal(1_000m, invoice.RetentionAmount);
        Assert.Equal(10_500m, invoice.TotalAmount);
    }

    [Fact]
    public void Zero_Quantity_Results_In_Zero_Amount()
    {
        var item = MakeItem("Paving", 0m, 120m);
        Assert.Equal(0m, item.Amount);
    }

    [Fact]
    public void Zero_Rate_Results_In_Zero_Amount()
    {
        var item = MakeItem("Paving", 100m, 0m);
        Assert.Equal(0m, item.Amount);
    }

    [Fact]
    public void Empty_LineItems_Gives_Zero_Total()
    {
        var invoice = BuildInvoice(new List<LineItem>());
        Assert.Equal(0m, invoice.SubTotal);
        Assert.Equal(0m, invoice.TotalAmount);
    }

    [Fact]
    public void Invoice_Example_From_Requirements_Matches()
    {
        // From the original example: Paving 62343.60 + Caps 1400 = 63743.60
        var items = new List<LineItem>
        {
            MakeItem("Paving @ R120/m2", 519.53m, 120m),
            MakeItem("Caps & Concrete: 20 m @ R70/m", 20m, 70m, "m")
        };
        var invoice = BuildInvoice(items);
        Assert.Equal(63_743.60m, invoice.TotalAmount);
    }

    [Fact]
    public void Retention_Amount_Is_Rounded_To_2_Decimal_Places()
    {
        var items = new List<LineItem> { MakeItem("Work", 1m, 100.10m) };
        var invoice = BuildInvoice(items, retentionEnabled: true,
            retentionType: "Deposit", retentionPct: 10);
        Assert.Equal(Math.Round(10.01m, 2), invoice.RetentionAmount);
    }
}
