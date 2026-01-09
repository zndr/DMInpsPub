using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace DMInps
{
    public partial class PrerequisiteWindow : Window
    {
        public PrerequisiteWindow()
        {
            InitializeComponent();
            LoadContent();
        }

        private void LoadContent()
        {
            var flowDoc = new FlowDocument();
            
            // Titolo
            var titlePara = new Paragraph(new Run("Prerequisiti per il corretto funzionamento"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            flowDoc.Blocks.Add(titlePara);

            // Sezione Millewin
            AddSection(flowDoc, "Installazione Software",
                "• Il software funziona esclusivamente se sul PC è installato Millewin, poiché estrae i dati dal suo database\n" +
                "• Il medico certificatore deve essere un titolare in Millewin");

            // Sezione Estrazione dati
            AddSection(flowDoc, "Estrazione dei Dati",
                "");

            AddSubSection(flowDoc, "Paziente:",
                "• Deve essere un paziente NON deceduto\n" +
                "• NON revocato (o con data di revoca posta nel futuro)\n" +
                "• Deve essere in convenzione SSN\n" +
                "• Non estrae dati da pazienti in libera professione");

            AddSubSection(flowDoc, "Terapia:",
                "• Estrae solo i farmaci antidiabetici contrassegnati come 'continuativi' in Millewin");

            AddSubSection(flowDoc, "Complicanze:",
                "• Estrae solo quelle registrate autonomamente come 'problema'\n" +
                "• Il problema deve essere contrassegnato come 'Cronico' (?) o 'Attivo' (?)\n" +
                "• Consultare la tabella delle complicanze supportate: ");

            // Link al file complicanze
            var linkPara = new Paragraph();
            var hyperlink = new Hyperlink(new Run("complicanze.md"))
            {
                NavigateUri = new Uri(GetComplicanzeFilePath(), UriKind.Absolute)
            };
            hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            linkPara.Inlines.Add(hyperlink);
            flowDoc.Blocks.Add(linkPara);

            TxtContent.Document = flowDoc;
        }

        private void AddSection(FlowDocument doc, string title, string content)
        {
            var titlePara = new Paragraph(new Run(title))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            doc.Blocks.Add(titlePara);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var contentPara = new Paragraph(new Run(content))
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };
                doc.Blocks.Add(contentPara);
            }
        }

        private void AddSubSection(FlowDocument doc, string title, string content)
        {
            var titleRun = new Run(title) { FontWeight = FontWeights.SemiBold };
            var para = new Paragraph();
            para.Inlines.Add(titleRun);
            para.Inlines.Add(new LineBreak());
            para.Inlines.Add(new Run(content));
            para.Margin = new Thickness(15, 0, 0, 10);
            doc.Blocks.Add(para);
        }

        private string GetComplicanzeFilePath()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "dgzani", "DMInps", "Files", "complicanze.md");

            // Crea il file se non esiste
            if (!File.Exists(appData))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(appData));
                CreateComplicanzeFile(appData);
            }

            return appData;
        }

        private void CreateComplicanzeFile(string path)
        {
            var content = @"# Complicanze Diabetiche Supportate

## Elenco Complicanze

### Complicanze Microvascolare
1. **Nefropatia Diabetica**
   - Lieve: VFG 90-60 ml/min
   - Moderato: VFG 60-30 ml/min
   - Grave: VFG <30 ml/min

2. **Retinopatia Diabetica**
   - Lieve: non proliferante
   - Moderato: con edema maculare
   - Grave: proliferante

3. **Neuropatia Diabetica**
   - Lieve: sensitiva periferica
   - Moderato: dolorosa
   - Grave: con piede diabetico

### Complicanze Macrovascolari
4. **Arteriopatia Periferica**
   - Lieve: claudicatio >500m
   - Moderato: claudicatio <300m
   - Grave: dolore a riposo

5. **Cardiopatia Ischemica**
6. **Malattia Cerebrovascolare**

## Codici ICD Supportati
Consultare il database Millewin per i codici specifici delle complicanze.
";

            File.WriteAllText(path, content);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsolutePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossibile aprire il file:\n{ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}