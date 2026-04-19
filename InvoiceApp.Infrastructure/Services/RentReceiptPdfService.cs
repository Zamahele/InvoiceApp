using InvoiceApp.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceApp.Infrastructure.Services;

public class RentReceiptPdfService
{
    private const string Navy  = "#1a1a2e";
    private const string Gold  = "#e8c84a";
    private const string Light = "#f8f9fa";
    private const string Muted = "#888888";
    private const string Body  = "#333333";

    public byte[] Generate(RentPayment payment, CompanySettings? company, BankingDetails? banking)
    {
        var companyName = company?.Name ?? string.Empty;
        var monthName = new DateTime(payment.Year, payment.Month, 1).ToString("MMMM yyyy");
        var receiptRef = $"RCP-{payment.Year}{payment.Month:D2}-{payment.Room.Name.Replace(" ", "").ToUpperInvariant()[..Math.Min(4, payment.Room.Name.Length)]}";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(Body));

                var stampSvg = BuildReceiptStampSvg(companyName, receiptRef);
                var stampImage = SvgImage.FromText(stampSvg);

                page.Foreground()
                    .AlignBottom().AlignRight()
                    .PaddingRight(28).PaddingBottom(58)
                    .Rotate(-12)
                    .Width(130).Height(130)
                    .Svg(stampImage);

