using DMInps.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace DMInps.Services
{
    public partial class DatabaseService
    {
        private static DatabaseService? _instance;
        private static readonly object _lock = new object();
        private string _connectionString; // Rimosso readonly per lazy initialization
        private string? _codiceMedicoCorrente;
        private string? _codiceMillewinDelPaziente;

        // Path per il file JSON dei medici (v1.0.7)
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dgzani"
        );
        private static readonly string MediciJsonPath = Path.Combine(AppDataPath, "medici.json");


        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DatabaseService();
                    }
                }
                return _instance;
            }
        }

        private DatabaseService()
        {
            // NON inizializziamo la connessione qui
            // Verrà inizializzata al primo uso (lazy initialization)
            _connectionString = string.Empty;
        }

        /// <summary>
        /// Inizializza la stringa di connessione leggendo dal Registry
        /// Chiamato al primo uso del database
        /// </summary>
        private void EnsureConnectionString()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return; // Già inizializzato

            try
            {
                string serverIp = RegistryService.GetDatabaseServerIp();
                _connectionString = $"Host={serverIp};Port=5432;Database=milleps;Username=dba;Password=sql;";
                System.Diagnostics.Debug.WriteLine("[DB] Connection string inizializzata correttamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] Errore inizializzazione: {ex.Message}");
                throw new InvalidOperationException(
                    "Impossibile inizializzare la connessione al database.\n\n" +
                    "Possibili cause:\n" +
                    "1. Millewin non è installato su questo computer\n" +
                    "2. Le chiavi di registro non sono accessibili\n" +
                    "3. È necessario eseguire l'applicazione come Amministratore\n\n" +
                    $"Dettagli tecnici: {ex.Message}", 
                    ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PatientData?> GetPatientDataAsync(string codiceFiscale)
        {
            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Carica tutti i codici medico dal JSON
                var mediciCodes = await GetAllMediciCodesAsync();

                if (mediciCodes.Count == 0)
                {
                    throw new Exception("Nessun medico disponibile per la ricerca");
                }

                // Costruisce la clausola IN dinamica
                string mediciInClause = BuildMediciInClause(mediciCodes);

                string query = $@"
                    SELECT pazienti.codice, 
                           pazienti.cognome||' '||pazienti.nome as Assistito, 
                           pazienti.nascita
                    FROM pazienti, nos_002
                    WHERE pazienti.codice = nos_002.codice
                        {mediciInClause}
                        AND pazienti.codice_fiscale = @cFisc
                        AND pazienti.pa_convenzione = 'S'
                        AND pazienti.decesso IS NULL
                        AND (nos_002.pa_drevoca IS NULL OR nos_002.pa_drevoca > CURRENT_DATE)";

                using var command = new NpgsqlCommand(query, connection);

                // Aggiunge i parametri dei medici
                AddMediciParameters(command, mediciCodes);
                command.Parameters.AddWithValue("@cFisc", codiceFiscale);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    _codiceMillewinDelPaziente = reader.GetString(0);
                    string nomeCompleto = reader.GetString(1);

                    var patient = new PatientData
                    {
                        CodiceMillewin = _codiceMillewinDelPaziente,
                        NomeCompleto = reader.GetString(1),
                        DataNascita = reader.GetDateTime(2),
                        CodiceFiscale = codiceFiscale
                    };

                    // Verifica la presenza del diabete
                    await GetDiabetesTypeAsync(patient);
                    
                    // CONTROLLO FONDAMENTALE: Se il diabete non è stato trovato, lancia un'eccezione specifica
                    if (string.IsNullOrEmpty(patient.TipoDiabete) || patient.DataInizioDiabete == default(DateTime))
                    {
                        System.Diagnostics.Debug.WriteLine($"[PAZIENTE] ❌ Paziente {patient.NomeCompleto} NON ha diabete valido:");
                        System.Diagnostics.Debug.WriteLine($"  - TipoDiabete: '{patient.TipoDiabete ?? "NULL"}'");
                        System.Diagnostics.Debug.WriteLine($"  - DataInizioDiabete: {patient.DataInizioDiabete}");
                        System.Diagnostics.Debug.WriteLine($"[PAZIENTE] Lancio eccezione specifica per mancanza diabete");
                        throw new InvalidOperationException("DIABETE_NON_TROVATO");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[PAZIENTE] ✓ Paziente valido con diabete: {patient.TipoDiabete}, data: {patient.DataInizioDiabete:dd/MM/yyyy}");

                    return patient;
                }
                
                // Paziente non trovato nel database
                System.Diagnostics.Debug.WriteLine($"[PAZIENTE] ❌ Nessun paziente trovato con CF: {codiceFiscale}");
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message == "DIABETE_NON_TROVATO")
            {
                // Rilancia l'eccezione specifica per gestirla nel MainWindow
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore paziente: {ex.Message}");
                throw;
            }
        }

        private async Task GetDiabetesTypeAsync(PatientData patient)
        {
            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query1 = @"
                    SELECT data_open, cp_code
                    FROM public.cart_pazpbl
                    WHERE codice = @CodiceMillewin
                        AND (cp_code LIKE '250.%' OR cp_code LIKE '648.8%')
                        AND (modalita = 'C' OR modalita = 'A')
                        AND pb_status = 'A'";

                using var cmd1 = new NpgsqlCommand(query1, connection);
                cmd1.Parameters.AddWithValue("@CodiceMillewin", _codiceMillewinDelPaziente ?? "");

                System.Diagnostics.Debug.WriteLine($"[DIABETE] Cerco diabete per codice: {_codiceMillewinDelPaziente}");
                
                using var reader1 = await cmd1.ExecuteReaderAsync();
                if (!await reader1.ReadAsync())
                {
                    // Non trovato diabete - ritorna senza impostare i dati
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ❌ DIABETE NON TROVATO per paziente {_codiceMillewinDelPaziente}");
                    // Il controllo verrà fatto nel metodo chiamante
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Trovato record diabete generico, verifico tipo...");
                reader1.Close();

                bool isDM2 = false;
                string dataDiagnosiDM2 = "";

                string query2 = @"
                    SELECT cp_code, data_open
                    FROM public.cart_pazpbl
                    WHERE codice = @CodiceMillewin
                        AND cp_code LIKE '250.%'
                        AND (modalita = 'C' OR modalita = 'A')
                        AND pb_status = 'A'
                        AND cp_code IN (SELECT cp_code FROM public.cprobl
                                        WHERE cp_code LIKE '250.%'
                                        AND (UPPER(nome_pbl) = 'DIABETE' OR UPPER(nome_pbl) = 'DIABETE MELLITO' 
                                        OR nome_pbl ILIKE '%tipo II%' OR nome_pbl ILIKE '%tipo 2%'))";

                using var cmd2 = new NpgsqlCommand(query2, connection);
                cmd2.Parameters.AddWithValue("@CodiceMillewin", _codiceMillewinDelPaziente ?? "");

                using var reader2 = await cmd2.ExecuteReaderAsync();
                if (await reader2.ReadAsync())
                {
                    isDM2 = true;
                    dataDiagnosiDM2 = reader2.GetDateTime(1).ToString("dd/MM/yyyy");
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Trovato DM2 con data {dataDiagnosiDM2}");
                }
                reader2.Close();

                bool isDM1 = false;
                string dataDiagnosiDM1 = "";

                string query3 = @"
                    SELECT cp_code, data_open
                    FROM public.cart_pazpbl
                    WHERE codice = @CodiceMillewin
                        AND cp_code LIKE '250.%'
                        AND (modalita = 'C' OR modalita = 'A')
                        AND pb_status = 'A'
                        AND cp_code IN (SELECT cp_code FROM public.cprobl
                                        WHERE cp_code LIKE '250.%'
                                        AND nome_pbl ILIKE '%tipo I%'
                                        AND nome_pbl NOT ILIKE '%II%'
                                        AND nome_pbl NOT ILIKE '%2%')";

                using var cmd3 = new NpgsqlCommand(query3, connection);
                cmd3.Parameters.AddWithValue("@CodiceMillewin", _codiceMillewinDelPaziente ?? "");

                using var reader3 = await cmd3.ExecuteReaderAsync();
                if (await reader3.ReadAsync())
                {
                    isDM1 = true;
                    dataDiagnosiDM1 = reader3.GetDateTime(1).ToString("dd/MM/yyyy");
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Trovato DM1 con data {dataDiagnosiDM1}");
                }

                if (isDM1 && isDM2)
                {
                    MessageBox.Show(
                        "Ho trovato registrati sia il diabete tipo 2 che il diabete tipo 1, entrambi in stato \"Attivo\". " +
                        "Utilizzo il diabete tipo 1, probabilmente il più rilevante ai fini certificativi.",
                        "Incongruenza nelle registrazioni",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    patient.TipoDiabete = "Diabete Mellito Tipo 1";
                    patient.DataInizioDiabete = DateTime.ParseExact(dataDiagnosiDM1, "dd/MM/yyyy", null);
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Impostato DM1+DM2 -> uso DM1");
                }
                else if (isDM1)
                {
                    patient.TipoDiabete = "Diabete Mellito Tipo 1";
                    patient.DataInizioDiabete = DateTime.ParseExact(dataDiagnosiDM1, "dd/MM/yyyy", null);
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Impostato DM1");
                }
                else if (isDM2)
                {
                    patient.TipoDiabete = "Diabete Mellito Tipo 2";
                    patient.DataInizioDiabete = DateTime.ParseExact(dataDiagnosiDM2, "dd/MM/yyyy", null);
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ✓ Impostato DM2");
                }
                else
                {
                    // Caso in cui viene trovato un record con 250.x ma non è né tipo 1 né tipo 2
                    System.Diagnostics.Debug.WriteLine($"[DIABETE] ⚠️ Trovato record diabete ma tipo non specificato");
                }
                // Se non c'è né DM1 né DM2, TipoDiabete rimane null e sarà gestito nel chiamante
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIABETE] ❌ ERRORE: {ex.Message}");
                throw new Exception($"Errore rilevamento tipo diabete: {ex.Message}");
            }
        }

        public async Task<bool> VerificaTrattamentoFarmacologicoAsync()
        {
            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT COUNT(*)
                    FROM public.cart_terap car
                    INNER JOIN public.mn_v_tbl_generica pat ON car.co_atc = pat.codice
                    WHERE car.codice = @CodiceMillewin
                        AND car.te_c_flag = 'C'
                        AND car.co_atc LIKE 'A10%'";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CodiceMillewin", _codiceMillewinDelPaziente ?? "");

                var result = await cmd.ExecuteScalarAsync();
                return result != null && Convert.ToInt64(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<GlycemicCompensationData> GetGlycemicCompensationDataAsync()
        {
            var data = new GlycemicCompensationData();

            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                data.TipoTrattamento = await VerificaTrattamentoFarmacologicoAsync() ? "farmacologico" : "dietetico";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT car.ac_val, TO_CHAR(car.data_open, 'DD/MM/YYYY')
                    FROM cart_accert car
                    WHERE car.codice = @CodiceMillewin
                        AND car.ac_des = 'EMOGLOBINA GLICATA'
                        AND car.ac_val IS NOT NULL
                        AND car.ac_val != 'non eseguito'
                        AND car.data_open >= (CURRENT_DATE - 180)
                    ORDER BY car.data_open DESC
                    LIMIT 1";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@CodiceMillewin", _codiceMillewinDelPaziente ?? "");

                System.Diagnostics.Debug.WriteLine($"[GLICATA] Cerco emoglobina glicata per paziente: {_codiceMillewinDelPaziente}");

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string valStr = reader.GetString(0);
                    data.DataPrelievo = reader.GetString(1);

                    if (decimal.TryParse(valStr.Replace(',', '.'),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal val))
                    {
                        data.UltimaGlicata = val;
                        data.DatiDisponibili = true;
                        data.CalcolaCompenso();
                        System.Diagnostics.Debug.WriteLine($"[GLICATA] ✓ Trovata HbA1c: {val} del {data.DataPrelievo}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GLICATA] ⚠️ Valore non parsabile: {valStr}");
                        data.MessaggioErrore = "Impossibile interpretare il valore di emoglobina glicata registrato.";
                    }
                }
                else
                {
                    // Nessun dato trovato negli ultimi 6 mesi
                    System.Diagnostics.Debug.WriteLine($"[GLICATA] ❌ Nessuna emoglobina glicata trovata negli ultimi 6 mesi");
                    data.MessaggioErrore = "Non è stato possibile recuperare un valore di emoglobina glicata (HbA1c) registrato negli ultimi 6 mesi.\n\n" +
                                          "Il certificato verrà generato senza i dati relativi al compenso glicemico.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GLICATA] ❌ ERRORE: {ex.Message}");
                data.MessaggioErrore = $"Errore durante il recupero dei dati di compenso glicemico: {ex.Message}";
            }

            return data;
        }

        // ==================== NUOVI METODI v1.0.5 - GESTIONE MEDICI CON JSON ====================

        /// <summary>
        /// Carica l'elenco dei medici dal database e salva in JSON
        /// Chiamato all'avvio dell'applicazione
        /// </summary>
        public async Task<List<MedicoSelectionData>> LoadAndSaveMediciAsync()
        {
            try
            {
                EnsureConnectionString(); // Inizializza se necessario
                
                // Crea la directory se non esiste
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                var medici = new List<MedicoSelectionData>();

                using var connection = GetConnection();
                await connection.OpenAsync();

                // Query per estrarre i medici titolari escludendo valori demo e vuoti
                string query = @"
                    SELECT
                        u.nomepass,
                        u.cognomeuser || ' ' || u.nomeuser AS NomeCompleto,
                        u.userid,
                        COALESCE(u.indirizzo, '') as Indirizzo,
                        COALESCE(u.telefono, '') as Telefono,
                        COALESCE(u.email, '') as Email
                    FROM users u
                    WHERE u.tipo_utente = 'T'
                    AND u.nomepass NOT LIKE '%demo%'
                    AND u.cognomeuser IS NOT NULL
                    AND TRIM(u.cognomeuser) <> ''
                    AND u.nomeuser IS NOT NULL
                    AND TRIM(u.nomeuser) <> ''
                    AND u.userid IS NOT NULL
                    AND TRIM(u.userid) <> ''
                    ORDER BY u.cognomeuser, u.nomeuser";

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    medici.Add(new MedicoSelectionData
                    {
                        NomePass = reader.GetString(0),
                        NomeCompleto = reader.GetString(1),
                        UserId = reader.GetString(2),
                        Indirizzo = reader.GetString(3),
                        Telefono = reader.GetString(4),
                        Email = reader.GetString(5)
                    });
                }

                // Salva in JSON
                var mediciData = new MediciListData
                {
                    Medici = medici,
                    DataAggiornamento = DateTime.Now
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string jsonString = JsonSerializer.Serialize(mediciData, options);
                await File.WriteAllTextAsync(MediciJsonPath, jsonString);

                return medici;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore nel caricamento elenco medici: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Legge i dati dei medici dal file JSON
        /// </summary>
        public async Task<List<MedicoSelectionData>> LoadMediciFromJsonAsync()
        {
            try
            {
                if (!File.Exists(MediciJsonPath))
                {
                    // Se il file non esiste, carica dal database
                    return await LoadAndSaveMediciAsync();
                }

                string jsonString = await File.ReadAllTextAsync(MediciJsonPath);
                var mediciData = JsonSerializer.Deserialize<MediciListData>(jsonString);

                return mediciData?.Medici ?? new List<MedicoSelectionData>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore nella lettura del file medici: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Salva un medico inserito manualmente nel file JSON
        /// Usato quando non è possibile recuperare l'elenco dal database
        /// </summary>
        public async Task SaveMedicoManualeAsync(MedicoSelectionData medico)
        {
            try
            {
                // Crea la directory se non esiste
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                List<MedicoSelectionData> medici;

                // Carica eventuali medici esistenti
                if (File.Exists(MediciJsonPath))
                {
                    try
                    {
                        string existingJson = await File.ReadAllTextAsync(MediciJsonPath);
                        var existingData = JsonSerializer.Deserialize<MediciListData>(existingJson);
                        medici = existingData?.Medici ?? new List<MedicoSelectionData>();
                    }
                    catch
                    {
                        medici = new List<MedicoSelectionData>();
                    }
                }
                else
                {
                    medici = new List<MedicoSelectionData>();
                }

                // Rimuovi eventuale medico con stesso UserId (per aggiornamento)
                medici.RemoveAll(m => m.UserId == medico.UserId);

                // Aggiungi il nuovo medico
                medici.Add(medico);

                // Salva in JSON
                var mediciData = new MediciListData
                {
                    Medici = medici,
                    DataAggiornamento = DateTime.Now
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string jsonString = JsonSerializer.Serialize(mediciData, options);
                await File.WriteAllTextAsync(MediciJsonPath, jsonString);

                System.Diagnostics.Debug.WriteLine($"[DB] Medico manuale salvato: {medico.NomeCompleto} ({medico.UserId})");
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore nel salvataggio del medico: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// NUOVO: Ottiene i dati del medico dal file JSON basandosi sull'UserId selezionato
        /// Sostituisce la lettura dal Registry
        /// </summary>
        public async Task<MedicoData> GetMedicoDataFromJsonAsync(string selectedUserId)
        {
            try
            {
                var medici = await LoadMediciFromJsonAsync();
                var medicoSelezionato = medici.Find(m => m.UserId == selectedUserId);

                if (medicoSelezionato == null)
                {
                    throw new Exception($"Medico con UserId '{selectedUserId}' non trovato nel file JSON");
                }

                // Imposta il codice medico corrente per le altre query
                _codiceMedicoCorrente = medicoSelezionato.UserId;

                // Costruisce il MedicoData dal medico selezionato
                return new MedicoData
                {
                    CodiceMedico = medicoSelezionato.UserId,
                    NomeCompleto = medicoSelezionato.NomeCompleto,
                    Email = medicoSelezionato.Email,
                    Telefono = medicoSelezionato.Telefono,
                    Indirizzo = medicoSelezionato.Indirizzo
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore nel recupero dati medico: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Costruisce una lista di codici medico dal file JSON per le query SQL
        /// </summary>
        private async Task<List<string>> GetAllMediciCodesAsync()
        {
            try
            {
                var medici = await LoadMediciFromJsonAsync();
                return medici.Select(m => m.UserId).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore caricamento codici medici: {ex.Message}");
                // Fallback: usa solo il medico corrente se disponibile
                return _codiceMedicoCorrente != null
                    ? new List<string> { _codiceMedicoCorrente }
                    : new List<string>();
            }
        }

        /// <summary>
        /// Aggiunge i parametri dei codici medico al comando SQL
        /// </summary>
        private void AddMediciParameters(NpgsqlCommand command, List<string> mediciCodes)
        {
            for (int i = 0; i < mediciCodes.Count; i++)
            {
                command.Parameters.AddWithValue($"@Medico{i}", mediciCodes[i]);
            }
        }

        /// <summary>
        /// Costruisce la clausola IN dinamica per i codici medico
        /// Es: "AND nos_002.pa_medi IN (@Medico0, @Medico1, @Medico2)"
        /// </summary>
        private string BuildMediciInClause(List<string> mediciCodes, string columnName = "nos_002.pa_medi")
        {
            if (mediciCodes == null || mediciCodes.Count == 0)
            {
                return "AND 1=0"; // Nessun medico trovato, ritorna condizione falsa
            }

            var parameters = string.Join(", ", mediciCodes.Select((_, i) => $"@Medico{i}"));
            return $"AND {columnName} IN ({parameters})";
        }
    }
}
