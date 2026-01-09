namespace DMInps.Models
{
    /// <summary>
    /// Risultato del controllo aggiornamenti via GitHub API
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// Indica se è disponibile un aggiornamento
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// Versione corrente dell'applicazione
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Ultima versione disponibile su GitHub
        /// </summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// URL diretto per il download dell'installer (.exe o .msi)
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Note della release (body del release su GitHub)
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// URL della pagina release su GitHub (fallback se non c'è installer)
        /// </summary>
        public string? ReleasePageUrl { get; set; }

        /// <summary>
        /// Messaggio di errore in caso di fallimento del controllo
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Indica se il controllo è andato a buon fine
        /// </summary>
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}
