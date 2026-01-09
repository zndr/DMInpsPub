using System;

namespace DMInps.Models
{
    /// <summary>
    /// Modello dati per le informazioni del paziente e del suo diabete
    /// </summary>
    public class PatientData
    {
        /// <summary>
        /// Codice fiscale del paziente (16 caratteri)
        /// </summary>
        public string CodiceFiscale { get; set; }

        /// <summary>
        /// Codice identificativo del paziente nel database Millewin
        /// </summary>
        public string CodiceMillewin { get; set; }

        /// <summary>
        /// Nome completo del paziente (Cognome + Nome)
        /// </summary>
        public string NomeCompleto { get; set; }

        /// <summary>
        /// Data di nascita del paziente
        /// </summary>
        public DateTime DataNascita { get; set; }

        /// <summary>
        /// Data di inizio/diagnosi del diabete
        /// </summary>
        public DateTime DataInizioDiabete { get; set; }

        /// <summary>
        /// Tipo di diabete: "tipo 1", "tipo 2" o "non specificato"
        /// </summary>
        public string TipoDiabete { get; set; }
    }
}
