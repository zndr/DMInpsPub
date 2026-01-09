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
            TxtVersione.Text = MainWindow.APP_VERSION;
            TxtData.Text = DateTime.Now.ToString("dd/MM/yyyy");
            TxtAutore.Text = "Dr. Dario Giorgio Zani";

            TxtNoteVersione.Text =
                $"Novita' versione {MainWindow.APP_VERSION}:\n\n" +
                "• Controllo automatico aggiornamenti all'avvio\n" +
                "• Notifica silenziosa solo se disponibile nuova versione\n" +
                "• Download diretto dell'installer dalla pagina release GitHub\n" +
                "• Persistenza del medico certificatore selezionato al riavvio\n" +
                "• Versione centralizzata in unico punto del codice\n";
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
