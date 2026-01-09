using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using DMInps.Models;

namespace DMInps.Services
{
    public partial class DatabaseService
    {
        /// <summary>
        /// Dizionario per mapping ATC -> Categoria (caricato dal CSV)
        /// </summary>
        private static Dictionary<string, string> _atcCategoryMap = null;

        /// <summary>
        /// Estrae la terapia antidiabetica in atto del paziente
        /// </summary>
        public async Task<DiabetesTherapyData> GetDiabetesTherapyAsync(string codiceMillewin)
        {
            var therapyData = new DiabetesTherapyData();

            try
            {
                // Carica mapping ATC se non già fatto
                if (_atcCategoryMap == null)
                {
                    await LoadAtcCategoryMappingAsync();
                }

                using var connection = GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT DISTINCT 
                        ct.co_atc, 
                        ct.co_des || ' / ' || ta.atc_des AS Farmaco, 
                        ct.po_des AS posologia, 
                        te_c_flag 
                    FROM cart_terap ct
                    INNER JOIN mn_v_prodotti mvp ON ct.co_codifa = mvp.codice_prodotto
                    INNER JOIN tab_atc ta ON mvp.codice_atc = ta.atc_cod
                    WHERE ct.codice = @codice
                      AND ct.te_c_flag = 'C'
                      AND ct.co_atc LIKE 'A10%'
                    ORDER BY ct.co_atc";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@codice", codiceMillewin);

                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var atcCode = reader["co_atc"]?.ToString() ?? string.Empty;
                    var drugName = reader["Farmaco"]?.ToString() ?? string.Empty;
                    var dosage = reader["posologia"]?.ToString() ?? string.Empty;

                    // Estrai categoria dal mapping
                    var category = GetCategoryFromAtc(atcCode);

                    therapyData.Therapies.Add(new TherapyItem
                    {
                        AtcCode = atcCode,
                        Category = category,
                        DrugName = drugName,
                        Dosage = dosage
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore estrazione terapia diabete: {ex.Message}");
            }

            return therapyData;
        }

        /// <summary>
        /// Carica il mapping ATC -> Categoria dal file CSV
        /// </summary>
        private async Task LoadAtcCategoryMappingAsync()
        {
            _atcCategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Percorso del CSV (deve essere nella cartella dell'applicazione)
                var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "atc_a10.csv");

                if (!File.Exists(csvPath))
                {
                    Console.WriteLine($"ATTENZIONE: File CSV non trovato in: {csvPath}");
                    return;
                }

                var lines = await File.ReadAllLinesAsync(csvPath);

                // Salta la prima riga (intestazione)
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        var atcCode = parts[1].Trim();
                        var category = parts[2].Trim();

                        if (!string.IsNullOrEmpty(atcCode))
                        {
                            _atcCategoryMap[atcCode] = category;
                        }
                    }
                }

                Console.WriteLine($"Caricati {_atcCategoryMap.Count} codici ATC dal CSV");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore caricamento CSV ATC: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottiene la categoria farmacologica dal codice ATC
        /// </summary>
        private string GetCategoryFromAtc(string atcCode)
        {
            if (string.IsNullOrEmpty(atcCode)) return "Non classificato";

            // Cerca il codice completo (es. A10BA02)
            if (_atcCategoryMap != null && _atcCategoryMap.TryGetValue(atcCode, out var category))
            {
                return category;
            }

            // Se non trovato, cerca per prefisso (primi 5 caratteri: A10BA)
            var prefix = atcCode.Length >= 5 ? atcCode.Substring(0, 5) : atcCode;
            var matchingCategory = _atcCategoryMap?
                .FirstOrDefault(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Value;

            return matchingCategory ?? "Altri antidiabetici";
        }
    }
}
