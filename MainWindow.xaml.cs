using DMInps.Models;
using DMInps.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace DMInps
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Versione corrente dell'applicazione (centralizzata)
        /// </summary>
        public const string APP_VERSION = "1.0.9";

        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;
        private List<MedicoSelectionData>? _mediciList;
        private string _selectedUserId = string.Empty;
        private string _lastGeneratedPdfPath = string.Empty;
        private string _customOutputFolder = string.Empty;
        private FileNameFormat _fileNameFormat = new FileNameFormat();

        public MainWindow()
        {
            InitializeComponent();
            _databaseService = DatabaseService.Instance;
            _pdfService = new PdfService();
            Title = $"DMInps - Generatore Relazione Diabete INPS v{APP_VERSION}";
            LoadSettings();
        }

        /// <summary>
        /// Carica le impostazioni salvate
        /// </summary>
        private void LoadSettings()
        {
            // Carica la cartella personalizzata se presente
            string settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DMInps", "settings.txt");

            if (File.Exists(settingsPath))
            {
                try
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("OutputFolder="))
                            _customOutputFolder = line.Substring("OutputFolder=".Length);
                        else if (line.StartsWith("FileNameFormat="))
                            _fileNameFormat = FileNameFormat.FromString(line.Substring("FileNameFormat=".Length));
                        else if (line.StartsWith("LastSelectedMedico="))
                            _selectedUserId = line.Substring("LastSelectedMedico=".Length);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Salva le impostazioni
        /// </summary>
        private void SaveSettings()
        {
            string settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DMInps");

            Directory.CreateDirectory(settingsPath);

            var lines = new List<string>
            {
                $"OutputFolder={_customOutputFolder}",
                $"FileNameFormat={_fileNameFormat}",
                $"LastSelectedMedico={_selectedUserId}"
            };

            File.WriteAllLines(Path.Combine(settingsPath, "settings.txt"), lines);
        }

        /// <summary>
        /// Ottiene il percorso della cartella di output
        /// </summary>
        private string GetOutputFolder()
        {
            if (!string.IsNullOrEmpty(_customOutputFolder) && Directory.Exists(_customOutputFolder))
                return _customOutputFolder;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DMInps");
        }

        #region Menu File

        /// <summary>
        /// Menu: Nuovo certificato (pulisce i campi)
        /// </summary>
        private void MenuNuovoCertificato_Click(object sender, RoutedEventArgs e)
        {
            PatientCodeTextBox.Clear();
            TxtNoteMedico.Clear();
            PatientCodeTextBox.Focus();
            UpdateStatus("Pronto per nuovo certificato");
        }

        /// <summary>
        /// Menu: Mostra certificati recenti
        /// </summary>
        private void MenuMostraRecenti_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string outputPath = GetOutputFolder();
                if (!Directory.Exists(outputPath))
                {
                    MessageBox.Show("Nessun certificato generato ancora.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var files = Directory.GetFiles(outputPath, "DMInps_*.pdf")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Take(10)
                    .ToList();

                if (files.Count == 0)
                {
                    MessageBox.Show("Nessun certificato trovato.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var recentWindow = new RecentFilesWindow(files);
                recentWindow.Owner = this;
                recentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel recupero dei file recenti:\n{ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Menu: Apri cartella certificati
        /// </summary>
        private void MenuApriCartella_Click(object sender, RoutedEventArgs e)
        {
            ApriCartellaOutput();
        }

        /// <summary>
        /// Menu: Cambia cartella certificati
        /// </summary>
        private void MenuCambiaCartella_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Seleziona la cartella dove salvare i certificati PDF",
                FileName = "Seleziona Cartella", // Default file name
                DefaultExt = ".folder",
                Filter = "Folder|*.folder"
            };

            if (dialog.ShowDialog() == true)
            {
                // Get the directory from the selected path
                _customOutputFolder = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                SaveSettings();
                MessageBox.Show($"Cartella impostata su:\n{_customOutputFolder}",
                    "Cartella modificata", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Menu: Esci dall'applicazione
        /// </summary>
        private void MenuEsci_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Menu Opzioni

        /// <summary>
        /// Menu: Cambia medico certificatore
        /// </summary>
        private void MenuCambiaMedico_Click(object sender, RoutedEventArgs e)
        {
            if (_mediciList == null || _mediciList.Count == 0)
            {
                MessageBox.Show("Nessun medico disponibile. Aggiorna l'elenco medici.",
                    "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selezioneWindow = new SelezioneMedicoWindow(_mediciList, _selectedUserId);
            selezioneWindow.Owner = this;
            
            if (selezioneWindow.ShowDialog() == true)
            {
                var medicoSelezionato = selezioneWindow.MedicoSelezionato;
                if (medicoSelezionato != null)
                {
                    _selectedUserId = medicoSelezionato.UserId;
                    ComboMedici.SelectedItem = medicoSelezionato;
                    UpdateStatus($"✅ Medico certificatore cambiato: {medicoSelezionato.NomeCompleto}");
                }
            }
        }

        /// <summary>
        /// Menu: Configura formato nome file
        /// </summary>
        private void MenuNomeFile_Click(object sender, RoutedEventArgs e)
        {
            var formatWindow = new FileNameFormatWindow(_fileNameFormat);
            formatWindow.Owner = this;
            
            if (formatWindow.ShowDialog() == true)
            {
                _fileNameFormat = formatWindow.FileNameFormat;
                SaveSettings();
                UpdateStatus("Formato nome file aggiornato");
            }
        }

        /// <summary>
        /// Menu: Mostra prerequisiti
        /// </summary>
        private void MenuPrerequisiti_Click(object sender, RoutedEventArgs e)
        {
            var prerequisitiWindow = new PrerequisiteWindow();
            prerequisitiWindow.Owner = this;
            prerequisitiWindow.ShowDialog();
        }

        #endregion

        #region Menu Aiuto

        /// <summary>
        /// Menu: Mostra guida
        /// </summary>
        private void MenuGuida_Click(object sender, RoutedEventArgs e)
        {
            var guidaWindow = new GuidaWindow();
            guidaWindow.Owner = this;
            guidaWindow.ShowDialog();
        }

        /// <summary>
        /// Menu: Informazioni sul programma
        /// </summary>
        private void MenuInfoProgramma_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new InfoProgrammaWindow();
            infoWindow.Owner = this;
            infoWindow.ShowDialog();
        }

        /// <summary>
        /// Menu: Storico versioni
        /// </summary>
        /// <summary>
        /// Gestisce il click sul menu "Aiuto → Storico Versioni"
        /// Apre il file CHANGELOG.md con l'editor predefinito
        /// </summary>
        private void MenuStoricoVersioni_Click(object sender, RoutedEventArgs e)
            {
            try
                {
                string changelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG_v1.0.6.2.md");

                if (File.Exists(changelogPath))
                    {
                    // Apri il file con l'applicazione predefinita del sistema
                    var startInfo = new ProcessStartInfo
                        {
                        FileName = changelogPath,
                        UseShellExecute = true
                        };
                    Process.Start(startInfo);
                    }
                else
                    {
                    MessageBox.Show(
                        "File CHANGELOG.md non trovato.\n\n" +
                        $"Path cercato:\n{changelogPath}",
                        "Storico Versioni - File Non Trovato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show(
                    $"Errore durante l'apertura dello storico versioni:\n\n{ex.Message}",
                    "Errore Storico Versioni",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                }
            }

        /// <summary>
        /// Menu: Cerca aggiornamenti
        /// </summary>
        private async void MenuCercaAggiornamenti_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesManualAsync();
        }

        /// <summary>
        /// Controllo aggiornamenti manuale (da menu) - mostra sempre il risultato
        /// </summary>
        private async Task CheckForUpdatesManualAsync()
        {
            try
            {
                UpdateStatus("Controllo aggiornamenti in corso...");

                var result = await UpdateCheckService.Instance.CheckForUpdatesAsync(APP_VERSION);

                if (!result.Success)
                {
                    MessageBox.Show(
                        $"Impossibile verificare gli aggiornamenti.\n\n{result.ErrorMessage}",
                        "Errore Controllo Aggiornamenti",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    UpdateStatus("Controllo aggiornamenti fallito");
                    return;
                }

                if (result.IsUpdateAvailable)
                {
                    ShowUpdateAvailableDialog(result);
                }
                else
                {
                    // Determina il messaggio appropriato
                    string message;
                    string title;

                    int comparison = CompareVersions(APP_VERSION, result.LatestVersion);

                    if (comparison > 0)
                    {
                        // Versione corrente più recente di quella pubblicata (es. sviluppo)
                        message = $"Stai utilizzando una versione di sviluppo.\n\n" +
                                  $"Versione corrente: {APP_VERSION}\n" +
                                  $"Ultima versione pubblicata: {result.LatestVersion}";
                        title = "Versione di Sviluppo";
                    }
                    else
                    {
                        // Versioni uguali
                        message = $"L'applicazione e' aggiornata all'ultima versione.\n\n" +
                                  $"Versione: {APP_VERSION}";
                        title = "Nessun Aggiornamento";
                    }

                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateStatus("Controllo aggiornamenti completato");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Errore durante il controllo aggiornamenti:\n\n{ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                UpdateStatus("Errore controllo aggiornamenti");
            }
        }

        /// <summary>
        /// Controllo aggiornamenti automatico all'avvio (silenzioso)
        /// </summary>
        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                // Piccolo ritardo per non interferire con il caricamento iniziale
                await Task.Delay(2000);

                var result = await UpdateCheckService.Instance.CheckForUpdatesAsync(APP_VERSION);

                if (result.Success && result.IsUpdateAvailable)
                {
                    // Mostra notifica solo se c'e' un aggiornamento
                    ShowUpdateAvailableDialog(result);
                }
                // Se non c'e' aggiornamento o c'e' un errore, non mostrare nulla (silenzioso)
            }
            catch (Exception ex)
            {
                // Errori silenziosi all'avvio - solo log
                Debug.WriteLine($"[UpdateCheck] Errore controllo automatico: {ex.Message}");
            }
        }

        /// <summary>
        /// Confronta due versioni semantiche
        /// </summary>
        /// <returns>-1 se v1 minore di v2, 0 se uguali, 1 se v1 maggiore di v2</returns>
        private static int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
                var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

                int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    int v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                    int v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

                    if (v1Part < v2Part) return -1;
                    if (v1Part > v2Part) return 1;
                }

                return 0;
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Mostra la finestra di dialogo per un aggiornamento disponibile
        /// </summary>
        private void ShowUpdateAvailableDialog(UpdateCheckResult result)
        {
            var message = $"E' disponibile una nuova versione di DMInps!\n\n" +
                          $"Versione corrente: {APP_VERSION}\n" +
                          $"Nuova versione: {result.LatestVersion}\n\n";

            if (!string.IsNullOrEmpty(result.ReleaseNotes))
            {
                // Prendi solo le prime righe delle note
                var notesPreview = string.Join("\n", result.ReleaseNotes.Split('\n').Take(5));
                if (result.ReleaseNotes.Split('\n').Length > 5)
                    notesPreview += "\n...";
                message += $"Note di rilascio:\n{notesPreview}\n\n";
            }

            message += "Vuoi scaricare l'aggiornamento?";

            var dialogResult = MessageBox.Show(
                message,
                "Aggiornamento Disponibile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (dialogResult == MessageBoxResult.Yes)
            {
                // Prova a scaricare l'installer direttamente, altrimenti apri la pagina release
                string urlToOpen = result.DownloadUrl ?? result.ReleasePageUrl ??
                    "https://github.com/zndr/DMInpsPub/releases";

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = urlToOpen,
                        UseShellExecute = true
                    });
                    UpdateStatus($"Apertura download aggiornamento...");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Impossibile aprire il link:\n{urlToOpen}\n\nErrore: {ex.Message}",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        #endregion

        /// <summary>
        /// Evento caricamento finestra - inizializza l'elenco medici
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Verifica connessione database...");
                bool isConnected = await _databaseService.TestConnectionAsync();

                if (!isConnected)
                {
                    UpdateStatus("⚠️ Database non raggiungibile. Tentativo caricamento da file locale...");

                    // Prova a caricare dal file JSON esistente
                    if (!await TryLoadMediciFromJsonOnly())
                    {
                        // Se fallisce anche il JSON, mostra finestra inserimento manuale
                        await ShowInserimentoManualeAsync("Impossibile connettersi al database e nessun dato locale disponibile.");
                    }
                    return;
                }

                UpdateStatus("✅ Connessione database OK. Caricamento elenco medici...");

                // Carica i medici dal database e salva in JSON
                try
                {
                    _mediciList = await _databaseService.LoadAndSaveMediciAsync();
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Errore caricamento da DB: {dbEx.Message}");

                    // Prova a caricare dal file JSON esistente
                    if (!await TryLoadMediciFromJsonOnly())
                    {
                        await ShowInserimentoManualeAsync($"Errore nel caricamento dal database: {dbEx.Message}");
                    }
                    return;
                }

                if (_mediciList == null || _mediciList.Count == 0)
                {
                    await ShowInserimentoManualeAsync("Nessun medico trovato nel database.");
                    return;
                }

                // Popola la ComboBox
                ComboMedici.ItemsSource = _mediciList;

                // Seleziona il medico salvato nelle impostazioni, altrimenti il primo
                SelectSavedMedico();

                UpdateStatus($"✅ Caricati {_mediciList.Count} medici. Seleziona il medico e inserisci il codice fiscale.");

                // Controllo aggiornamenti in background (fire-and-forget, silenzioso)
                _ = CheckForUpdatesOnStartupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Errore generale Window_Loaded: {ex.Message}");

                // Ultimo tentativo: inserimento manuale
                await ShowInserimentoManualeAsync($"Errore imprevisto: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta di caricare i medici solo dal file JSON locale (senza database)
        /// </summary>
        private async Task<bool> TryLoadMediciFromJsonOnly()
        {
            try
            {
                string jsonPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "dgzani", "medici.json");

                if (!System.IO.File.Exists(jsonPath))
                {
                    return false;
                }

                string jsonString = await System.IO.File.ReadAllTextAsync(jsonPath);
                var mediciData = System.Text.Json.JsonSerializer.Deserialize<Models.MediciListData>(jsonString);

                if (mediciData?.Medici == null || mediciData.Medici.Count == 0)
                {
                    return false;
                }

                _mediciList = mediciData.Medici;
                ComboMedici.ItemsSource = _mediciList;

                // Seleziona il medico salvato nelle impostazioni, altrimenti il primo
                SelectSavedMedico();

                UpdateStatus($"✅ Caricati {_mediciList.Count} medici da file locale.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Errore caricamento JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Seleziona il medico salvato nelle impostazioni, altrimenti il primo della lista
        /// </summary>
        private void SelectSavedMedico()
        {
            if (_mediciList == null || _mediciList.Count == 0)
                return;

            // Se c'è un medico salvato, cerca di selezionarlo
            if (!string.IsNullOrEmpty(_selectedUserId))
            {
                var medicoSalvato = _mediciList.FirstOrDefault(m => m.UserId == _selectedUserId);
                if (medicoSalvato != null)
                {
                    ComboMedici.SelectedItem = medicoSalvato;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Medico salvato selezionato: {medicoSalvato.NomeCompleto}");
                    return;
                }
            }

            // Altrimenti seleziona il primo
            ComboMedici.SelectedIndex = 0;
        }

        /// <summary>
        /// Mostra la finestra per l'inserimento manuale del medico
        /// </summary>
        private async Task ShowInserimentoManualeAsync(string motivo)
        {
            UpdateStatus("⚠️ Richiesto inserimento manuale dati medico...");

            var inserimentoWindow = new InserimentoMedicoWindow();
            inserimentoWindow.Owner = this;

            if (inserimentoWindow.ShowDialog() == true && inserimentoWindow.MedicoInserito != null)
            {
                try
                {
                    // Salva il medico nel file JSON
                    await _databaseService.SaveMedicoManualeAsync(inserimentoWindow.MedicoInserito);

                    // Ricarica la lista
                    _mediciList = new List<Models.MedicoSelectionData> { inserimentoWindow.MedicoInserito };
                    ComboMedici.ItemsSource = _mediciList;
                    ComboMedici.SelectedIndex = 0;

                    UpdateStatus($"✅ Medico {inserimentoWindow.MedicoInserito.NomeCompleto} configurato correttamente.");

                    MessageBox.Show(
                        $"Dati del medico salvati correttamente.\n\n" +
                        $"Medico: {inserimentoWindow.MedicoInserito.NomeCompleto}\n" +
                        $"Codice: {inserimentoWindow.MedicoInserito.UserId}",
                        "Configurazione completata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Errore nel salvataggio dei dati:\n{ex.Message}",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    UpdateStatus($"❌ Errore salvataggio: {ex.Message}");
                }
            }
            else
            {
                // L'utente ha annullato
                MessageBox.Show(
                    "Senza i dati del medico certificatore non è possibile utilizzare l'applicazione.\n\n" +
                    "L'applicazione verrà chiusa.",
                    "Operazione annullata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Gestisce la selezione del medico dalla ComboBox
        /// </summary>
        private void ComboMedici_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboMedici.SelectedItem is MedicoSelectionData medicoSelezionato)
            {
                _selectedUserId = medicoSelezionato.UserId;
                UpdateStatus($"✅ Medico selezionato: {medicoSelezionato.NomeCompleto}");
                ValidateForm();

                // Salva automaticamente il medico selezionato per riproporlo al riavvio
                SaveSettings();
            }
        }

        /// <summary>
        /// Aggiorna l'elenco medici dal database
        /// </summary>
        private async void BtnRefreshMedici_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRefreshMedici.IsEnabled = false;
                UpdateStatus("🔄 Aggiornamento elenco medici...");

                _mediciList = await _databaseService.LoadAndSaveMediciAsync();
                ComboMedici.ItemsSource = null;
                ComboMedici.ItemsSource = _mediciList;

                // Seleziona il medico salvato nelle impostazioni, altrimenti il primo
                SelectSavedMedico();

                UpdateStatus($"✅ Elenco aggiornato: {_mediciList.Count} medici caricati.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Errore nell'aggiornamento:\n{ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                UpdateStatus($"❌ Errore aggiornamento: {ex.Message}");
            }
            finally
            {
                BtnRefreshMedici.IsEnabled = true;
            }
        }

        /// <summary>
        /// Validazione codice fiscale al cambio testo
        /// </summary>
        private void PatientCodeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        /// <summary>
        /// Valida il form e abilita/disabilita il pulsante Genera
        /// </summary>
        private void ValidateForm()
        {
            string codiceFiscale = PatientCodeTextBox.Text.Trim();
            bool isValid = !string.IsNullOrWhiteSpace(codiceFiscale) && 
                           codiceFiscale.Length == 16 && 
                           !string.IsNullOrWhiteSpace(_selectedUserId);
            
            GenerateButton.IsEnabled = isValid;
        }

        /// <summary>
        /// Genera il certificato PDF
        /// </summary>
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GenerateButton.IsEnabled = false;
                
                UpdateStatus("Recupero dati medico certificatore selezionato...");

                if (string.IsNullOrEmpty(_selectedUserId))
                {
                    MessageBox.Show(
                        "Selezionare un medico certificatore prima di generare il PDF.",
                        "Attenzione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var medicoData = await _databaseService.GetMedicoDataFromJsonAsync(_selectedUserId);
                if (medicoData == null)
                {
                    MessageBox.Show(
                        "Dati del medico selezionato non trovati.",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                UpdateStatus("Recupero dati paziente...");

                string codiceFiscale = PatientCodeTextBox.Text.Trim().ToUpper();
                if (string.IsNullOrEmpty(codiceFiscale))
                {
                    MessageBox.Show(
                        "Inserire il codice fiscale del paziente.",
                        "Attenzione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                PatientData? patientData = null;
                bool diabeteNonTrovato = false;
                
                try
                {
                    patientData = await _databaseService.GetPatientDataAsync(codiceFiscale);
                }
                catch (InvalidOperationException ex) when (ex.Message == "DIABETE_NON_TROVATO")
                {
                    // Il paziente esiste ma non ha diabete
                    diabeteNonTrovato = true;
                    System.Diagnostics.Debug.WriteLine($"[MAIN] ❌ Paziente trovato ma senza diabete");
                }
                
                System.Diagnostics.Debug.WriteLine($"[MAIN] GetPatientDataAsync ritornato: {(patientData == null ? "NULL" : "VALIDO")}");
                
                if (diabeteNonTrovato)
                {
                    // Caso 2: Paziente trovato ma senza diagnosi di diabete
                    System.Diagnostics.Debug.WriteLine($"[MAIN] ❌ Mostro messaggio: paziente senza diabete");
                    MessageBox.Show(
                        "Il paziente cercato non ha una diagnosi di diabete registrata nella sua cartella clinica.\n\n" +
                        "Per generare il certificato INPS è necessario che sia presente:\n" +
                        "• Una diagnosi di diabete (codice ICD9: 250.x o 648.8x)\n" +
                        "• In stato ATTIVO\n" +
                        "• Con modalità Cronica (C) o Acuta (A)",
                        "Diagnosi di Diabete Non Trovata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    UpdateStatus("❌ Paziente senza diagnosi di diabete");
                    return;
                }
                
                if (patientData == null)
                {
                    // Caso 1: Paziente non trovato nel database
                    System.Diagnostics.Debug.WriteLine($"[MAIN] ❌ Mostro messaggio: paziente non trovato");
                    MessageBox.Show(
                        "Il codice fiscale digitato non corrisponde ad alcun paziente nel database.\n\n" +
                        "Verificare:\n" +
                        "• Che il codice fiscale sia corretto (16 caratteri)\n" +
                        "• Che il paziente sia registrato nel sistema\n" +
                        "• Che il paziente sia in carico ai medici dello studio\n" +
                        "• Che il paziente sia convenzionato SSN\n" +
                        "• Che il paziente non sia deceduto",
                        "Paziente Non Trovato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    UpdateStatus("❌ Codice fiscale non trovato nel database");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MAIN] ✓ Paziente valido: {patientData.NomeCompleto}, diabete: {patientData.TipoDiabete}");

                UpdateStatus("Analisi compenso glicemico...");

                var compensationData = await _databaseService.GetGlycemicCompensationDataAsync();

                if (!compensationData.DatiDisponibili)
                {
                    var result = MessageBox.Show(
                        $"{compensationData.MessaggioErrore}\n\n" +
                        "Vuoi continuare comunque con la generazione del PDF?\n" +
                        "(Il documento conterrà solo i dati disponibili)",
                        "Dati Incompleti",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        UpdateStatus("Generazione PDF annullata dall'utente");
                        return;
                    }
                }

                UpdateStatus("Recupero terapia antidiabetica...");
                var therapyData = await _databaseService.GetDiabetesTherapyAsync(patientData.CodiceMillewin);

                UpdateStatus("Recupero complicanze diabete...");
                List<DiabetesComplicationData>? complications = null;
                try
                {
                    complications = await Task.Run(() =>
                        _databaseService.GetCompleteComplicationsList(patientData.CodiceMillewin));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Errore recupero complicanze: {ex.Message}");
                    complications = null;
                }

                // Apri la finestra di modifica complicanze
                UpdateStatus("Modifica complicanze...");
                var editorWindow = new ComplicanzeEditorWindow(complications ?? new List<DiabetesComplicationData>());
                editorWindow.Owner = this;

                if (editorWindow.ShowDialog() != true || !editorWindow.Confirmed)
                {
                    UpdateStatus("Generazione PDF annullata dall'utente");
                    return;
                }

                // Usa le complicanze modificate dall'utente
                complications = editorWindow.ResultComplicanze;

                string noteMedico = TxtNoteMedico.Text.Trim();

                UpdateStatus("Generazione PDF in corso...");

                string outputFolder = GetOutputFolder();
                Directory.CreateDirectory(outputFolder);

                string fileName = _fileNameFormat.GenerateFileName(
                    medicoData.CodiceMedico,
                    medicoData.NomeCompleto,
                    patientData.NomeCompleto,
                    patientData.CodiceFiscale);

                string outputPath = Path.Combine(outputFolder, fileName);

                bool success = _pdfService.GeneratePdf(
                    medicoData,
                    patientData,
                    compensationData,
                    outputPath,
                    therapyData,
                    complications ?? new List<DiabetesComplicationData>(),
                    noteMedico);

                if (success)
                {
                    _lastGeneratedPdfPath = outputPath;
                    UpdateStatus($"PDF generato con successo: {outputPath}");

                    var openFileResult = MessageBox.Show(
                        $"PDF generato con successo!\n\n{outputPath}\n\nVuoi aprire il file?",
                        "Successo",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openFileResult == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = outputPath,
                            UseShellExecute = true
                        });
                    }

                    TxtNoteMedico.Clear();
                }
                else
                {
                    UpdateStatus("Errore durante la generazione del PDF");
                    MessageBox.Show(
                        "Si è verificato un errore durante la generazione del PDF.",
                        "Errore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Errore: {ex.Message}");
                MessageBox.Show(
                    $"Errore durante la generazione del PDF:\n{ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Apre la cartella contenente i PDF generati
        /// </summary>
        private void BtnApriCartella_Click(object sender, RoutedEventArgs e)
        {
            ApriCartellaOutput();
        }

        /// <summary>
        /// Chiude l'applicazione
        /// </summary>
        private void BtnChiudi_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Apre Windows Explorer nella cartella output
        /// </summary>
        private void ApriCartellaOutput()
        {
            try
            {
                string outputPath = GetOutputFolder();

                if (Directory.Exists(outputPath))
                {
                    Process.Start("explorer.exe", outputPath);
                }
                else
                {
                    MessageBox.Show("La cartella dei PDF non esiste ancora.",
                        "Cartella non trovata", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura della cartella:\n{ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aggiorna il messaggio di stato
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Debug.WriteLine($"[STATUS] {message}");
        }

        /// <summary>
        /// Gestisce il click sul menu "Aiuto → Debug Info"
        /// Versione robusta che cerca in più percorsi
        /// </summary>
        private void MenuDebugInfo_Click(object sender, RoutedEventArgs e)
            {
            try
                {
                string appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(appBaseDir, @"..\..\..\"));

                // Lista di percorsi possibili (in ordine di preferenza)
                string[] possiblePaths = new[]
                {
            // Con RuntimeIdentifier (win-x64)
            Path.Combine(projectRoot, @"debugs\debugEstraiCodiciMedici\bin\Debug\net8.0\win-x64\debugEstraiCodiciMedici.exe"),
            // Senza RuntimeIdentifier
            Path.Combine(projectRoot, @"debugs\debugEstraiCodiciMedici\bin\Debug\net8.0\debugEstraiCodiciMedici.exe"),
            // Release con RuntimeIdentifier
            Path.Combine(projectRoot, @"debugs\debugEstraiCodiciMedici\bin\Release\net8.0\win-x64\debugEstraiCodiciMedici.exe"),
            // Release senza RuntimeIdentifier
            Path.Combine(projectRoot, @"debugs\debugEstraiCodiciMedici\bin\Release\net8.0\debugEstraiCodiciMedici.exe"),
        };

                string? debugExePath = null;

                // Cerca il primo path che esiste
                foreach (var path in possiblePaths)
                    {
                    string fullPath = Path.GetFullPath(path);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] Controllo: {fullPath}");

                    if (File.Exists(fullPath))
                        {
                        debugExePath = fullPath;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ✓ Trovato: {fullPath}");
                        break;
                        }
                    }

                if (debugExePath == null)
                    {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ❌ Nessun eseguibile trovato");

                    MessageBox.Show(
                        "Il programma di diagnostica non è stato trovato in nessuno dei percorsi previsti.\n\n" +
                        "Percorsi controllati:\n" +
                        string.Join("\n", possiblePaths.Select(p => "• " + Path.GetFullPath(p))) + "\n\n" +
                        "Per compilarlo, eseguire:\n" +
                        "cd \"debugs\\debugEstraiCodiciMedici\"\n" +
                        "dotnet build -c Debug",
                        "Debug Info - File Non Trovato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                    }

                System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] Avvio: {debugExePath}");

                // Avvia il processo
                var startInfo = new ProcessStartInfo
                    {
                    FileName = debugExePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(debugExePath)
                    };

                Process.Start(startInfo);
                System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ✓ Avviato con successo");
                }
            catch (Exception ex)
                {
                System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ❌ Errore: {ex.Message}");

                MessageBox.Show(
                    $"Errore durante l'avvio del programma di diagnostica:\n\n{ex.Message}",
                    "Errore Debug Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                }
            }
        private bool CompileDebugProject(string projectRoot)
{
    try
    {
        string debugProjectPath = Path.Combine(
            projectRoot,
            "debugs",
            "debugEstraiCodiciMedici"
        );

        if (!Directory.Exists(debugProjectPath))
        {
            MessageBox.Show(
                $"La cartella del progetto di debug non esiste:\n\n{debugProjectPath}",
                "Debug Info - Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build -c Debug",
            WorkingDirectory = debugProjectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] Compilazione in: {debugProjectPath}");

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ✓ Compilazione riuscita");
                return true;
            }
            else
            {
                string error = process.StandardError.ReadToEnd();
                System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ❌ Errore compilazione: {error}");
                
                MessageBox.Show(
                    "Errore durante la compilazione del progetto di debug.\n\n" +
                    "Compilare manualmente il progetto 'debugEstraiCodiciMedici'.",
                    "Debug Info - Errore Compilazione",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
        
        return false;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG_INFO] ❌ Errore durante compilazione: {ex.Message}");
        MessageBox.Show(
            $"Errore durante la compilazione:\n\n{ex.Message}",
            "Debug Info - Errore",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
    }
}
        }
}
