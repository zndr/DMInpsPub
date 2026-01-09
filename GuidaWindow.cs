using System.Windows;

namespace DMInps
{
    public partial class GuidaWindow : Window
    {
        public GuidaWindow()
        {
            InitializeComponent();
        }

        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}