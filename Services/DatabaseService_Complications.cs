using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using DMInps.Models;

namespace DMInps.Services
{
    /// <summary>
    /// Estensioni DatabaseService per gestione complicanze diabete
    /// FILE UNIFICATO - Contiene sia query che helper
    /// </summary>
    public partial class DatabaseService
    {
        #region Query Complicanze
        
        /// <summary>
        /// Estrae le complicanze diabetiche del paziente
        /// </summary>
        public List<(string NomeProblema, DateTime? DataApertura, string GruppoComplicanza)> GetDiabetesComplications(string codiceMillewin)
        {
            var complications = new List<(string, DateTime?, string)>();

            string query = @"
SELECT cp.nome_pbl, cp.data_open,
CASE 
    -- Nefropatia diabetica
    WHEN cp.cp_code LIKE '250.4%' OR cp.cp_code IN ('583.81','585','586','791.0') 
        THEN 'Nefropatia'
    -- Retinopatia diabetica  
    WHEN cp.cp_code LIKE '250.5%' OR 
         cp.cp_code LIKE '362.0%' OR 
         cp.cp_code IN ('362.1','362.83','361.9','364.42','365.44','366.41') OR
         cp.cp_code LIKE '361.0%' OR cp.cp_code LIKE '369.%'
        THEN 'Retinopatia'
    -- Neuropatia diabetica
    WHEN cp.cp_code LIKE '250.6%' OR 
         cp.cp_code IN ('337.1','357.2','536.3','458','731.8') OR
         cp.cp_code LIKE '354.%' OR cp.cp_code LIKE '355.%' OR cp.cp_code LIKE '713.%'
        THEN 'Neuropatia'
    -- Arteriopatia periferica
    WHEN cp.cp_code LIKE '250.7%' OR 
         cp.cp_code LIKE '440.2%' OR 
         cp.cp_code IN ('443.81','443.9')
        THEN 'Arteriopatia periferica'
    -- Altre complicanze
    WHEN cp.cp_code LIKE '250.1%' OR cp.cp_code LIKE '250.2%' OR 
         cp.cp_code LIKE '250.3%' OR cp.cp_code LIKE '250.8%' OR 
         cp.cp_code LIKE '250.9%' OR cp.cp_code LIKE 'V49.7%' OR
         cp.cp_code IN ('681.1','681.11','682.6','682.7','729.4',
                     '730.06','730.07','785.4') OR
         cp.cp_code LIKE '707.1%'
        THEN 'Altre complicanze'
    ELSE 'Non classificato'
END AS gruppo_complicanza
FROM pazienti p, cart_pazpbl cp
WHERE p.codice = cp.codice
AND p.codice = @CodiceMillewin
AND cp.nome_pbl IS NOT NULL 
AND cp.pb_status = 'A'
AND (cp.modalita = 'A' OR cp.modalita = 'C') 
AND (cp.cp_code LIKE '250.%' OR cp.cp_code IN ('583.81','585','586','791.0') OR
     cp.cp_code LIKE '362.%' OR cp.cp_code LIKE '361.%' OR cp.cp_code LIKE '369.%' OR
     cp.cp_code IN ('337.1','357.2','536.3','458','731.8') OR
     cp.cp_code LIKE '354.%' OR cp.cp_code LIKE '355.%' OR cp.cp_code LIKE '713.%' OR
     cp.cp_code LIKE '440.2%' OR cp.cp_code IN ('443.81','443.9') OR
     cp.cp_code IN ('681.1','681.11','682.6','682.7','729.4','730.06','730.07','785.4') OR
     cp.cp_code LIKE '707.1%' OR cp.cp_code LIKE 'V49.7%')";

            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CodiceMillewin", codiceMillewin ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string nomeProblema = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                DateTime? dataApertura = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                                string gruppoComplicanza = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                                complications.Add((nomeProblema, dataApertura, gruppoComplicanza));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'estrazione complicanze: {ex.Message}");
                throw;
            }

