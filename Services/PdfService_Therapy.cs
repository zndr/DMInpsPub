using System.Linq; // <-- Add this using directive
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DMInps.Models;

namespace DMInps.Services
{
    public partial class PdfService
    {
        /// <summary>
        /// Renderizza la sezione "Terapia Antidiabetica in Atto"
        /// </summary>
        private void RenderSezioneTerapiaAntidiabetica(IContainer container, DiabetesTherapyData therapyData)
        {
            container.Column(column =>
            {
                // Header sezione
                column.Item().PaddingVertical(5).Row(row =>
                {
                    row.RelativeItem().Background("#16A085").Padding(8).Text("TERAPIA ANTIDIABETICA IN ATTO")
                        .FontSize(14).Bold().FontColor("#FFFFFF");
                });

                column.Item().PaddingVertical(5);

                if (therapyData?.HasTherapies == true)
                {
                    // Tabella farmaci
                    column.Item().Table(table =>
                    {
                        // Definizione colonne
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Classe farmacologica
                            columns.RelativeColumn(4); // Nome commerciale/principio
                            columns.RelativeColumn(2); // Dosaggio
                        });

                        // Header tabella
                        table.Header(header =>
                        {
                            header.Cell().Background("#ECF0F1").Border(1).BorderColor("#BDC3C7")
                                .Padding(5).Text("Classe Farmacologica")
                                .FontSize(10).Bold();

                            header.Cell().Background("#ECF0F1").Border(1).BorderColor("#BDC3C7")
                                .Padding(5).Text("Nome Commerciale / Principio Attivo")
                                .FontSize(10).Bold();

                            header.Cell().Background("#ECF0F1").Border(1).BorderColor("#BDC3C7")
                                .Padding(5).Text("Dosaggio")
                                .FontSize(10).Bold();
                        });

                        // Raggruppamento per categoria
                        var groupedTherapies = therapyData.Therapies
                            .GroupBy(t => t.Category)
                            .OrderBy(g => GetCategoryOrder(g.Key));

                        foreach (var group in groupedTherapies)
                        {
                            bool isFirstInGroup = true;
                            
                            foreach (var therapy in group)
                            {
                                // Categoria (solo prima riga del gruppo)
                                table.Cell().Border(1).BorderColor("#BDC3C7")
                                    .Padding(5).AlignMiddle()
                                    .Text(isFirstInGroup ? therapy.Category : "")
                                    .FontSize(9).FontColor("#2C3E50");

                                // Nome farmaco
                                table.Cell().Border(1).BorderColor("#BDC3C7")
                                    .Padding(5).AlignMiddle()
                                    .Text(therapy.DrugName)
                                    .FontSize(9).FontColor("#34495E");

                                // Dosaggio
                                table.Cell().Border(1).BorderColor("#BDC3C7")
                                    .Padding(5).AlignMiddle()
                                    .Text(therapy.Dosage)
                                    .FontSize(9).FontColor("#34495E");

                                isFirstInGroup = false;
                            }
                        }
                    });

                    // Note informative
                    column.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span("Farmaci attivi: ").FontSize(8).Italic().FontColor("#7F8C8D");
                        text.Span(therapyData.Therapies.Count.ToString()).FontSize(8).Bold().FontColor("#2C3E50");
                    });
                }
                else
                {
                    // Nessuna terapia trovata
                    column.Item().Background("#FEF9E7").Border(1).BorderColor("#F39C12")
                        .Padding(10).Text("Nessuna terapia antidiabetica continuativa rilevata nel database.")
                        .FontSize(10).FontColor("#7D6608");
                }
            });
        }

        /// <summary>
        /// Ordine di visualizzazione delle categorie
        /// </summary>
        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "Metformina" => 1,
                "Sulfaniluree/Glinidi" => 2,
                "Inibitori DPP-4" => 3,
                "Agonisti GLP-1/GIP" => 4,
                "Inibitori SGLT2" => 5,
                "Insulina" => 6,
                _ => 99 // Altri
            };
        }
    }
}
