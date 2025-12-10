using Microsoft.Win32;
using System;

namespace DMInps.DebugConsole
{
    /// <summary>
    /// Classe per il debug della configurazione del Registry di Windows
    /// Analizza le chiavi ODBC necessarie per la connessione a Millewin
    /// </summary>
    public class RegistryDebugger
    {
        // Costanti delle chiavi di registro (identiche a RegistryService.cs)
        private const string RegistryBasePath = @"Software\ODBC\ODBC.INI";
        private const string PrimaryKeyName = "mille_MillePS";
        private const string FallbackKeyName = "milleps";
        private const string DoctorCodesKey = "doctorCodes";
        private const string DbServerIpKey = "Servername";

        /// <summary>
        /// Risultato dell'analisi del Registry
        /// </summary>
        public class RegistryAnalysisResult
        {
            public bool Success { get; set; }
            public string? KeyPath { get; set; }
            public string? KeyName { get; set; }
            public string? HiveName { get; set; }
            public string? ServerIp { get; set; }
            public string? DoctorCode { get; set; }
        }

        /// <summary>
        /// Analizza la configurazione del Registry passo per passo
        /// Stampa ogni operazione eseguita
        /// </summary>
        public RegistryAnalysisResult AnalyzeRegistry()
        {
            var result = new RegistryAnalysisResult();

            PrintStep("Inizio analisi Registry di Windows...");
            Console.WriteLine();

            // Stampa i valori delle costanti
            PrintVariable("RegistryBasePath", RegistryBasePath);
            PrintVariable("PrimaryKeyName", PrimaryKeyName);
            PrintVariable("FallbackKeyName", FallbackKeyName);
            PrintVariable("DoctorCodesKey", DoctorCodesKey);
            PrintVariable("DbServerIpKey", DbServerIpKey);
            Console.WriteLine();

            // === Ricerca in HKEY_CURRENT_USER ===
            PrintStep("Ricerca in HKEY_CURRENT_USER...");
            
            var foundKey = TryFindKey(Registry.CurrentUser, "HKEY_CURRENT_USER");
            
            if (foundKey.key != null)
            {
                result.Success = true;
                result.KeyPath = $@"HKEY_CURRENT_USER\{RegistryBasePath}\{foundKey.keyName}";
                result.KeyName = foundKey.keyName;
                result.HiveName = "HKEY_CURRENT_USER";
                
                PrintSuccess($"✓ Chiave trovata: {result.KeyPath}");
                Console.WriteLine();

                // Leggi i valori dalla chiave trovata
                ReadKeyValues(foundKey.key, result);
                foundKey.key.Dispose();
                
                return result;
            }

            Console.WriteLine();

            // === Ricerca in HKEY_LOCAL_MACHINE ===
            PrintStep("Ricerca in HKEY_LOCAL_MACHINE...");
            
            foundKey = TryFindKey(Registry.LocalMachine, "HKEY_LOCAL_MACHINE");
            
            if (foundKey.key != null)
            {
                result.Success = true;
                result.KeyPath = $@"HKEY_LOCAL_MACHINE\{RegistryBasePath}\{foundKey.keyName}";
                result.KeyName = foundKey.keyName;
                result.HiveName = "HKEY_LOCAL_MACHINE";
                
                PrintSuccess($"✓ Chiave trovata: {result.KeyPath}");
                Console.WriteLine();

                // Leggi i valori dalla chiave trovata
                ReadKeyValues(foundKey.key, result);
                foundKey.key.Dispose();
                
                return result;
            }

            Console.WriteLine();
            PrintError($"❌ Nessuna chiave ODBC trovata");
            PrintError($"   Cercato '{PrimaryKeyName}' e '{FallbackKeyName}' in:");
            PrintError($"   - HKEY_CURRENT_USER\\{RegistryBasePath}");
            PrintError($"   - HKEY_LOCAL_MACHINE\\{RegistryBasePath}");
            
            return result;
        }