                page.Content().Column(col =>
                {
                    // ── HEADER ─────────────────────────────────────────────
                    col.Item().Background(Navy).Padding(20).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(companyName)
                                .FontSize(16).Bold().FontColor("#ffffff");

                            if (!string.IsNullOrEmpty(company?.Phone))
                                left.Item().PaddingTop(4)
                                    .Text(company.Phone)
                                    .FontSize(8).FontColor("#cccccc");
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().AlignRight()
                                .Text("RENT RECEIPT")
                                .FontSize(22).Bold().FontColor(Gold)
                                .LetterSpacing(0.1f);

                            right.Item().AlignRight().PaddingTop(4).Text(txt =>
                            {
                                txt.Span("Ref: ").FontColor("#cccccc").FontSize(8);
                                txt.Span(receiptRef).FontColor("#ffffff").Bold().FontSize(8);
                            });

                            right.Item().AlignRight().Text(txt =>
                            {
                                txt.Span("Period: ").FontColor("#cccccc").FontSize(8);
                                txt.Span(monthName).FontColor("#ffffff").FontSize(8);
                            });

                            if (payment.PaidDate.HasValue)
                                right.Item().AlignRight().Text(txt =>
                                {
                                    txt.Span("Paid: ").FontColor("#cccccc").FontSize(8);
                                    txt.Span(payment.PaidDate.Value.ToString("dd MMM yyyy"))
                                       .FontColor("#ffffff").FontSize(8);
                                });
                        });
                    });

                    // ── GOLD ACCENT BAR ────────────────────────────────────
                    col.Item().Height(5).Background(Gold);

                    // ── LANDLORD / TENANT ROW ──────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .BorderLeft(5).BorderColor(Gold)
                            .Background("#ffffff")
                            .Padding(16).Column(from =>
                        {
                            from.Item().Text("FROM")
                                .FontSize(7).Bold().FontColor(Gold).LetterSpacing(0.15f);

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
                        });

                        row.RelativeItem()
                            .Background(Navy)
                            .Padding(16).Column(to =>
                        {
                            to.Item().Text("TENANT")
                                .FontSize(7).Bold().FontColor(Gold).LetterSpacing(0.15f);

                            to.Item().PaddingTop(5)
                                .Text(payment.Room.TenantName)
                                .FontSize(10).Bold().FontColor("#ffffff");

                            to.Item().PaddingTop(4).Text(txt =>
                            {
                                txt.Span("Room: ").FontColor("#aaaaaa").FontSize(8);
                                txt.Span(payment.Room.Name).FontColor("#ffffff").FontSize(8);
                            });

                            if (!string.IsNullOrEmpty(payment.Room.TenantPhone))
                                to.Item().Text(txt =>
                                {
                                    txt.Span("Phone: ").FontColor("#aaaaaa").FontSize(8);
                                    txt.Span(payment.Room.TenantPhone).FontColor("#ffffff").FontSize(8);
                                });
                        });
                    });

                    // ── SEPARATOR ──────────────────────────────────────────
                    col.Item().Height(2).Background("#e0e0e0");

                    // ── PAYMENT SUMMARY TABLE ──────────────────────────────
                    col.Item().PaddingHorizontal(20).PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(12)
                                .Text("Description").FontSize(8).Bold().FontColor("#ffffff");
                            header.Cell().Background(Navy).PaddingVertical(8).PaddingHorizontal(12)
                                .AlignRight().Text("Amount (R)").FontSize(8).Bold().FontColor("#ffffff");
                        });

                        // Rent row
                        table.Cell().Background(Light).BorderBottom(1).BorderColor("#e0e0e0")
                            .PaddingVertical(10).PaddingHorizontal(12)
                            .Text($"Rent — {payment.Room.Name} ({monthName})")
                            .FontSize(9).Bold().FontColor(Navy);
                        table.Cell().Background(Light).BorderBottom(1).BorderColor("#e0e0e0")
                            .PaddingVertical(10).PaddingHorizontal(12).AlignRight()
                            .Text(payment.AmountDue.ToString("N2")).FontSize(9);

                        if (payment.AmountPaid.HasValue && payment.AmountPaid != payment.AmountDue)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#e0e0e0")
                                .PaddingVertical(8).PaddingHorizontal(12)
                                .Text("Amount Due").FontSize(9).FontColor(Muted);
                            table.Cell().BorderBottom(1).BorderColor("#e0e0e0")
                                .PaddingVertical(8).PaddingHorizontal(12).AlignRight()
                                .Text(payment.AmountDue.ToString("N2")).FontSize(9).FontColor(Muted);
                        }

                        // Amount paid (total row)
                        var paid = payment.AmountPaid ?? payment.AmountDue;
                        table.Cell().Background(Navy)
                            .PaddingVertical(10).PaddingHorizontal(12)
                            .Text("AMOUNT RECEIVED").FontSize(10).Bold().FontColor("#ffffff");
                        table.Cell().Background(Navy)
                            .PaddingVertical(10).PaddingHorizontal(12).AlignRight()
                            .Text($"R {paid:N2}").FontSize(10).Bold().FontColor(Gold);
                    });

                    // ── NOTES ──────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(payment.Notes))
                    {
                        col.Item().PaddingHorizontal(20).PaddingTop(12)
                            .BorderLeft(4).BorderColor(Gold)
                            .Background("#fffbea")
                            .Padding(10)
                            .Text(txt =>
                            {
                                txt.Span("Notes: ").Bold().FontSize(8.5f);
                                txt.Span(payment.Notes).FontSize(8.5f).FontColor("#555555");
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

                    // ── PAID CONFIRMATION WATERMARK TEXT ──────────────────
                    col.Item().PaddingHorizontal(20).PaddingTop(20)
                        .AlignCenter()
                        .Text("PAYMENT RECEIVED IN FULL")
                        .FontSize(10).Bold().FontColor(Gold).LetterSpacing(0.2f);

                    col.Item().Height(16);
                });

                // ── FOOTER ─────────────────────────────────────────────────
                page.Footer().Background(Navy).PaddingVertical(8).AlignCenter()
                    .Text(txt =>
                    {
                        txt.Span("Thank you  —  ").FontColor("#aaaaaa").FontSize(8);
                        txt.Span(companyName).FontColor("#cccccc").FontSize(8).Bold();
                        txt.Span($"  —  {receiptRef}").FontColor("#aaaaaa").FontSize(8);
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static string BuildReceiptStampSvg(string companyName, string receiptRef)
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
                <text x="65" y="65" text-anchor="middle" font-size="11" font-weight="900" fill="#1a1a2e" letter-spacing="1">RENT</text>
                <text x="65" y="80" text-anchor="middle" font-size="11" font-weight="700" fill="#1a1a2e" letter-spacing="1">RECEIPT</text>
                <text x="65" y="103" text-anchor="middle" font-size="7" font-weight="600" fill="#1a1a2e" letter-spacing="1">{receiptRef}</text>
            </svg>
            """;
    }
}
