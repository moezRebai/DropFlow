using DropFlow.Application.Dto;
using DropFlow.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DropFlow.Infrastructure.Services.Pdf;

/// <summary>
/// Générateur de feuilles de route PDF - VERSION FINALE CORRIGÉE
/// </summary>
public class RouteSheetPdfGenerator : IRouteSheetPdfGenerator
{
    private readonly HttpClient _httpClient;

    public RouteSheetPdfGenerator()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public byte[] Generate(RouteSheetDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(c => ComposeFooter(c, data));
            });
        }).GeneratePdf();
    }

    public Task<byte[]> GenerateAsync(RouteSheetDto data)
    {
        return Task.FromResult(Generate(data));
    }

    /// <summary>
    /// Télécharge l'image depuis une URL HTTP
    /// </summary>
    private byte[]? DownloadImageFromUrl(string url)
    {
        try
        {
            var response = _httpClient.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsByteArrayAsync().Result;
            }
        }
        catch
        {
            // Erreur de téléchargement, retourner null
        }
        return null;
    }

    private void ComposeHeader(IContainer container, RouteSheetDto data)
    {
        container.Column(column =>
        {
            // Ligne principale : Logo + Titre (MÊME HAUTEUR)
            column.Item().Row(row =>
            {
                // ✅ LOGO - Tout en haut à gauche
                row.ConstantItem(180).Column(logoColumn =>
                {
                    // Télécharger et afficher le logo
                    if (!string.IsNullOrEmpty(data.CompanyLogoUrl))
                    {
                        var imageBytes = DownloadImageFromUrl(data.CompanyLogoUrl);
                        
                        if (imageBytes != null)
                        {
                            logoColumn.Item().Height(60).Width(180)
                                .Image(imageBytes);
                        }
                        else
                        {
                            // Fallback si échec téléchargement
                            logoColumn.Item().Height(60).Width(180)
                                .Border(1).BorderColor(Colors.Grey.Lighten2)
                                .AlignCenter().AlignMiddle()
                                .Text("[LOGO]").FontSize(10).FontColor(Colors.Grey.Medium);
                        }
                    }
                    else
                    {
                        // Placeholder si pas de logo
                        logoColumn.Item().Height(60).Width(180)
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .AlignCenter().AlignMiddle()
                            .Text("[LOGO]").FontSize(10).FontColor(Colors.Grey.Medium);
                    }
                });

                row.ConstantItem(10); // Espace

                // ✅ TITRE - Grand rectangle avec cadre SANS fond bleu
                row.RelativeItem().Column(titleColumn =>
                {
                    titleColumn.Item()
                        .Border(2).BorderColor(Colors.Blue.Darken2) // Cadre bleu épais
                        .Padding(12)
                        .Column(innerColumn =>
                        {
                            // Titre
                            innerColumn.Item().AlignCenter()
                                .Text("LETTRE DE VOITURE")
                                .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            
                            // Numéro de référence
                            innerColumn.Item().AlignCenter()
                                .Text($"N° {data.RouteReference}")
                                .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            
                            // Date de tournée (en bas du rectangle)
                            innerColumn.Item().PaddingTop(8).AlignCenter()
                                .Text(data.RouteDate.ToString("dd/MM/yyyy"))
                                .FontSize(12).FontColor(Colors.Grey.Darken2);
                        });
                });
            });

            // Informations entreprise (sous le logo)
            column.Item().PaddingTop(5).PaddingLeft(0).Text(text =>
            {
                text.Span(data.CompanyName).FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                text.Line("");
                text.Span(data.CompanyAddress).FontSize(8).FontColor(Colors.Black);
                text.Line("");
                text.Span(data.CompanyCity).FontSize(8).FontColor(Colors.Black);
                text.Line("");
                text.Span($"Tél: {data.CompanyPhone}").FontSize(8).FontColor(Colors.Black);
                text.Line("");
                text.Span($"Siret: {data.CompanySiret}").FontSize(8).FontColor(Colors.Black);
            });

            // Ligne de séparation
            column.Item().PaddingVertical(8).LineHorizontal(2).LineColor(Colors.Blue.Lighten3);

            // Informations ÉQUIPE et VÉHICULE
            column.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("ÉQUIPE : ").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                    text.Span(data.TeamMembers).FontSize(9).FontColor(Colors.Black);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("VÉHICULE : ").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                    text.Span(data.VehicleName).FontSize(9).FontColor(Colors.Black);
                });
            });

            column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, RouteSheetDto data)
    {
        container.PaddingVertical(8).Column(column =>
        {
            // Tableau des livraisons
            column.Item().Table(table =>
            {
                // Définir les colonnes
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20);      // #
                    columns.ConstantColumn(65);      // N° Dossier
                    columns.RelativeColumn(1.2f);    // Client
                    columns.ConstantColumn(70);      // Téléphone
                    columns.RelativeColumn(1f);      // Ville
                    columns.ConstantColumn(30);      // H
                    columns.ConstantColumn(50);      // Mtg
                    columns.RelativeColumn(1f);      // Enseigne
                    columns.ConstantColumn(70);      // CRT MAG(€)
                    columns.ConstantColumn(50);      // CRT {CompanyAcronym}(€)
                    columns.RelativeColumn(1.5f);    // Instructions
                });

                // En-tête du tableau
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("#").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("N° Dossier").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Client").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Téléphone").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Ville").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("H").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Service").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Enseigne").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("DÛ MAG(\u20ac)").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text($"DÛ {data.CompanyAcronym}(€)").Bold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Instructions").Bold().FontSize(8);

                    static IContainer HeaderCellStyle(IContainer c) => c
                        .Background(Colors.Blue.Darken2)
                        .BorderBottom(2).BorderColor(Colors.Blue.Darken3)
                        .Padding(4)
                        .DefaultTextStyle(x => x.FontColor(Colors.White));
                });

                // Lignes du tableau (alternées)
                var isEven = false;
                foreach (var delivery in data.Deliveries)
                {
                    isEven = !isEven;
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven)).AlignCenter()
                        .Text(delivery.SequenceOrder.ToString()).FontSize(8).Bold();
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.DeliveryReference).FontSize(7);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.ClientName).FontSize(7);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.ClientPhone).FontSize(6);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.City.ToUpper()).FontSize(7);
                    
                    // TimeSlot format HH (sans minutes)
                    table.Cell().Element(c => RowCellStyle(c, isEven)).AlignCenter()
                        .Text(GetTimeSlotDisplay(delivery))
                        .FontSize(8).Bold().FontColor(Colors.Blue.Darken1);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven)).AlignCenter()
                        .Text(delivery.ServiceType)
                        .FontSize(8).Bold().FontColor(delivery.ServiceType == "M" ? Colors.Orange.Darken1 : Colors.Grey.Darken1);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.StoreName ?? "").FontSize(6);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven)).AlignRight()
                        .Text(delivery.StorePaymentAmount > 0 
                            ? delivery.StorePaymentAmount.ToString("F1") 
                            : "0.0")
                        .FontSize(7);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven)).AlignRight()
                        .Text(delivery.ClientPaymentAmount > 0 
                            ? delivery.ClientPaymentAmount.ToString("F1") 
                            : "0.0")
                        .FontSize(7);
                    
                    table.Cell().Element(c => RowCellStyle(c, isEven))
                        .Text(delivery.Instructions ?? "").FontSize(6);

                    static IContainer RowCellStyle(IContainer c, bool isEven) => c
                        .Background(isEven ? Colors.Grey.Lighten4 : Colors.White)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4);
                }

                // Ligne Total
                table.Cell().ColumnSpan(8).Element(TotalCellStyle).AlignRight()
                    .Text("TOTAL :").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                
                table.Cell().Element(TotalCellStyle).AlignRight()
                    .Text(data.TotalStorePayment.ToString("F1"))
                    .Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                
                table.Cell().Element(TotalCellStyle).AlignRight()
                    .Text(data.TotalClientPayment.ToString("F1"))
                    .Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                
                table.Cell().Element(TotalCellStyle);

                static IContainer TotalCellStyle(IContainer c) => c
                    .Background(Colors.Blue.Lighten4)
                    .BorderTop(2).BorderColor(Colors.Blue.Darken2)
                    .Padding(5);
            });
        });
    }

    /// <summary>
    /// Formate le TimeSlot en HH (sans minutes)
    /// </summary>
    private string GetTimeSlotDisplay(RouteSheetDeliveryDto delivery)
    {
        if (delivery.TimeSlotStart.HasValue && delivery.TimeSlotEnd.HasValue)
        {
            var start = delivery.TimeSlotStart.Value.Hours;
            var end = delivery.TimeSlotEnd.Value.Hours;
            return $"{start:D2}-{end:D2}";
        }
        
        if (delivery.EstimatedArrivalTime.HasValue)
        {
            return delivery.EstimatedArrivalTime.Value.ToString(@"hh");
        }
        
        return "";
    }

    private void ComposeFooter(IContainer container, RouteSheetDto data)
    {
        container.Column(column =>
        {
            // Cases de signature
            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Lighten2)
                    .Background(Colors.Grey.Lighten5)
                    .Padding(8).Column(col =>
                    {
                        col.Item().Text("Espèces remis (€)")
                            .FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(20).Text("").FontSize(8);
                    });

                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Lighten2)
                    .Background(Colors.Grey.Lighten5)
                    .Padding(8).Column(col =>
                    {
                        col.Item().Text("Nbre Chèques")
                            .FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(20).Text("").FontSize(8);
                    });

                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Lighten2)
                    .Background(Colors.Grey.Lighten5)
                    .Padding(8).Column(col =>
                    {
                        col.Item().Text("Signature livreur")
                            .FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(20).Text("").FontSize(8);
                    });
            });

            // Ligne retour et contrôle
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Retour le : ").FontSize(8).Bold();
                    text.Span("..........................................").FontSize(8);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Contrôlé par : ").FontSize(8).Bold();
                    text.Span("..........................................").FontSize(8);
                });
            });

            // Note importante
            if (!string.IsNullOrEmpty(data.Notes))
            {
                column.Item().PaddingTop(12)
                    .Background(Colors.Blue.Lighten4)
                    .Padding(8)
                    .Text(data.Notes)
                    .FontSize(7).Italic().FontColor(Colors.Blue.Darken2);
            }

            // Pied de page
            column.Item().PaddingTop(12).AlignCenter()
                .Text($"{data.CompanyName} - {data.CompanyAddress}, {data.CompanyCity}")
                .FontSize(6).FontColor(Colors.Grey.Darken1);
        });
    }
}