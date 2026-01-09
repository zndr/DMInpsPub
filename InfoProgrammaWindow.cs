using System;
using System.Windows;

namespace DMInps
{
    public partial class InfoProgrammaWindow : Window
    {
        public InfoProgrammaWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            TxtVersione.Text = "1.0.7";
            TxtData.Text = DateTime.Now.ToString("dd/MM/yyyy");
            TxtAutore.Text = "Dr. Dario Giorgio Zani";

            TxtNoteVersione.Text =
                "Novita' versione 1.0.7:\n\n" +
                "• Inserimento manuale dati medico quando database non disponibile\n" +
                "• Fallback automatico su file JSON locale per i dati medici\n" +
                "• Nuovo titolo applicazione: 'DMInps - generatore relazione diabete per INPS'\n" +
                "• Finestra 'Formato Nome File' ridimensionabile con barra di scorrimento\n" +
                "• Sezione 'Separatore' sempre visibile nella finestra formato nome file\n" +
                "• Corretta anteprima nome file (rimossi riferimenti ai controlli WPF)\n" +
                "• Rimossa voce menu 'Aiuto -> Debug Info' (non piu' necessaria)\n" +
                "• Rimozione codice obsoleto (GetDoctorCode, GetMedicoDataAsync)\n" +
                "• Correzione query con COALESCE per campi NULL\n" +
                "• Migliorata gestione errori connessione database\n";
        }

        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnStorico_Click(object sender, RoutedEventArgs e)
        {
            var storicoWindow = new StoricoVersioniWindow();
            storicoWindow.Owner = this;
            storicoWindow.ShowDialog();
        }
    }
}
