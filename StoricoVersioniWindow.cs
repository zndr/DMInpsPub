using System.Windows;

namespace DMInps
{
    public partial class StoricoVersioniWindow : Window
    {
        public StoricoVersioniWindow()
        {
            InitializeComponent();
        }

        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}