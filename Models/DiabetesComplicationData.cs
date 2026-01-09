using System;

namespace DMInps.Models
{
    /// <summary>
    /// Rappresenta i dati di una singola complicanza diabetica
    /// </summary>
    public class DiabetesComplicationData
    {
        /// <summary>
        /// Nome della complicanza (es. "Nefropatia", "Retinopatia")
        /// </summary>
        public string ComplicationType { get; set; }

        /// <summary>
        /// Indica se la complicanza è presente
        /// Valori possibili: "sì", "no", "N.V" (Non Valutato)
        /// </summary>
        public string IsPresent { get; set; }

        /// <summary>
        /// Gravità della complicanza
        /// Valori possibili: "Lieve", "Moderata", "Grave", null se non classificabile
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Note aggiuntive dalla diagnosi originale
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Data di apertura del problema
        /// </summary>
        public DateTime? DateOpened { get; set; }

        /// <summary>
        /// Costruttore di default
        /// </summary>
        public DiabetesComplicationData()
        {
            IsPresent = "no"; // Default
            Severity = null;
            Notes = string.Empty;
        }

        /// <summary>
        /// Costruttore con parametri
        /// </summary>
        public DiabetesComplicationData(string complicationType, string isPresent = "no", 
            string severity = null, string notes = "", DateTime? dateOpened = null)
        {
            ComplicationType = complicationType;
            IsPresent = isPresent;
            Severity = severity;
            Notes = notes;
            DateOpened = dateOpened;
        }
    }
}
