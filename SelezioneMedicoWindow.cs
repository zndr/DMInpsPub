using DMInps.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DMInps
{
    public partial class SelezioneMedicoWindow : Window
    {
        public MedicoSelectionData? MedicoSelezionato { get; private set; }

        public SelezioneMedicoWindow(List<MedicoSelectionData> medici, string currentUserId)
        {
            InitializeComponent();
            ListMedici.ItemsSource = medici;
            
            // Seleziona il medico corrente
            var current = medici.FirstOrDefault(m => m.UserId == currentUserId);
            if (current != null)
                ListMedici.SelectedItem = current;
        }

        private void BtnSeleziona_Click(object sender, RoutedEventArgs e)
        {
            MedicoSelezionato = ListMedici.SelectedItem as MedicoSelectionData;
            DialogResult = true;
            Close();
        }

        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ListMedici_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListMedici.SelectedItem != null)
                BtnSeleziona_Click(sender, e);
        }
    }
}