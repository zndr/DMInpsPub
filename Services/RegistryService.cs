using Microsoft.Win32;
using System;

namespace DMInps.Services
{
    /// <summary>
    /// Servizio per la lettura dei valori dal registry di Windows
    /// Gestisce l'accesso alle chiavi di configurazione di Millewin
    /// </summary>
    public static class RegistryService
    {
        // Percorsi delle chiavi di registry (con fallback)
        private const string RegistryBasePath = @"Software\ODBC\ODBC.INI";
        private const string PrimaryKeyName = "mille_MillePS";
        private const string FallbackKeyName = "milleps";
        private const string DbServerIpKey = "Servername";

        /// <summary>
        /// Trova la chiave ODBC corretta cercando prima "mille_MillePS" poi "milleps"
        /// </summary>
        /// <returns>RegistryKey aperta o null se non trovata</returns>
        private static RegistryKey? FindOdbcKey()
        {
            // Prima cerca in HKEY_CURRENT_USER
            var baseKey = Registry.CurrentUser.OpenSubKey(RegistryBasePath);
            if (baseKey != null)
            {
                // Prova prima con "mille_MillePS"
                var primaryKey = baseKey.OpenSubKey(PrimaryKeyName);
                if (primaryKey != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_CURRENT_USER: {RegistryBasePath}\\{PrimaryKeyName}");
                    baseKey.Dispose();
                    return primaryKey;
                }

                // Fallback a "milleps"
                var fallbackKey = baseKey.OpenSubKey(FallbackKeyName);
                if (fallbackKey != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_CURRENT_USER (fallback): {RegistryBasePath}\\{FallbackKeyName}");
                    baseKey.Dispose();
                    return fallbackKey;
                }

                baseKey.Dispose();
            }

            // Se non trovato in CURRENT_USER, cerca in LOCAL_MACHINE
            baseKey = Registry.LocalMachine.OpenSubKey(RegistryBasePath);
            if (baseKey != null)
            {
                // Prova prima con "mille_MillePS"
                var primaryKey = baseKey.OpenSubKey(PrimaryKeyName);
                if (primaryKey != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_LOCAL_MACHINE: {RegistryBasePath}\\{PrimaryKeyName}");
                    baseKey.Dispose();
                    return primaryKey;
                }

                // Fallback a "milleps"
                var fallbackKey = baseKey.OpenSubKey(FallbackKeyName);
                if (fallbackKey != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_LOCAL_MACHINE (fallback): {RegistryBasePath}\\{FallbackKeyName}");
                    baseKey.Dispose();
                    return fallbackKey;
                }

                baseKey.Dispose();
            }

            System.Diagnostics.Debug.WriteLine($"[REGISTRY] ❌ Nessuna chiave ODBC trovata (cercato: {PrimaryKeyName}, {FallbackKeyName})");
            return null;
        }

        /// <summary>
        /// Apre una chiave di registry cercando prima in HKEY_CURRENT_USER e poi in HKEY_LOCAL_MACHINE
        /// Mantenuto per retrocompatibilità con path diretti
        /// </summary>
        /// <param name="path">Percorso della chiave di registry</param>
        /// <returns>RegistryKey aperta o null se non trovata</returns>
        private static RegistryKey? OpenRegistryKey(string path)
        {
            // Prova prima in HKEY_CURRENT_USER (non richiede privilegi amministrativi)
            var key = Registry.CurrentUser.OpenSubKey(path);
            if (key != null)
            {
                System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_CURRENT_USER: {path}");
                return key;
            }

            // Fallback a HKEY_LOCAL_MACHINE
            key = Registry.LocalMachine.OpenSubKey(path);
            if (key != null)
            {
                System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave trovata in HKEY_LOCAL_MACHINE: {path}");
                return key;
            }

            System.Diagnostics.Debug.WriteLine($"[REGISTRY] Chiave non trovata in nessun hive: {path}");
            return null;
        }

        /// <summary>
        /// Recupera l'indirizzo IP del server database dal registry di Windows
        /// Cerca prima in "mille_MillePS" poi in "milleps" come fallback
        /// </summary>
        /// <returns>Indirizzo IP del server database</returns>
        /// <exception cref="InvalidOperationException">Se impossibile leggere dal registry o valore non trovato</exception>
        public static string GetDatabaseServerIp()
        {
            try
            {
                using var key = FindOdbcKey();

                if (key == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] ❌ Chiave ODBC non trovata");
                    throw new InvalidOperationException(
                        $"Chiave registry ODBC non trovata. Verificato: '{PrimaryKeyName}' e '{FallbackKeyName}' in {RegistryBasePath}.\n\n" +
                        "Verificare che Millewin sia installato correttamente.");
                }

                // Prova prima a leggere il valore Servername
                var value = key.GetValue(DbServerIpKey);

                // Se non trovato, prova a leggere il valore predefinito
                if (value == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] Valore '{DbServerIpKey}' non trovato, provo con il valore predefinito");
                    value = key.GetValue(null); // null legge il valore predefinito (Default)

                    if (value == null)
                    {
                        value = key.GetValue(""); // Alternativa per leggere il valore predefinito
                    }
                }

                if (value == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[REGISTRY] ❌ Nessun valore trovato nel registry");
                    throw new InvalidOperationException(
                        $"IP del server database non configurato nel registry. " +
                        $"Cercato: '{DbServerIpKey}' e valore predefinito nella chiave ODBC trovata");
                }

                string serverIp = value.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(serverIp))
                {
                    throw new InvalidOperationException(
                        "IP del server database vuoto nel registry");
                }

                System.Diagnostics.Debug.WriteLine($"[REGISTRY] ✓ IP server database recuperato: {serverIp}");

                return serverIp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[REGISTRY] ❌ Errore lettura IP database: {ex.Message}");

                if (ex is InvalidOperationException)
                    throw;

                throw new InvalidOperationException(
                    "Impossibile leggere l'IP del server database dal registry. " +
                    "Verificare che Millewin sia installato correttamente.", ex);
            }
        }

        /// <summary>
        /// Verifica se le chiavi di configurazione esistono nel registry
        /// </summary>
        /// <returns>True se tutte le chiavi necessarie esistono, False altrimenti</returns>
        public static bool ValidateRegistryConfiguration()
        {
            try
            {
                using var key = FindOdbcKey();

                if (key == null)
                    return false;

                bool hasDbServerIp = key.GetValue(DbServerIpKey) != null;

                System.Diagnostics.Debug.WriteLine($"[REGISTRY] Validazione configurazione: DbServerIp={hasDbServerIp}");

                return hasDbServerIp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[REGISTRY] ❌ Errore validazione configurazione: {ex.Message}");
                return false;
            }
        }
    }
}
