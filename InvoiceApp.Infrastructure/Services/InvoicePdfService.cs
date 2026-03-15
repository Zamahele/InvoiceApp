using InvoiceApp.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceApp.Infrastructure.Services;

public class InvoicePdfService
{
    // Colours matching the HTML preview
    private const string Navy  = "#1a1a2e";
    private const string Gold  = "#e8c84a";
    private const string Light = "#f8f9fa";
    private const string Muted = "#888888";
    private const string Body  = "#333333";

    public byte[] Generate(Invoice invoice, CompanySettings? company, BankingDetails? banking)
    {
        var companyName = company?.Name ?? string.Empty;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(Body));

                // Stamp - same SVG as HTML preview, bottom right corner
                var stampSvg = BuildStampSvg(companyName, invoice.InvoiceNumber);
                var stampImage = SvgImage.FromText(stampSvg);

                page.Foreground()
                    .AlignBottom().AlignRight()
                    .PaddingRight(28).PaddingBottom(58)
                    .Rotate(-12)
                    .Width(130).Height(130)
                    .Svg(stampImage);

                page.Content().Column(col =>
                {
                    // ── HEADER BAR ─────────────────────────────────────────
                    col.Item().Background(Navy).Padding(20).Row(row =>
                    {
                        // Left: company name + phone
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(companyName)
                                .FontSize(16).Bold().FontColor("#ffffff");

                            if (!string.IsNullOrEmpty(company?.Phone))
                                left.Item().PaddingTop(4)
                                    .Text(company.Phone)
                                    .FontSize(8).FontColor("#cccccc");
                        });

                        // Right: INVOICE label + meta
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().AlignRight()
                                .Text("INVOICE")
                                .FontSize(26).Bold().FontColor(Gold)
                                .LetterSpacing(0.15f);

                            right.Item().AlignRight().PaddingTop(4).Text(txt =>
                            {
                                txt.Span($"No: ").FontColor("#cccccc").FontSize(8);
                                txt.Span(invoice.InvoiceNumber).FontColor("#ffffff").Bold().FontSize(8);
                            });

                            right.Item().AlignRight().Text(txt =>
                            {
                                txt.Span("Date: ").FontColor("#cccccc").FontSize(8);
                                txt.Span(invoice.InvoiceDate.ToString("dd MMM yyyy"))
                                   .FontColor("#ffffff").FontSize(8);
                            });

                            if (invoice.IsReissue && invoice.ReissueDate.HasValue)
                                right.Item().AlignRight().Text(txt =>
                                {
                                    txt.Span("Re-issued: ").FontColor("#cccccc").FontSize(8);
                                    txt.Span(invoice.ReissueDate.Value.ToString("dd MMM yyyy"))
                                       .FontColor("#ffffff").FontSize(8);
                                });
                        });
                    });

                    // ── GOLD ACCENT BAR ────────────────────────────────────
                    col.Item().Height(5).Background(Gold);

                    // ── ADDRESS ROW ────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // LEFT: From (white + gold left border)
                        row.RelativeItem()
                            .BorderLeft(5).BorderColor(Gold)
                            .Background("#ffffff")
                            .Padding(16).Column(from =>
                        {
                            from.Item().Text("FROM")
                                .FontSize(7).Bold().FontColor(Gold)
                                .LetterSpacing(0.15f);

                            from.Item().PaddingTop(5)
                                .Text(companyName)
                                .FontSize(10).Bold().FontColor(Navy);

                            if (!string.IsNullOrEmpty(company?.AddressLine1))
                                from.Item().Text(company.AddressLine1)
                                    .FontSize(8.5f).FontColor("#555555");

                            if (!string.IsNullOrEmpty(company?.AddressLine2))
                                from.Item().Text(company.AddressLine2)
                                    .FontSize(8.5f).FontColor("#555555");

                            if (!string.IsNullOrEmpty(company?.City))
                                from.Item().Text($"{company.City} {company.PostalCode}")
                                    .FontSize(8.5f).FontColor("#555555");

                            if (!string.IsNullOrEmpty(company?.Phone))
                                from.Item().PaddingTop(4)
                                    .Text(company.Phone)
                                    .FontSize(8).FontColor("#777777");
                        });

                        // RIGHT: Bill To (dark navy background)
                        row.RelativeItem()
                            .Background(Navy)
                            .Padding(16).Column(to =>
                        {
                            to.Item().Text("BILL TO")
                                .FontSize(7).Bold().FontColor(Gold)
                                .LetterSpacing(0.15f);

                            to.Item().PaddingTop(5)
                                .Text(invoice.ClientName)
                                .FontSize(10).Bold().FontColor("#ffffff");

                            if (!string.IsNullOrEmpty(invoice.ClientAddressLine1))
                                to.Item().Text(invoice.ClientAddressLine1)
                                    .FontSize(8.5f).FontColor("#b0b8cc");

                            if (!string.IsNullOrEmpty(invoice.ClientAddressLine2))
                                to.Item().Text(invoice.ClientAddressLine2)
                                    .FontSize(8.5f).FontColor("#b0b8cc");

                            if (!string.IsNullOrEmpty(invoice.ClientCity))
                                to.Item().Text($"{invoice.ClientCity}, {invoice.ClientPostalCode}")
                                    .FontSize(8.5f).FontColor("#b0b8cc");
                        });
                    });

                    // ── SEPARATOR ──────────────────────────────────────────
                    col.Item().Height(2).Background("#e0e0e0");

                    // ── LINE ITEMS TABLE ───────────────────────────────────
                    col.Item().PaddingHorizontal(20).PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);   // Description
                            c.RelativeColumn(1.2f); // Qty
                            c.RelativeColumn(1.2f); // Unit
                            c.RelativeColumn(1.8f); // Rate
                            c.RelativeColumn(2);    // Amount
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(8)
                                .Text("Description").FontSize(7.5f).Bold().FontColor("#ffffff");
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(8)
                                .AlignRight().Text("Qty").FontSize(7.5f).Bold().FontColor("#ffffff");
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(8)
                                .Text("Unit").FontSize(7.5f).Bold().FontColor("#ffffff");
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(8)
                                .AlignRight().Text("Rate (R)").FontSize(7.5f).Bold().FontColor("#ffffff");
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(8)
                                .AlignRight().Text("Amount (R)").FontSize(7.5f).Bold().FontColor("#ffffff");
                        });

                        // Line item rows
                        bool alt = false;
                        foreach (var item in invoice.LineItems.OrderBy(l => l.SortOrder))
                        {
                            var bg = alt ? Light : "#ffffff";
                            alt = !alt;
                            var isLump = item.Unit == "lump sum";

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#eeeeee")
                                .PaddingVertical(8).PaddingHorizontal(8)
                                .Text(item.Description).FontSize(9).Bold().FontColor(Navy);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#eeeeee")
                                .PaddingVertical(8).PaddingHorizontal(8).AlignRight()
                                .Text(isLump ? "-" : item.Quantity.ToString("G")).FontSize(9);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#eeeeee")
                                .PaddingVertical(8).PaddingHorizontal(8)
                                .Text(item.Unit).FontSize(9).FontColor(Muted);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#eeeeee")
                                .PaddingVertical(8).PaddingHorizontal(8).AlignRight()
                                .Text(isLump ? "-" : item.Rate.ToString("N2")).FontSize(9);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#eeeeee")
                                .PaddingVertical(8).PaddingHorizontal(8).AlignRight()
                                .Text(item.Amount.ToString("N2")).FontSize(9).Bold();
                        }

                        // Subtotal row (only if VAT enabled)
                        if (invoice.VATEnabled)
                        {
                            table.Cell().ColumnSpan(4).PaddingVertical(6).PaddingHorizontal(8)
                                .AlignRight().Text("Subtotal").FontSize(9).FontColor(Muted);
                            table.Cell().PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                                .Text(invoice.SubTotal.ToString("N2")).FontSize(9).FontColor(Muted);

                            table.Cell().ColumnSpan(4).PaddingVertical(6).PaddingHorizontal(8)
                                .AlignRight().Text($"VAT ({invoice.VATRate}%)").FontSize(9).FontColor(Muted);
                            table.Cell().PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                                .Text(invoice.VATAmount.ToString("N2")).FontSize(9).FontColor(Muted);
                        }

                        // Retention row
                        if (invoice.RetentionEnabled)
                        {
                            table.Cell().ColumnSpan(4).PaddingVertical(6).PaddingHorizontal(8)
                                .AlignRight()
                                .Text($"{invoice.RetentionType} ({invoice.RetentionPercentage}%)")
                                .FontSize(9).FontColor("#c0392b");
                            table.Cell().PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                                .Text($"- {invoice.RetentionAmount:N2}").FontSize(9).FontColor("#c0392b");
                        }

                        // Grand total row
                        table.Cell().ColumnSpan(4).Background(Navy)
                            .PaddingVertical(10).PaddingHorizontal(8).AlignRight()
                            .Text("TOTAL AMOUNT DUE").FontSize(10).Bold().FontColor("#ffffff");
                        table.Cell().Background(Navy)
                            .PaddingVertical(10).PaddingHorizontal(8).AlignRight()
                            .Text($"R {invoice.TotalAmount:N2}").FontSize(10).Bold().FontColor(Gold);
                    });

                    // ── NOTES ──────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(invoice.Notes))
                    {
                        col.Item().PaddingHorizontal(20).PaddingTop(12)
                            .BorderLeft(4).BorderColor(Gold)
                            .Background("#fffbea")
                            .Padding(10)
                            .Text(txt =>
                            {
                                txt.Span("Notes: ").Bold().FontSize(8.5f);
                                txt.Span(invoice.Notes).FontSize(8.5f).FontColor("#555555");
                            });
                    }

                    // ── BANKING DETAILS ────────────────────────────────────
                    if (banking != null && !string.IsNullOrEmpty(banking.BankName))
                    {
                        col.Item().PaddingHorizontal(20).PaddingTop(14)
                            .Background(Light).Padding(14).Column(bank =>
                        {
                            bank.Item().Text("BANKING DETAILS")
                                .FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.15f);

                            bank.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    left.Item().Text("Bank").FontSize(7.5f).FontColor(Muted);
                                    left.Item().Text(banking.BankName).FontSize(9).Bold().FontColor(Navy);

                                    if (!string.IsNullOrEmpty(banking.AccountNumber))
                                    {
                                        left.Item().PaddingTop(6).Text("Account Number").FontSize(7.5f).FontColor(Muted);
                                        left.Item().Text(banking.AccountNumber).FontSize(9).Bold().FontColor(Navy);
                                    }
                                });

                                row.RelativeItem().Column(right =>
                                {
                                    if (!string.IsNullOrEmpty(banking.AccountType))
                                    {
                                        right.Item().Text("Account Type").FontSize(7.5f).FontColor(Muted);
                                        right.Item().Text(banking.AccountType).FontSize(9).Bold().FontColor(Navy);
                                    }

                                    if (!string.IsNullOrEmpty(banking.BranchCode))
                                    {
                                        right.Item().PaddingTop(6).Text("Branch Code").FontSize(7.5f).FontColor(Muted);
                                        right.Item().Text(banking.BranchCode).FontSize(9).Bold().FontColor(Navy);
                                    }
                                });
                            });
                        });
                    }

                    col.Item().Height(16);
                });

                // ── FOOTER ─────────────────────────────────────────────────
                page.Footer().Background(Navy).PaddingVertical(8).AlignCenter()
                    .Text(txt =>
                    {
                        txt.Span("Thank you for your business  —  ").FontColor("#aaaaaa").FontSize(8);
                        txt.Span(companyName).FontColor("#cccccc").FontSize(8).Bold();
                        txt.Span($"  —  {invoice.InvoiceNumber}").FontColor("#aaaaaa").FontSize(8);
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static string BuildStampSvg(string companyName, string invoiceNumber)
    {
        var name = companyName.ToUpperInvariant();
        if (name.Length > 22) name = name[..22];

        return $"""
            <svg viewBox="0 0 130 130" xmlns="http://www.w3.org/2000/svg">
                <defs>
                    <path id="topArc" d="M 9,65 A 56,56 0 0,1 121,65" fill="none"/>
                </defs>
                <circle cx="65" cy="65" r="61" fill="none" stroke="#1a1a2e" stroke-width="3"/>
                <circle cx="65" cy="65" r="52" fill="none" stroke="#1a1a2e" stroke-width="1.5"/>
                <text font-size="9" font-weight="700" fill="#1a1a2e" letter-spacing="1.5">
                    <textPath href="#topArc" startOffset="50%" text-anchor="middle">{name}</textPath>
                </text>
                <line x1="24" y1="48" x2="106" y2="48" stroke="#1a1a2e" stroke-width="1"/>
                <line x1="24" y1="86" x2="106" y2="86" stroke="#1a1a2e" stroke-width="1"/>
                <text x="65" y="67" text-anchor="middle" font-size="13" font-weight="900" fill="#1a1a2e" letter-spacing="1">ORIGINAL</text>
                <text x="65" y="82" text-anchor="middle" font-size="11" font-weight="700" fill="#1a1a2e" letter-spacing="1">INVOICE</text>
                <text x="65" y="103" text-anchor="middle" font-size="8.5" font-weight="600" fill="#1a1a2e" letter-spacing="1.5">{invoiceNumber}</text>
            </svg>
            """;
    }
}
