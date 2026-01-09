using System;

namespace DMInps.Models
{
    public class GlycemicCompensationData
    {
        public string TipoTrattamento { get; set; } = "dietetico";
        public decimal UltimaGlicata { get; set; }
        public string DataPrelievo { get; set; } = string.Empty;
        public decimal HbPercento { get; set; }
        public decimal HbMmol { get; set; }
        public string ValutazioneCompenso { get; set; } = string.Empty;
        public bool DatiDisponibili { get; set; } = false;
        public string MessaggioErrore { get; set; } = string.Empty;

        public void CalcolaCompenso()
        {
            if (!DatiDisponibili)
            {
                ValutazioneCompenso = "Dati non disponibili";
                return;
            }

            bool isMmol = UltimaGlicata > 25;

            if (isMmol)
            {
                HbMmol = UltimaGlicata;
                HbPercento = Math.Round((UltimaGlicata * 0.0915m) + 2.15m, 1);
            }
            else
            {
                HbPercento = UltimaGlicata;
                HbMmol = Math.Round((UltimaGlicata - 2.15m) * 10.929m, 0);
            }

            if (HbMmol < 53)
            {
                ValutazioneCompenso = "buon compenso";
            }
            else if (HbMmol < 85)
            {
                ValutazioneCompenso = "compenso mediocre";
            }
            else
            {
                ValutazioneCompenso = "scompensato";
            }
        }

        public string GetValoreFormattato()
        {
            if (!DatiDisponibili)
                return "N/D";

            return $"{HbPercento:F1}% ({HbMmol:F0} mmol/mol)";
        }
    }
}
