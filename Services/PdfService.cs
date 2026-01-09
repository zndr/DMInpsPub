using System.Collections.Generic;
using DMInps.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DMInps.Services
{
    public partial class PdfService
    {
        public bool GeneratePdf(
            MedicoData medicoData,
            PatientData patientData,
            GlycemicCompensationData compensationData,
            string outputPath,
            DiabetesTherapyData therapyData,
            List<DiabetesComplicationData> complications = null,
            string noteMedico = null)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre); // Ridotto da 2cm a 1.5cm
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                        // Header vuoto (nessuna ripetizione)
                        page.Header().Height(0);

                        page.Content().Column(column =>
                        {
                            column.Spacing(12); // Ridotto da 20 a 12

                            // Intestazione solo sulla prima pagina
                            column.Item().AlignCenter().Text("CERTIFICATO DIABETE - ESENZIONE INPS")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#2C3E50");

                            column.Item().PaddingBottom(5); // Ridotto da 10 a 5

                            column.Item().ShowEntire().Element(c => RenderSezioneMedico(c, medicoData));
                            column.Item().ShowEntire().Element(c => RenderSezionePaziente(c, patientData));
                            column.Item().ShowEntire().Element(c => RenderSezioneDiabete(c, patientData));

                            if (compensationData != null && compensationData.DatiDisponibili)
                                column.Item().ShowEntire().Element(c => RenderSezioneCompensoGlicemico(c, compensationData));

                            if (therapyData != null && therapyData.HasTherapies)
                                column.Item().ShowEntire().Element(c => RenderSezioneTerapiaAntidiabetica(c, therapyData));

                            if (complications != null && complications.Count > 0)
                                column.Item().ShowEntire().Element(c => RenderSezioneComplicanze(c, complications));

                            if (!string.IsNullOrWhiteSpace(noteMedico))
                                column.Item().ShowEntire().Element(c => RenderSezioneNote(c, noteMedico));
                        });

                        page.Footer().AlignCenter().Column(col =>
                        {
                            col.Item().Text(t =>
                            {
                                t.Span("Documento generato il ").FontSize(9).FontColor("#7F8C8D");
                                t.Span(System.DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold().FontSize(9).FontColor("#7F8C8D");
                                t.Span(" - v1.0.7").FontSize(9).FontColor("#7F8C8D");
                            });
                        });
                    });
                }).GeneratePdf(outputPath);

                return true;
            }
            catch { return false; }
        }

        private void RenderSezioneMedico(IContainer container, MedicoData medico)
        {
            container.Column(column =>
            {
                column.Item().Background("#3498DB").Padding(8).Text("DATI MEDICO CERTIFICATORE").Bold().FontSize(12).FontColor("#FFFFFF"); // Modificato da "DATI MEDICO"
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(12).Column(content =>
                {
                    content.Spacing(6);
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Medico:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(medico.NomeCompleto ?? "N/D");
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Indirizzo:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(medico.Indirizzo ?? "N/D");
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Telefono:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(medico.Telefono ?? "N/D");
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Email:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(medico.Email ?? "N/D");
                    });
                });
            });
        }

        private void RenderSezionePaziente(IContainer container, PatientData patient)
        {
            container.Column(column =>
            {
                column.Item().Background("#27AE60").Padding(8).Text("PAZIENTE").Bold().FontSize(12).FontColor("#FFFFFF");
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(12).Column(content =>
                {
                    content.Spacing(6);
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Paziente:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(patient.NomeCompleto ?? "N/D");
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Data di nascita:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(patient.DataNascita.ToString("dd/MM/yyyy"));
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Codice fiscale:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(patient.CodiceFiscale ?? "N/D");
                    });
                });
            });
        }

        private void RenderSezioneDiabete(IContainer container, PatientData patient)
        {
            container.Column(column =>
            {
                column.Item().Background("#E67E22").Padding(8).Text("DIABETE").Bold().FontSize(12).FontColor("#FFFFFF");
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(12).Column(content =>
                {
                    content.Spacing(6);
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Tipo di diabete:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(patient.TipoDiabete ?? "non specificato");
                    });
                    content.Item().Row(row =>
                    {
                        row.ConstantItem(120).Text("Data di diagnosi:").Bold().FontColor("#34495E");
                        row.RelativeItem().Text(patient.DataInizioDiabete.ToString("dd/MM/yyyy"));
                    });
                });
            });
        }

        private void RenderSezioneCompensoGlicemico(IContainer container, GlycemicCompensationData glycemicData)
        {
            container.Column(column =>
            {
                column.Item().Background("#9B59B6").Padding(8).Text("COMPENSO GLICEMICO").Bold().FontSize(12).FontColor("#FFFFFF");
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(12).Column(content =>
                {
                    content.Spacing(6);
                    if (!string.IsNullOrEmpty(glycemicData.TipoTrattamento))
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("In trattamento:").Bold().FontColor("#34495E");
                            row.RelativeItem().Text(glycemicData.TipoTrattamento);
                        });
                    if (glycemicData.HbPercento > 0)
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("HbA1c:").Bold().FontColor("#34495E");
                            row.RelativeItem().Text(glycemicData.GetValoreFormattato());
                        });
                    if (!string.IsNullOrEmpty(glycemicData.DataPrelievo))
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("Data prelievo:").Bold().FontColor("#34495E");
                            row.RelativeItem().Text(glycemicData.DataPrelievo);
                        });
                    if (!string.IsNullOrEmpty(glycemicData.ValutazioneCompenso))
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("Compenso metabolico:").Bold().FontColor("#34495E");
                            row.RelativeItem().Text(glycemicData.ValutazioneCompenso)
                                .FontColor(GetCompensoColor(glycemicData.ValutazioneCompenso)).Bold();
                        });
                });
            });
        }

        private string GetCompensoColor(string valutazione)
        {
            if (valutazione.Contains("buon", System.StringComparison.OrdinalIgnoreCase)) return "#27AE60";
            if (valutazione.Contains("mediocre", System.StringComparison.OrdinalIgnoreCase)) return "#F39C12";
            if (valutazione.Contains("scompensato", System.StringComparison.OrdinalIgnoreCase)) return "#E74C3C";
            return "#34495E";
        }

        private void RenderSezioneComplicanze(IContainer container, List<DiabetesComplicationData> complications)
        {
            container.Column(column =>
            {
                column.Item().Background("#8E44AD").Padding(8).Text("COMPLICANZE").Bold().FontSize(12).FontColor("#FFFFFF");
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);  // Colonna "Complicanza" (invariata)
                        columns.RelativeColumn(1.5f);  // Colonna "Presente" (aumentata da 1 a 1.5)
                        columns.RelativeColumn(1.5f);  // Colonna "Grado" (aumentata da 1 a 1.5)
                        columns.RelativeColumn(2.5f);  // Colonna "Note" (ridotta da 4 a 2.5)
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeaderStyle).Text("Complicanza").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Presente").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Grado").Bold();
                        header.Cell().Element(CellHeaderStyle).Text("Note").Bold();
                    });

                    foreach (var c in complications)
                    {
                        table.Cell().Element(CellDataStyle).Text(c.ComplicationType).FontSize(10).FontColor("#34495E");
                        table.Cell().Element(CellDataStyle).Text(c.IsPresent).FontSize(10).Bold().FontColor(GetColorPresence(c.IsPresent));
                        table.Cell().Element(CellDataStyle).Text(string.IsNullOrWhiteSpace(c.Severity) ? "-" : c.Severity)
                            .FontSize(10).Bold().FontColor(string.IsNullOrWhiteSpace(c.Severity) ? "#95A5A6" : GetColorSeverity(c.Severity));
                        table.Cell().Element(CellDataStyle).Text(c.Notes ?? "-").FontSize(9).FontColor("#34495E");
                    }
                });

                column.Item().PaddingTop(6).Column(noteColumn =>
                {
                    noteColumn.Item().Text("Criteri di classificazione:").FontSize(8).Bold().FontColor("#34495E");
                    noteColumn.Item().PaddingTop(3).Text(text =>
                    {
                        text.Span("• Lieve: ").FontSize(7).Bold().FontColor("#27AE60");
                        text.Span("nefropatia (VFG 90-60 ml/min); retinopatia non proliferante; neuropatia lieve; arteriopatia con claudicatio <500 m.").FontSize(7).FontColor("#34495E");
                    });
                    noteColumn.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("• Moderato: ").FontSize(7).Bold().FontColor("#F39C12");
                        text.Span("nefropatia (VFG 60-30 ml/min); retinopatia con edema; neuropatia dolorosa; arteriopatia con claudicatio <300 m.").FontSize(7).FontColor("#34495E");
                    });
                    noteColumn.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("• Grave: ").FontSize(7).Bold().FontColor("#E74C3C");
                        text.Span("nefropatia (VFG <30 ml/min); retinopatia proliferante; neuropatia con piede diabetico; arteriopatia con dolore a riposo.").FontSize(7).FontColor("#34495E");
                    });
                });
            });
        }

        /// <summary>
        /// Renderizza la sezione "Note Medico" (v1.0.5)
        /// </summary>
        private void RenderSezioneNote(IContainer container, string noteMedico)
        {
            container.Column(column =>
            {
                column.Item().Background("#16A085").Padding(8).Text("NOTE MEDICO").Bold().FontSize(12).FontColor("#FFFFFF");
                column.Item().Border(1).BorderColor("#BDC3C7").Padding(12).Column(content =>
                {
                    content.Item().Text(noteMedico)
                        .FontSize(10)
                        .FontColor("#34495E")
                        .LineHeight(1.3f); // Ridotto da 1.5f a 1.3f
                });
            });
        }

        private IContainer CellHeaderStyle(IContainer c) => c.Border(1).BorderColor("#BDC3C7").Background("#ECF0F1").Padding(6).AlignCenter().AlignMiddle(); // Ridotto da 8 a 6
        private IContainer CellDataStyle(IContainer c) => c.Border(1).BorderColor("#E5E7EB").Padding(6).AlignLeft().AlignMiddle(); // Ridotto da 8 a 6
        private string GetColorPresence(string p) => p switch { "sì" => "#E74C3C", "no" => "#27AE60", "N.V" => "#95A5A6", _ => "#34495E" };
        private string GetColorSeverity(string s) => s switch { "Lieve" => "#27AE60", "Moderato" => "#F39C12", "Grave" => "#E74C3C", _ => "#34495E" };
    }
}
