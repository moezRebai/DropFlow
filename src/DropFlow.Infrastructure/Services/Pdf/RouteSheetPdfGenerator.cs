using DropFlow.Application.Dto;
using DropFlow.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DropFlow.Infrastructure.Services.Pdf;

public class RouteSheetPdfGenerator : IRouteSheetPdfGenerator
{
    // ── Neutral palette ──────────────────────────────────────────
    private const string BgLight   = "#F9FAFB";
    private const string BgRow     = "#F9FAFB";
    private const string Border    = "#E5E7EB";
    private const string BorderMid = "#D1D5DB";
    private const string TextDark  = "#111827";
    private const string TextMid   = "#374151";
    private const string TextMuted = "#6B7280";
    private const string TextLight = "#9CA3AF";
    private const string Green     = "#059669";
    private const string Orange    = "#D97706";
    private const string OrangeBg  = "#FEF3C7";

    private readonly HttpClient _httpClient;

    public RouteSheetPdfGenerator()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public byte[] Generate(RouteSheetDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(TextDark));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(c => ComposeFooter(c, data));
            });
        }).GeneratePdf();
    }

    public Task<byte[]> GenerateAsync(RouteSheetDto data) => Task.FromResult(Generate(data));

    // ── Helpers ──────────────────────────────────────────────────

    private byte[]? DownloadImageFromUrl(string url)
    {
        try
        {
            var r = _httpClient.GetAsync(url).Result;
            return r.IsSuccessStatusCode ? r.Content.ReadAsByteArrayAsync().Result : null;
        }
        catch { return null; }
    }

    private static string FormatTimeSlot(RouteSheetDeliveryDto d)
    {
        if (d.TimeSlotStart.HasValue && d.TimeSlotEnd.HasValue)
            return $"{d.TimeSlotStart.Value.Hours}–{d.TimeSlotEnd.Value.Hours}";

        if (d.EstimatedArrivalTime.HasValue)
            return d.EstimatedArrivalTime.Value.Hours.ToString();

        return "—";
    }

    // ── HEADER ───────────────────────────────────────────────────

    private void ComposeHeader(IContainer container, RouteSheetDto data)
    {
        container.Column(col =>
        {
            // Top row: Logo | Title | Route details
            col.Item().Row(row =>
            {
                // Left — Logo + company
                row.ConstantItem(170).Column(left =>
                {
                    byte[]? logoBytes = null;
                    if (!string.IsNullOrEmpty(data.CompanyLogoUrl))
                        logoBytes = DownloadImageFromUrl(data.CompanyLogoUrl);

                    if (logoBytes != null)
                        left.Item().Height(60).Width(170).Image(logoBytes);

                    left.Item().PaddingTop(8).Column(info =>
                    {
                        info.Item().Text(data.CompanyName)
                            .FontSize(9).Bold().FontColor(TextDark);
                        info.Item().Text(data.CompanyAddress)
                            .FontSize(7).FontColor(TextMuted);
                        info.Item().Text(data.CompanyCity)
                            .FontSize(7).FontColor(TextMuted);
                        if (!string.IsNullOrEmpty(data.CompanyPhone))
                            info.Item().Text($"Tél : {data.CompanyPhone}")
                                .FontSize(7).FontColor(TextMuted);
                        if (!string.IsNullOrEmpty(data.CompanySiret))
                            info.Item().Text($"SIRET : {data.CompanySiret}")
                                .FontSize(7).FontColor(TextMuted);
                    });
                });

                row.ConstantItem(14);

                // Center — Title block (light gray bg, dark text)
                row.RelativeItem()
                    .Background(BgLight)
                    .Border(1).BorderColor(Border)
                    .Padding(14)
                    .Column(center =>
                    {
                        center.Item().AlignCenter()
                            .Text("FEUILLE DE ROUTE")
                            .FontSize(20).Bold().FontColor(TextDark);

                        center.Item().PaddingTop(4).AlignCenter()
                            .Text($"N° {data.RouteReference}")
                            .FontSize(12).Bold().FontColor(TextMid);

                        center.Item().PaddingTop(6)
                            .LineHorizontal(1).LineColor(Border);

                        center.Item().PaddingTop(6).AlignCenter()
                            .Text(data.RouteDate.ToString("dddd d MMMM yyyy").ToUpper())
                            .FontSize(9).FontColor(TextMuted);
                    });

                row.ConstantItem(14);

                // Right — Operational info
                row.ConstantItem(170)
                    .Border(1).BorderColor(Border)
                    .Background(BgLight)
                    .Padding(10)
                    .Column(right =>
                    {
                        void InfoRow(string label, string value)
                        {
                            right.Item().PaddingBottom(7).Row(r =>
                            {
                                r.ConstantItem(50)
                                    .Text(label)
                                    .FontSize(7).Bold().FontColor(TextMuted);
                                r.RelativeItem()
                                    .Text(value)
                                    .FontSize(8).FontColor(TextDark);
                            });
                        }

                        InfoRow("Véhicule :", data.VehicleName);
                        InfoRow("Équipe :", data.TeamMembers);
                        InfoRow("Départ :", $"{data.DepartureTime:hh\\:mm}");
                        if (!string.IsNullOrEmpty(data.DepartureAddress))
                            InfoRow("Dépôt :", data.DepartureAddress);
                    });
            });

            // Stats strip
            col.Item().PaddingTop(10)
                .Border(1).BorderColor(Border)
                .Background(BgLight)
                .Row(bar =>
                {
                    void Stat(string value, string label, bool last = false)
                    {
                        var cell = bar.RelativeItem();
                        if (!last) cell = cell.BorderRight(1).BorderColor(Border);
                        cell.Padding(7).Column(c =>
                        {
                            c.Item().AlignCenter()
                                .Text(value).FontSize(13).Bold().FontColor(TextDark);
                            c.Item().PaddingTop(2).AlignCenter()
                                .Text(label).FontSize(6).FontColor(TextMuted);
                        });
                    }

                    Stat(data.Deliveries.Count.ToString(), "ARRÊTS");
                    Stat($"{data.TotalClientPayment:N0} €", "TOTAL CLIENT");
                    Stat($"{data.TotalStorePayment:N0} €", "TOTAL ENSEIGNE");
                    Stat($"{data.DepartureTime:hh\\:mm}", "HEURE DÉPART", last: true);
                });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Border);
        });
    }

    // ── CONTENT ──────────────────────────────────────────────────

    private void ComposeContent(IContainer container, RouteSheetDto data)
    {
        container.PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(22);    // #
                cols.ConstantColumn(55);    // N° Dossier
                cols.RelativeColumn(1.5f);  // Client / Tél
                cols.ConstantColumn(72);    // Ville
                cols.ConstantColumn(36);    // Créneau  ("8–10")
                cols.ConstantColumn(22);    // Svc      ("M"/"N")
                cols.RelativeColumn(0.9f);  // Enseigne
                cols.ConstantColumn(65);    // Client (€)
                cols.ConstantColumn(65);    // Enseigne (€)
                cols.RelativeColumn(1.2f);  // Instructions
            });

            // Header row — light gray, dark text
            table.Header(header =>
            {
                static IContainer H(IContainer c) => c
                    .Background(BgLight)
                    .BorderBottom(2).BorderColor(BorderMid)
                    .PaddingVertical(6).PaddingHorizontal(5)
                    .DefaultTextStyle(x => x.FontColor(TextMid).FontSize(7).Bold());

                header.Cell().Element(H).AlignCenter().Text("#");
                header.Cell().Element(H).Text("N° Dossier");
                header.Cell().Element(H).Text("Client / Téléphone");
                header.Cell().Element(H).Text("Ville");
                header.Cell().Element(H).AlignCenter().Text("Créneau");
                header.Cell().Element(H).AlignCenter().Text("Svc");
                header.Cell().Element(H).Text("Enseigne");
                header.Cell().Element(H).AlignRight().Text("Client (€)");
                header.Cell().Element(H).AlignRight().Text($"{data.CompanyAcronym} (€)");
                header.Cell().Element(H).Text("Instructions");
            });

            // Data rows
            for (int i = 0; i < data.Deliveries.Count; i++)
            {
                var d = data.Deliveries[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : BgRow;

                IContainer Row(IContainer c) => c
                    .Background(bg)
                    .BorderBottom(1).BorderColor(Border)
                    .PaddingHorizontal(5).PaddingVertical(5);

                // Stop # — bold number, subtle left border accent
                table.Cell()
                    .Background(bg)
                    .BorderLeft(3).BorderColor(BorderMid)
                    .BorderBottom(1).BorderColor(Border)
                    .PaddingHorizontal(4).PaddingVertical(5)
                    .AlignCenter().AlignMiddle()
                    .Text(d.SequenceOrder.ToString())
                    .FontSize(11).Bold().FontColor(TextDark);

                // N° Dossier
                table.Cell().Element(Row)
                    .Text(d.DeliveryReference).FontSize(7).FontColor(TextMuted);

                // Client + Phone
                table.Cell().Element(Row).Column(c =>
                {
                    c.Item().Text(d.ClientName).FontSize(8).Bold().FontColor(TextDark);
                    if (!string.IsNullOrEmpty(d.ClientPhone))
                        c.Item().PaddingTop(2).Text(d.ClientPhone).FontSize(7).FontColor(TextMuted);
                });

                // City only
                table.Cell().Element(Row)
                    .Text(d.City.ToUpper()).FontSize(8).Bold().FontColor(TextMid);

                // Time slot
                table.Cell().Element(Row).AlignCenter()
                    .Text(FormatTimeSlot(d)).FontSize(8).Bold().FontColor(TextDark);

                // Service badge
                var isAssembly = d.ServiceType == "M";
                table.Cell()
                    .Background(isAssembly ? OrangeBg : bg)
                    .BorderBottom(1).BorderColor(Border)
                    .PaddingHorizontal(5).PaddingVertical(5)
                    .AlignCenter().AlignMiddle()
                    .Text(d.ServiceType).FontSize(8).Bold()
                    .FontColor(isAssembly ? Orange : TextMuted);

                // Enseigne
                table.Cell().Element(Row)
                    .Text(d.StoreName ?? "—").FontSize(7).FontColor(TextMuted);

                // Rgl. Client
                var hasClientAmt = d.ClientPaymentAmount > 0;
                var clientTxt = table.Cell().Element(Row).AlignRight()
                    .Text(hasClientAmt ? $"{d.ClientPaymentAmount:N0} €" : "—")
                    .FontSize(8).FontColor(hasClientAmt ? Green : TextLight);
                if (hasClientAmt) clientTxt.Bold();

                // Rgl. Enseigne
                var hasStoreAmt = d.StorePaymentAmount > 0;
                var storeTxt = table.Cell().Element(Row).AlignRight()
                    .Text(hasStoreAmt ? $"{d.StorePaymentAmount:N0} €" : "—")
                    .FontSize(8).FontColor(hasStoreAmt ? TextDark : TextLight);
                if (hasStoreAmt) storeTxt.Bold();

                // Instructions
                table.Cell().Element(Row)
                    .Text(d.Instructions ?? "").FontSize(7).Italic().FontColor(TextMuted);
            }

            // Total row
            table.Cell().ColumnSpan(7)
                .Background(BgLight).BorderTop(2).BorderColor(BorderMid)
                .PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                .Text("TOTAL").FontSize(8).Bold().FontColor(TextMid);

            table.Cell()
                .Background(BgLight).BorderTop(2).BorderColor(BorderMid)
                .PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                .Text($"{data.TotalClientPayment:N0} €").FontSize(9).Bold().FontColor(Green);

            table.Cell()
                .Background(BgLight).BorderTop(2).BorderColor(BorderMid)
                .PaddingVertical(6).PaddingHorizontal(8).AlignRight()
                .Text($"{data.TotalStorePayment:N0} €").FontSize(9).Bold().FontColor(TextDark);

            table.Cell()
                .Background(BgLight).BorderTop(2).BorderColor(BorderMid)
                .Padding(6);
        });
    }

    // ── FOOTER ───────────────────────────────────────────────────

    private void ComposeFooter(IContainer container, RouteSheetDto data)
    {
        container.Column(col =>
        {
            // Notes banner (only when filled)
            if (!string.IsNullOrWhiteSpace(data.Notes))
            {
                col.Item().PaddingBottom(6)
                    .Border(1).BorderColor(Border)
                    .Background(BgLight)
                    .Padding(7)
                    .Text($"Note : {data.Notes}")
                    .FontSize(7).Italic().FontColor(TextMuted);
            }

            // Signature boxes
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Border(1).BorderColor(Border)
                    .Background(BgLight).Padding(8).Height(55)
                    .Column(c =>
                    {
                        c.Item().Text("Espèces remis (€)")
                            .FontSize(7).Bold().FontColor(TextMid);
                        c.Item().PaddingTop(5).LineHorizontal(1).LineColor(Border);
                    });

                row.ConstantItem(6);

                row.RelativeItem()
                    .Border(1).BorderColor(Border)
                    .Background(BgLight).Padding(8).Height(55)
                    .Column(c =>
                    {
                        c.Item().Text("Nombre de chèques")
                            .FontSize(7).Bold().FontColor(TextMid);
                        c.Item().PaddingTop(5).LineHorizontal(1).LineColor(Border);
                    });

                row.ConstantItem(6);

                row.RelativeItem()
                    .Border(1).BorderColor(Border)
                    .Background(BgLight).Padding(8).Height(55)
                    .Column(c =>
                    {
                        c.Item().Text("Signature du livreur")
                            .FontSize(7).Bold().FontColor(TextMid);
                    });

                row.ConstantItem(6);

                row.RelativeItem()
                    .Border(1).BorderColor(Border)
                    .Background(BgLight).Padding(8).Height(55)
                    .Column(c =>
                    {
                        c.Item().Text("Contrôlé par")
                            .FontSize(7).Bold().FontColor(TextMid);
                    });
            });

            // Bottom row
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Retour le : ").FontSize(8).Bold().FontColor(TextMid);
                    t.Span("  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·")
                        .FontSize(8).FontColor(Border);
                });
                row.ConstantItem(30);
                row.RelativeItem().AlignRight()
                    .Text($"{data.CompanyName}  —  {data.CompanyAddress}, {data.CompanyCity}")
                    .FontSize(6).FontColor(TextLight);
            });
        });
    }
}
