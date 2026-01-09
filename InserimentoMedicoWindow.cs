using DMInps.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DMInps
{
    public partial class InserimentoMedicoWindow : Window
    {
        public MedicoSelectionData? MedicoInserito { get; private set; }

        // Path per il file JSON dei medici
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dgzani"
        );
        private static readonly string MediciJsonPath = Path.Combine(AppDataPath, "medici.json");

        public InserimentoMedicoWindow()
        {
            InitializeComponent();

            // Verifica se il file medici.json esiste e abilita/disabilita il pulsante
            CheckCaricaDatiButtonState();

            TxtNomeUtente.Focus();
        }

        /// <summary>
        /// Verifica se il file medici.json esiste e abilita/disabilita il pulsante "Carica dati salvati"
        /// </summary>
        private void CheckCaricaDatiButtonState()
        {
            bool fileExists = File.Exists(MediciJsonPath);
            BtnCaricaDati.IsEnabled = fileExists;

            if (!fileExists)
            {
                BtnCaricaDati.ToolTip = "Nessun file di dati salvati trovato";
                BtnCaricaDati.Opacity = 0.5;
            }
            else
            {
                BtnCaricaDati.ToolTip = "Carica i dati del medico dal file salvato";
                BtnCaricaDati.Opacity = 1.0;
            }
        }

        /// <summary>
        /// Pulsante "Carica dati salvati" - Popola i campi con i dati dal file medici.json
        /// </summary>
        private void BtnCaricaDati_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(MediciJsonPath))
                {
                    MessageBox.Show(
                        "Il file dei dati salvati non esiste.",
                        "File non trovato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string jsonString = File.ReadAllText(MediciJsonPath);
                var mediciData = JsonSerializer.Deserialize<MediciListData>(jsonString);

                if (mediciData?.Medici == null || mediciData.Medici.Count == 0)
                {
                    MessageBox.Show(
                        "Il file dei dati salvati è vuoto o non contiene medici.",
                        "Nessun dato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Prende il primo medico dalla lista (o l'ultimo inserito)
                var medico = mediciData.Medici[0];

                // Popola i campi del form
                TxtNomeUtente.Text = medico.NomePass;
                TxtCodiceMedico.Text = medico.UserId;
                TxtCognomeNome.Text = medico.NomeCompleto;
                TxtIndirizzo.Text = medico.Indirizzo;
                TxtTelefono.Text = medico.Telefono;
                TxtEmail.Text = medico.Email;

                MessageBox.Show(
                    $"Dati caricati correttamente.\n\nMedico: {medico.NomeCompleto}",
                    "Dati caricati",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Errore durante il caricamento dei dati:\n{ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Pulsante "Salva" - Salva i dati nel file medici.json
        /// </summary>
        private void BtnSalva_Click(object sender, RoutedEventArgs e)
        {
            // Validazione campi obbligatori
            if (string.IsNullOrWhiteSpace(TxtNomeUtente.Text))
            {
                MessageBox.Show(
                    "Il Nome login Millewin è obbligatorio.",
                    "Campo mancante",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtNomeUtente.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtCodiceMedico.Text))
            {
                MessageBox.Show(
                    "Il Codice Medico è obbligatorio.",
                    "Campo mancante",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtCodiceMedico.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtCognomeNome.Text))
            {
                MessageBox.Show(
                    "Il Cognome e Nome è obbligatorio.",
                    "Campo mancante",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtCognomeNome.Focus();
                return;
            }

            // Verifica se il file esiste già e chiede conferma per sovrascrittura
            if (File.Exists(MediciJsonPath))
            {
                var result = MessageBox.Show(
                    "Esiste già un file con i dati del medico.\n\nVuoi sovrascriverlo con i nuovi dati?",
                    "Conferma sovrascrittura",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            try
            {
                // Crea la directory se non esiste
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                // Crea l'oggetto MedicoSelectionData
                MedicoInserito = new MedicoSelectionData
                {
                    NomePass = TxtNomeUtente.Text.Trim(),
                    UserId = TxtCodiceMedico.Text.Trim(),
                    NomeCompleto = TxtCognomeNome.Text.Trim(),
                    Indirizzo = string.IsNullOrWhiteSpace(TxtIndirizzo.Text) ? "" : TxtIndirizzo.Text.Trim(),
                    Telefono = string.IsNullOrWhiteSpace(TxtTelefono.Text) ? "" : TxtTelefono.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? "" : TxtEmail.Text.Trim()
                };

                // Salva in JSON
                var mediciData = new MediciListData
                {
                    Medici = new System.Collections.Generic.List<MedicoSelectionData> { MedicoInserito },
                    DataAggiornamento = DateTime.Now
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string jsonString = JsonSerializer.Serialize(mediciData, options);
                File.WriteAllText(MediciJsonPath, jsonString);

                MessageBox.Show(
                    $"Dati del medico salvati correttamente.\n\n" +
                    $"Medico: {MedicoInserito.NomeCompleto}\n" +
                    $"Codice: {MedicoInserito.UserId}\n\n" +
                    $"File salvato in:\n{MediciJsonPath}",
                    "Salvataggio completato",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Errore durante il salvataggio dei dati:\n{ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Pulsante "Annulla" - Chiude l'applicazione con messaggio
        /// </summary>
        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Sei sicuro di voler annullare?\n\n" +
                "Senza i dati del medico certificatore non è possibile utilizzare l'applicazione.\n\n" +
                "L'applicazione verrà chiusa.",
                "Conferma annullamento",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