            return complications;
        }

        #endregion

        #region Classificazione Gravità

        /// <summary>
        /// Classifica la gravità della complicanza
        /// </summary>
        public string ClassifyComplicationSeverity(string complicationType, string nomeProblema, string codiceMillewin)
        {
            if (string.IsNullOrWhiteSpace(nomeProblema))
                return null;

            string nomeUpper = nomeProblema.ToUpper();

            switch (complicationType)
            {
                case "Nefropatia":
                    return ClassifyNephropathy(nomeUpper, codiceMillewin);
                case "Retinopatia":
                    return ClassifyRetinopathy(nomeUpper);
                case "Neuropatia":
                    return ClassifyNeuropathy(nomeUpper);
                case "Arteriopatia periferica":
                    return ClassifyArteriopathy(nomeUpper);
                case "Altre complicanze":
                    return ClassifyOtherComplications(nomeUpper);
                default:
                    return null;
            }
        }

        private string ClassifyNephropathy(string nomeUpper, string codiceMillewin)
        {
            if (nomeUpper.Contains("INSUFFICIENZA RENALE CRONICA") ||
                nomeUpper.Contains("DIALISI") || nomeUpper.Contains("TRAPIANTO") ||
                nomeUpper.Contains("STADIO 4") || nomeUpper.Contains("STADIO 5") ||
                nomeUpper.Contains("VFG") && (nomeUpper.Contains("<30") || nomeUpper.Contains("< 30")))
                return "Grave";

            if (nomeUpper.Contains("PROTEINURIA") || nomeUpper.Contains("MICROALBUMINURIA") ||
                nomeUpper.Contains("ALBUMINURIA") || nomeUpper.Contains("STADIO 3"))
                return "Moderato";

            if (nomeUpper.Contains("INIZIALE") || nomeUpper.Contains("STADIO 1") ||
                nomeUpper.Contains("STADIO 2") || nomeUpper.Contains("VFG >60"))
                return "Lieve";

            return null;
        }

        private string ClassifyRetinopathy(string nomeUpper)
        {
            if (nomeUpper.Contains("PROLIFERANTE") || nomeUpper.Contains("EDEMA MACULARE") ||
                nomeUpper.Contains("CECITÀ") || nomeUpper.Contains("CECITA") ||
                nomeUpper.Contains("IPOVISIONE GRAVE") || nomeUpper.Contains("RESIDUO VISIVO"))
                return "Grave";

            if (nomeUpper.Contains("MODERATA") || nomeUpper.Contains("ESSUDATI") ||
                nomeUpper.Contains("EMORRAGIE") || nomeUpper.Contains("IPOVISIONE"))
                return "Moderato";

            if (nomeUpper.Contains("LIEVE") || nomeUpper.Contains("MICROANEURISMI") ||
                nomeUpper.Contains("INIZIALE") || nomeUpper.Contains("NON PROLIFERANTE"))
                return "Lieve";

            return null;
        }

        private string ClassifyNeuropathy(string nomeUpper)
        {
            if (nomeUpper.Contains("PIEDE DIABETICO") || nomeUpper.Contains("CHARCOT") ||
                nomeUpper.Contains("GASTROPARESI") || nomeUpper.Contains("VESCICA NEUROLOGICA") ||
                nomeUpper.Contains("ULCERA") || nomeUpper.Contains("AMPUTAZIONE"))
                return "Grave";

            if (nomeUpper.Contains("DOLOROSA") || nomeUpper.Contains("DOLORE") ||
                nomeUpper.Contains("SINTOMATICA") || nomeUpper.Contains("PARESTESIE"))
                return "Moderato";

            if (nomeUpper.Contains("ASINTOMATICA") || nomeUpper.Contains("LIEVE") ||
                nomeUpper.Contains("INIZIALE") || nomeUpper.Contains("MINIMA"))
                return "Lieve";

            return null;
        }

        private string ClassifyArteriopathy(string nomeUpper)
        {
            if (nomeUpper.Contains("DOLORE A RIPOSO") || nomeUpper.Contains("GANGRENA") ||
                nomeUpper.Contains("ULCERA ISCHEMICA") || nomeUpper.Contains("ISCHEMIA CRITICA"))
                return "Grave";

            if (nomeUpper.Contains("CLAUDICATIO") || nomeUpper.Contains("CLAUDICAZIONE") ||
                nomeUpper.Contains("LESION") || nomeUpper.Contains("STENOSI"))
                return "Moderato";

            if (nomeUpper.Contains("ASINTOMATICA") || nomeUpper.Contains("LIEVE"))
                return "Lieve";

            return null;
        }

        private string ClassifyOtherComplications(string nomeUpper)
        {
            if (nomeUpper.Contains("CHETOACIDOSI") || nomeUpper.Contains("COMA") ||
                nomeUpper.Contains("IPOGLICEMIA GRAVE") || nomeUpper.Contains("SEPSI"))
                return "Grave";

            if (nomeUpper.Contains("INFEZIONE") || nomeUpper.Contains("CELLULITE") ||
                nomeUpper.Contains("ASCESSO"))
                return "Moderato";

            if (nomeUpper.Contains("LIEVE") || nomeUpper.Contains("MINORE"))
                return "Lieve";

            return null;
        }

        #endregion

        #region Lista Completa Complicanze

        /// <summary>
        /// Crea la lista completa delle complicanze con classificazione
        /// </summary>
        public List<DiabetesComplicationData> GetCompleteComplicationsList(string codiceMillewin)
        {
            var completeList = new List<DiabetesComplicationData>
            {
                new DiabetesComplicationData("Nefropatia"),
                new DiabetesComplicationData("Retinopatia"),
                new DiabetesComplicationData("Neuropatia"),
                new DiabetesComplicationData("Arteriopatia periferica"),
                new DiabetesComplicationData("Altre complicanze")
            };

            var dbComplications = GetDiabetesComplications(codiceMillewin);

            // CORREZIONE: Non usa decostruzione
            foreach (var item in dbComplications)
            {
                var nomeProblema = item.NomeProblema;
                var dataApertura = item.DataApertura;
                var gruppoComplicanza = item.GruppoComplicanza;
                
                var complication = completeList.Find(c => c.ComplicationType == gruppoComplicanza);
                
                if (complication != null)
                {
                    complication.IsPresent = "sì";
                    
                    if (!string.IsNullOrWhiteSpace(nomeProblema))
                    {
                        if (!string.IsNullOrWhiteSpace(complication.Notes))
                            complication.Notes += "; ";
                        complication.Notes += nomeProblema;
                    }

                    if (dataApertura.HasValue && 
                        (!complication.DateOpened.HasValue || dataApertura.Value < complication.DateOpened.Value))
                    {
                        complication.DateOpened = dataApertura;
                    }

                    string severity = ClassifyComplicationSeverity(gruppoComplicanza, nomeProblema, codiceMillewin);
                    if (!string.IsNullOrWhiteSpace(severity))
                    {
                        if (string.IsNullOrWhiteSpace(complication.Severity))
                        {
                            complication.Severity = severity;
                        }
                        else
                        {
                            if (severity == "Grave" ||
                                (severity == "Moderato" && complication.Severity == "Lieve"))
                            {
                                complication.Severity = severity;
                            }
                        }
                    }
                }
            }

            return completeList;
        }

        #endregion
    }
}
