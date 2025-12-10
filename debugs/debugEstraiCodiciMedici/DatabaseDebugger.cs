using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DMInps.DebugConsole
{
    /// <summary>
    /// Classe per il debug della connessione al database PostgreSQL
    /// </summary>
    public class DatabaseDebugger
    {
        private readonly string _connectionString;

        /// <summary>
        /// Dati essenziali di un medico per il debug
        /// </summary>
        public class MedicoDebugData
        {
            public string UserId { get; set; } = string.Empty;
            public string NomeCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public DatabaseDebugger(string serverIp)
        {
            _connectionString = $"Host={serverIp};Port=5432;Database=milleps;Username=dba;Password=sql;";
            
            PrintInfo("Parametri connessione:");
            PrintVariable("Host", serverIp);
            PrintVariable("Port", "5432");
            PrintVariable("Database", "milleps");
            PrintVariable("Username", "dba");
            PrintVariable("Password", "***");
            Console.WriteLine();
        }

        /// <summary>
        /// Testa la connessione al database PostgreSQL
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                PrintStep("Tentativo di connessione al database...");
                
                using var connection = new NpgsqlConnection(_connectionString);
                
                await connection.OpenAsync();
                
                PrintSuccess($"✓ Connessione stabilita");
                PrintInfo($"  Versione PostgreSQL: {connection.PostgreSqlVersion}");
                PrintInfo($"  Database: {connection.Database}");
                PrintInfo($"  Host: {connection.Host}:{connection.Port}");
                
                return true;
            }
            catch (Exception ex)
            {
                PrintError($"❌ Errore connessione: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    PrintError($"   Dettagli: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Carica l'elenco dei medici dal database
        /// </summary>
        public async Task<List<MedicoDebugData>> LoadMediciAsync()
        {
            var medici = new List<MedicoDebugData>();

            try
            {
                PrintStep("Caricamento elenco medici dal database...");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query identica a quella in DatabaseService.cs (riga 111)
                string query = @"
                    SELECT 
                        u.nomepass, 
                        u.userid, 
                        u.cognomeuser || ' ' || u.nomeuser as Medico, 
                        COALESCE(u.indirizzo || ', ' || u.cap || ' ' || u.citta, u.indirizzo, 'Non specificato') as Indirizzo,
                        COALESCE(u.telefono, 'Non specificato') as Telefono,
                        COALESCE(u.email, 'Non specificata') as Email
                    FROM users u 
                    WHERE u.tipo_utente = 'T'
                    AND u.nomepass NOT LIKE 'demo%'
                    ORDER BY u.cognomeuser, u.nomeuser";

                PrintInfo($"Query SQL:");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(query);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    medici.Add(new MedicoDebugData
                    {
                        UserId = reader.GetString(1),
                        NomeCompleto = reader.GetString(2),
                        Email = reader.GetString(5)
                    });
                }

                PrintSuccess($"✓ Caricati {count} medici dalla tabella 'users'");
                
                return medici;
            }
            catch (Exception ex)
            {
                PrintError($"❌ Errore caricamento medici: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    PrintError($"   Dettagli: {ex.InnerException.Message}");
                }
                
                return medici;
            }
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

        private void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

        private void PrintVariable(string name, string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"  {name,-15}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" = {value}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        #endregion
    }
}
