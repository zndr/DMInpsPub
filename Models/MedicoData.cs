namespace DMInps.Models
{
    /// <summary>
    /// Modello che rappresenta i dati del medico certificatore
    /// Contiene le informazioni professionali necessarie per la firma della relazione
    /// </summary>
    public class MedicoData
    {
        /// <summary>
        /// Codice identificativo del medico (nomepass)
        /// </summary>
        public string CodiceMedico { get; set; } = string.Empty;

        /// <summary>
        /// Nome completo del medico (cognome + nome)
        /// </summary>
        public string NomeCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Indirizzo completo dello studio medico
        /// </summary>
        public string Indirizzo { get; set; } = string.Empty;

        /// <summary>
        /// Numero di telefono dello studio medico
        /// </summary>
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Indirizzo email del medico
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
