using DMInps.Models;
using System.Windows;

namespace DMInps
{
    public partial class FileNameFormatWindow : Window
    {
        public FileNameFormat FileNameFormat { get; private set; }

        public FileNameFormatWindow(FileNameFormat currentFormat)
        {
            InitializeComponent();
            FileNameFormat = new FileNameFormat
            {
                IncludiCodiceMedico = currentFormat.IncludiCodiceMedico,
                IncludiNomeMedico = currentFormat.IncludiNomeMedico,
                IncludiNomePaziente = currentFormat.IncludiNomePaziente,
                IncludiCodiceFiscale = currentFormat.IncludiCodiceFiscale,
                IncludiDataOra = currentFormat.IncludiDataOra,
                Separatore = currentFormat.Separatore
            };

            DataContext = FileNameFormat;
        }

        private void BtnSalva_Click(object sender, RoutedEventArgs e)
        {
            if (!FileNameFormat.IsValid())
            {
                MessageBox.Show(
                    "Almeno uno tra 'Nome Paziente' o 'Codice Fiscale' deve essere selezionato.",
                    "Validazione",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdatePreview()
        {
            if (FileNameFormat == null || TxtPreview == null)
                return;
                
            string example = FileNameFormat.GenerateFileName(
                "DR001", "Rossi Mario", "Bianchi Giovanni", "BNCGNN80A01H501Z");
            TxtPreview.Text = example;
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }
    }
}