using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace DMInps
{
    public partial class RecentFilesWindow : Window
    {
        private List<FileInfo> _files;

        public RecentFilesWindow(List<FileInfo> files)
        {
            InitializeComponent();
            _files = files;
            
            ListFiles.ItemsSource = files.Select(f => new
            {
                Nome = f.Name,
                Data = f.CreationTime.ToString("dd/MM/yyyy HH:mm"),
                Dimensione = $"{f.Length / 1024} KB",
                Path = f.FullName
            });
        }

        private void BtnApri_Click(object sender, RoutedEventArgs e)
        {
            if (ListFiles.SelectedItem == null) return;

            dynamic selected = ListFiles.SelectedItem;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selected.Path,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura del file:\n{ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ListFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnApri_Click(sender, e);
        }
    }
}