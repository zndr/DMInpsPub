using System.Collections.Generic;

namespace DMInps.Models
{
    /// <summary>
    /// Rappresenta i dati della terapia antidiabetica in atto
    /// </summary>
    public partial class DiabetesTherapyData
    {
        public List<TherapyItem> Therapies { get; set; } = new List<TherapyItem>();
        public bool HasTherapies => Therapies?.Count > 0;
    }

    /// <summary>
    /// Singolo farmaco antidiabetico
    /// </summary>
    public class TherapyItem
    {
        public string AtcCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DrugName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
    }
}
