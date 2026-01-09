using System;
using System.Text;

namespace DMInps.Models
{
    /// <summary>
    /// Modello per la configurazione del formato del nome file
    /// </summary>
    public class FileNameFormat
    {
        public bool IncludiCodiceMedico { get; set; }
        public bool IncludiNomeMedico { get; set; }
        public bool IncludiNomePaziente { get; set; } = true; // Obbligatorio per default
        public bool IncludiCodiceFiscale { get; set; }
        public bool IncludiDataOra { get; set; } = true;
        public string Separatore { get; set; } = "_";

        /// <summary>
        /// Genera il nome del file in base alle impostazioni
        /// </summary>
        public string GenerateFileName(string codiceMedico, string nomeMedico, 
            string nomePaziente, string codiceFiscale)
        {
            var parts = new System.Collections.Generic.List<string>();

            parts.Add("DMInps"); // Prefisso fisso

            if (IncludiCodiceMedico && !string.IsNullOrWhiteSpace(codiceMedico))
                parts.Add(SanitizeFileName(codiceMedico));

            if (IncludiNomeMedico && !string.IsNullOrWhiteSpace(nomeMedico))
                parts.Add(SanitizeFileName(nomeMedico));

            if (IncludiNomePaziente && !string.IsNullOrWhiteSpace(nomePaziente))
                parts.Add(SanitizeFileName(nomePaziente));

            if (IncludiCodiceFiscale && !string.IsNullOrWhiteSpace(codiceFiscale))
                parts.Add(codiceFiscale);

            if (IncludiDataOra)
                parts.Add(DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            return string.Join(Separatore, parts) + ".pdf";
        }

        /// <summary>
        /// Rimuove i caratteri non validi dal nome file
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();

            foreach (char c in fileName)
            {
                if (!char.IsControl(c) && Array.IndexOf(invalid, c) < 0)
                    sanitized.Append(c);
            }

            return sanitized.ToString().Replace(" ", "");
        }

        /// <summary>
        /// Serializza il formato in stringa
        /// </summary>
        public override string ToString()
        {
            return $"{IncludiCodiceMedico}|{IncludiNomeMedico}|{IncludiNomePaziente}|" +
                   $"{IncludiCodiceFiscale}|{IncludiDataOra}|{Separatore}";
        }

        /// <summary>
        /// Deserializza il formato da stringa
        /// </summary>
        public static FileNameFormat FromString(string data)
        {
            try
            {
                var parts = data.Split('|');
                return new FileNameFormat
                {
                    IncludiCodiceMedico = bool.Parse(parts[0]),
                    IncludiNomeMedico = bool.Parse(parts[1]),
                    IncludiNomePaziente = bool.Parse(parts[2]),
                    IncludiCodiceFiscale = bool.Parse(parts[3]),
                    IncludiDataOra = bool.Parse(parts[4]),
                    Separatore = parts.Length > 5 ? parts[5] : "_"
                };
            }
            catch
            {
                return new FileNameFormat();
            }
        }

        /// <summary>
        /// Valida che almeno un campo obbligatorio sia selezionato
        /// </summary>
        public bool IsValid()
        {
            return IncludiNomePaziente || IncludiCodiceFiscale;
        }
    }
}