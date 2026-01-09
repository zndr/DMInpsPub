using System;
using System.Collections.Generic;

namespace DMInps.Models
{
    /// <summary>
    /// Model per i dati del medico estratti dalla query di selezione
    /// Utilizzato per popolare la ComboBox e salvare in JSON
    /// </summary>
    public class MedicoSelectionData
    {
        public string NomePass { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Indirizzo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container per la lista dei medici salvata in JSON
    /// </summary>
    public class MediciListData
    {
        public List<MedicoSelectionData> Medici { get; set; } = new List<MedicoSelectionData>();
        public DateTime DataAggiornamento { get; set; }
    }
}
