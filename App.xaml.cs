using System.Windows;

namespace DMInps
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// Punto di ingresso dell'applicazione WPF
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Costruttore dell'applicazione
        /// </summary>
        public App()
        {
            // Gestione errori non gestiti
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// Gestisce le eccezioni non gestite nell'applicazione
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Eccezione non gestita: {e.Exception.Message}");
            
            MessageBox.Show(
                $"Si è verificato un errore imprevisto:\n\n{e.Exception.Message}\n\n" +
                "L'applicazione verrà chiusa. Contattare il supporto tecnico se il problema persiste.",
                "Errore Critico",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Marca l'eccezione come gestita per evitare il crash dell'applicazione
            e.Handled = true;
            
            // Chiudi l'applicazione
            Application.Current.Shutdown();
        }
    }
}