        /// <summary>
        /// Tenta di trovare la chiave ODBC nell'hive specificato
        /// </summary>
        private (RegistryKey? key, string keyName) TryFindKey(RegistryKey hive, string hiveName)
        {
            // Apri la base path
            var baseKey = hive.OpenSubKey(RegistryBasePath);
            
            if (baseKey == null)
            {
                PrintWarning($"  ⚠ Path base '{RegistryBasePath}' non trovato in {hiveName}");
                return (null, string.Empty);
            }

            PrintInfo($"  • Path base trovato: {hiveName}\\{RegistryBasePath}");

            // Prova con PrimaryKeyName
            PrintInfo($"  • Cerco chiave '{PrimaryKeyName}'...");
            var primaryKey = baseKey.OpenSubKey(PrimaryKeyName);
            
            if (primaryKey != null)
            {
                PrintSuccess($"  ✓ Trovata chiave primaria: {PrimaryKeyName}");
                baseKey.Dispose();
                return (primaryKey, PrimaryKeyName);
            }
            
            PrintWarning($"    ✗ Chiave '{PrimaryKeyName}' non trovata");

            // Prova con FallbackKeyName
            PrintInfo($"  • Cerco chiave fallback '{FallbackKeyName}'...");
            var fallbackKey = baseKey.OpenSubKey(FallbackKeyName);
            
            if (fallbackKey != null)
            {
                PrintSuccess($"  ✓ Trovata chiave fallback: {FallbackKeyName}");
                baseKey.Dispose();
                return (fallbackKey, FallbackKeyName);
            }
            
            PrintWarning($"    ✗ Chiave '{FallbackKeyName}' non trovata");

            baseKey.Dispose();
            return (null, string.Empty);
        }

        /// <summary>
        /// Legge i valori dalla chiave di registro trovata
        /// </summary>
        private void ReadKeyValues(RegistryKey key, RegistryAnalysisResult result)
        {
            PrintStep("Lettura valori dalla chiave...");

            // Elenca tutti i valori presenti nella chiave
            var valueNames = key.GetValueNames();
            PrintInfo($"Valori presenti nella chiave ({valueNames.Length}):");
            
            foreach (var valueName in valueNames)
            {
                var value = key.GetValue(valueName);
                string displayName = string.IsNullOrEmpty(valueName) ? "(Predefinito)" : valueName;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  • {displayName} = {value}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.WriteLine();

            // === Lettura DbServerIpKey (Servername) ===
            PrintInfo($"Lettura valore '{DbServerIpKey}'...");
            
            var serverIp = key.GetValue(DbServerIpKey);
            
            if (serverIp != null)
            {
                result.ServerIp = serverIp.ToString();
                PrintSuccess($"  ✓ {DbServerIpKey} = {result.ServerIp}");
            }
            else
            {
                PrintWarning($"  ⚠ Valore '{DbServerIpKey}' non trovato");
                
                // Prova a leggere il valore predefinito
                PrintInfo("  Tento con il valore predefinito (Default)...");
                var defaultValue = key.GetValue(null) ?? key.GetValue("");
                
                if (defaultValue != null)
                {
                    result.ServerIp = defaultValue.ToString();
                    PrintSuccess($"  ✓ Valore predefinito = {result.ServerIp}");
                }
                else
                {
                    PrintError($"  ❌ Nessun valore IP trovato nella chiave");
                }
            }

            Console.WriteLine();

            // === Lettura DoctorCodesKey (doctorCodes) ===
            PrintInfo($"Lettura valore '{DoctorCodesKey}' (opzionale)...");
            
            var doctorCode = key.GetValue(DoctorCodesKey);
            
            if (doctorCode != null)
            {
                result.DoctorCode = doctorCode.ToString();
                PrintSuccess($"  ✓ {DoctorCodesKey} = {result.DoctorCode}");
                PrintInfo("  Nota: DMInps non usa più questo valore, carica i medici dal database");
            }
            else
            {
                PrintWarning($"  ⚠ Valore '{DoctorCodesKey}' non presente");
                PrintInfo("  Questo è normale: DMInps carica i medici dal database");
            }

            Console.WriteLine();
        }

        #region Utility Methods per Output

        private void PrintStep(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"► {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

        private void PrintVariable(string name, string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"  {name,-20}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" = \"{value}\"");
            Console.ForegroundColor = ConsoleColor.White;
        }

        #endregion
    }
}
