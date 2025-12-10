using System;
using System.Threading.Tasks;

namespace DMInps.DebugConsole
{
    /// <summary>
    /// Applicazione console di debug per DMInps
    /// Verifica la configurazione del Registry e la connessione al database PostgreSQL
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configurazione colori console
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            PrintHeader();

            try
            {
                // === STEP 1: Debug Registry ===
                PrintSection("STEP 1 - ANALISI REGISTRY DI WINDOWS");
                
                var registryDebugger = new RegistryDebugger();
                var registryResult = registryDebugger.AnalyzeRegistry();

                if (!registryResult.Success)
                {
                    PrintError("❌ Impossibile procedere: configurazione Registry non valida");
                    PrintWarning("\nPossibili soluzioni:");
                    PrintWarning("  1. Verificare che Millewin sia installato correttamente");
                    PrintWarning("  2. Controllare che esista la chiave ODBC 'mille_MillePS' o 'milleps'");
                    PrintWarning("  3. Eseguire l'applicazione come Amministratore");
                    
                    WaitForExit();
                    return;
                }

                PrintSuccess("✓ Configurazione Registry valida");
                Console.WriteLine();

                // === STEP 2: Test connessione Database ===
                PrintSection("STEP 2 - TEST CONNESSIONE DATABASE");

                var dbDebugger = new DatabaseDebugger(registryResult.ServerIp!);
                bool dbConnected = await dbDebugger.TestConnectionAsync();

                if (!dbConnected)
                {
                    PrintError("❌ Impossibile connettersi al database");
                    PrintWarning("\nPossibili soluzioni:");
                    PrintWarning($"  1. Verificare che il server PostgreSQL sia attivo su {registryResult.ServerIp}");
                    PrintWarning("  2. Controllare username/password (dba/sql)");
                    PrintWarning("  3. Verificare che la porta 5432 sia accessibile");
                    PrintWarning("  4. Controllare il firewall di Windows");
                    
                    WaitForExit();
                    return;
                }

                PrintSuccess("✓ Connessione database riuscita");
                Console.WriteLine();

                // === STEP 3: Caricamento medici ===
                PrintSection("STEP 3 - CARICAMENTO ELENCO MEDICI");

                var medici = await dbDebugger.LoadMediciAsync();
                
                if (medici.Count == 0)
                {
                    PrintWarning("⚠ Nessun medico trovato nel database");
                }
                else
                {
                    PrintSuccess($"✓ Trovati {medici.Count} medici nel database");
                    Console.WriteLine();
                    
                    PrintInfo("Elenco medici:");
                    foreach (var medico in medici)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  • {medico.NomeCompleto}");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"    UserId: {medico.UserId} | Email: {medico.Email}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.WriteLine();
                PrintSuccess("═══════════════════════════════════════════════════");
                PrintSuccess("    DEBUG COMPLETATO CON SUCCESSO");
                PrintSuccess("═══════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                PrintError($"❌ ERRORE FATALE: {ex.Message}");
                PrintError($"\nStack trace:\n{ex.StackTrace}");
            }

            WaitForExit();
        }

        #region Utility Methods per Output Colorato

        /// <summary>
        /// Stampa l'intestazione dell'applicazione
        /// </summary>
        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("  DMInps - Strumento di Debug Registry e Database");
            Console.WriteLine($"  Versione 1.0 - {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        /// <summary>
        /// Stampa una sezione con sfondo blu
        /// </summary>
        static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($" {title} ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();
        }

        /// <summary>
        /// Stampa un messaggio di successo in verde
        /// </summary>
        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Stampa un messaggio di errore in rosso
        /// </summary>
        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Stampa un messaggio di warning in giallo
        /// </summary>
        static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Stampa un messaggio informativo in ciano
        /// </summary>
        static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Aspetta l'input dell'utente prima di chiudere
        /// </summary>
        static void WaitForExit()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Premi un tasto per uscire...");
            Console.ReadKey(true);
        }

        #endregion
    }
}
