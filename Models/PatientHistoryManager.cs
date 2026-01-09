using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DMInps.Models
{
    /// <summary>
    /// Gestisce lo storico degli ultimi pazienti certificati
    /// </summary>
    public class PatientHistoryManager
    {
        private const int MAX_HISTORY_ITEMS = 10;
        private readonly string _historyFilePath;

        public PatientHistoryManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DMInps");
            
            Directory.CreateDirectory(appDataPath);
            _historyFilePath = Path.Combine(appDataPath, "patient_history.json");
        }

        /// <summary>
        /// Aggiunge un paziente allo storico
        /// </summary>
        public void AddPatient(string codiceFiscale, string nomeCompleto, DateTime timestamp)
        {
            try
            {
                var history = LoadHistory();
                
                // Rimuovi duplicati (stesso codice fiscale)
                history.RemoveAll(h => h.CodiceFiscale.Equals(codiceFiscale, StringComparison.OrdinalIgnoreCase));
                
                // Aggiungi in testa
                history.Insert(0, new PatientHistoryItem
                {
                    CodiceFiscale = codiceFiscale.ToUpperInvariant(),
                    NomeCompleto = nomeCompleto,
                    Timestamp = timestamp
                });
                
                // Mantieni solo ultimi 10
                if (history.Count > MAX_HISTORY_ITEMS)
                {
                    history = history.Take(MAX_HISTORY_ITEMS).ToList();
                }
                
                SaveHistory(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio storico: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottiene lo storico dei pazienti
        /// </summary>
        public List<PatientHistoryItem> GetHistory()
        {
            return LoadHistory();
        }

        /// <summary>
        /// Cancella lo storico
        /// </summary>
        public void ClearHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    File.Delete(_historyFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore cancellazione storico: {ex.Message}");
            }
        }

        private List<PatientHistoryItem> LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    return JsonSerializer.Deserialize<List<PatientHistoryItem>>(json) ?? new List<PatientHistoryItem>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore caricamento storico: {ex.Message}");
            }
            
            return new List<PatientHistoryItem>();
        }

        private void SaveHistory(List<PatientHistoryItem> history)
        {
            try
            {
                var json = JsonSerializer.Serialize(history, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore salvataggio storico: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Singolo elemento dello storico
    /// </summary>
    public class PatientHistoryItem
    {
        public string CodiceFiscale { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public string DisplayText => $"{NomeCompleto} ({CodiceFiscale}) - {Timestamp:dd/MM/yyyy HH:mm}";
    }
}
