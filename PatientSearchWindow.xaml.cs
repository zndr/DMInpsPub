using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace DMInps
{
    /// <summary>
    /// Finestra modale per l'inserimento e la validazione del codice fiscale del paziente
    /// </summary>
    public partial class PatientSearchWindow : Window
    {
        // Regex per validazione codice fiscale italiano (formato standard)
        private const string CodiceFiscaleRegex = 
            @"(?:(?:[B-DF-HJ-NP-TV-Z]|[AEIOU])[AEIOU][AEIOUX]|[B-DF-HJ-NP-TV-Z]{2}[A-Z]){2}[\dLMNP-V]{2}(?:[A-EHLMPR-T](?:[04LQ][1-9MNP-V]|[1256LMRS][\dLMNP-V])|[DHPS][37PT][0L]|[ACELMRT][37PT][01LM])(?:[A-MZ][1-9MNP-V][\dLMNP-V]{2}|[A-M][0L](?:[1-9MNP-V][\dLMNP-V]|[0L][1-9MNP-V]))[A-Z]";

        /// <summary>
        /// Codice fiscale validato inserito dall'utente
        /// </summary>
        public string CodiceFiscale { get; private set; }

        public PatientSearchWindow()
        {
            InitializeComponent();
            txtCodiceFiscale.Focus();
            
            // Gestione Enter per conferma
            txtCodiceFiscale.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    BtnConferma_Click(null, null);
                }
            };
        }

        /// <summary>
        /// Gestisce il click sul pulsante Conferma
        /// Valida il codice fiscale e chiude la finestra se valido
        /// </summary>
        private void BtnConferma_Click(object sender, RoutedEventArgs e)
        {
            string cf = txtCodiceFiscale.Text.Trim().ToUpper();

            // Verifica che il campo non sia vuoto
            if (string.IsNullOrEmpty(cf))
            {
                MostraErrore("Il codice fiscale non può essere vuoto.");
                return;
            }

            // Verifica lunghezza
            if (cf.Length != 16)
            {
                MostraErrore("Il codice fiscale deve essere lungo esattamente 16 caratteri.");
                return;
            }

            // Valida formato con regex
            if (!ValidaCodiceFiscale(cf))
            {
                MostraErrore("Codice fiscale non valido. Verificare il formato e riprovare.");
                return;
            }

            // Codice fiscale valido
            CodiceFiscale = cf;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Gestisce il click sul pulsante Annulla
        /// </summary>
        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Valida il formato del codice fiscale usando regex
        /// </summary>
        /// <param name="cf">Codice fiscale da validare</param>
        /// <returns>True se il formato è valido</returns>
        private bool ValidaCodiceFiscale(string cf)
        {
            if (string.IsNullOrEmpty(cf) || cf.Length != 16)
            {
                return false;
            }

            return Regex.IsMatch(cf, CodiceFiscaleRegex);
        }

        /// <summary>
        /// Mostra un messaggio di errore nella finestra
        /// </summary>
        /// <param name="messaggio">Messaggio da visualizzare</param>
        private void MostraErrore(string messaggio)
        {
            txtErrore.Text = messaggio;
            borderErrore.Visibility = Visibility.Visible;
            txtCodiceFiscale.Focus();
            txtCodiceFiscale.SelectAll();
        }

        /// <summary>
        /// Nasconde il messaggio di errore
        /// </summary>
        private void NascondiErrore()
        {
            borderErrore.Visibility = Visibility.Collapsed;
            txtErrore.Text = string.Empty;
        }
    }
}
