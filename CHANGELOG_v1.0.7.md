# Storico Versioni - DMInps

## Versione 1.0.7 - 10/12/2024

### Nuove Funzionalita'
- **Inserimento manuale dati medico**
  - Possibilita' di inserire manualmente i dati del medico quando il database non e' disponibile
  - Fallback automatico su file JSON locale per i dati medici

- **Miglioramenti interfaccia utente**
  - Nuovo titolo applicazione: "DMInps - generatore relazione diabete per INPS"
  - Finestra "Formato Nome File" ora ridimensionabile verticalmente con barra di scorrimento
  - Sezione "Separatore" sempre visibile nella finestra formato nome file
  - Corretta anteprima nome file (rimossi riferimenti ai controlli WPF)

### Modifiche
- Rimossa voce menu "Aiuto -> Debug Info" (non piu' necessaria)
- Rimozione codice obsoleto (GetDoctorCode, GetMedicoDataAsync)
- Correzione query con COALESCE per campi NULL
- Migliorata gestione errori connessione database

---

## Versione 1.0.6.2 - 21/11/2024

### Nuove Funzionalita'
- **Strumento di Diagnostica Registry e Database**
  - Aggiunto progetto console `debugEstraiCodiciMedici` per il debug della configurazione
  - Analisi dettagliata delle chiavi di registro ODBC (HKEY_CURRENT_USER e HKEY_LOCAL_MACHINE)
  - Verifica automatica della connessione al database PostgreSQL
  - Estrazione e visualizzazione dell'elenco completo dei medici dal database
  - Output colorato per distinguere successi, warning e errori
  - Tracciamento completo di ogni passo eseguito durante l'analisi

- **Menu "Aiuto -> Debug Info"**
  - Aggiunta nuova voce di menu Debug Info
  - Avvio automatico dello strumento di diagnostica dall'applicazione principale
  - Ricerca intelligente dell'eseguibile di debug in percorsi multipli
  - Gestione robusta degli errori con messaggi informativi dettagliati

### Miglioramenti Tecnici
- Risolti errori di compilazione CS0579 (attributi assembly duplicati)
- Aggiunta proprieta' `GenerateAssemblyInfo=false` e `GenerateTargetFrameworkAttribute=false`
- Ottimizzazione della gestione dei percorsi relativi per il progetto di debug
- Supporto per eseguibili compilati sia con che senza RuntimeIdentifier (win-x64)

### Documentazione
- Documentazione completa dello strumento di diagnostica
- Guide per la compilazione e distribuzione del progetto debug
- Script PowerShell e Batch per l'avvio rapido del debug tool

---

## Versione 1.0.6.1 - [Data Precedente]

### Correzioni
- Ottimizzazioni varie della connessione al database
- Miglioramenti alla gestione degli errori

---

## Versione 1.0.6 - [Data Precedente]

### Nuove Funzionalita'
- Sistema di selezione medico tramite file JSON locale
- Caricamento elenco medici dal database all'avvio
- Salvataggio configurazione medici in `%LocalAppData%\dgzani\DMInps\medici.json`

### Miglioramenti
- Eliminata dipendenza da chiave Registry `doctorCodes`
- Ricerca pazienti estesa a tutti i medici configurati
- Lazy initialization della stringa di connessione al database
- Gestione migliorata delle eccezioni di connessione

### Database
- Query ottimizzate con clausole IN dinamiche per medici multipli
- Supporto per ricerca pazienti attraverso tutto il database medici

---

## Versione 1.0.5 - [Data Precedente]

### Nuove Funzionalita'
- Analisi avanzata del tipo di trattamento (farmacologico/dietetico)
- Calcolo automatico del compenso glicemico
- Gestione valori laboratorio con conversione unita' di misura

### Miglioramenti
- Interfaccia utente migliorata con 9 sezioni distinte
- Validazione dati pazienti piu' robusta
- Gestione errori specifica per pazienti senza diabete registrato

---

## Versione 1.0.4 - [Data Precedente]

### Nuove Funzionalita'
- Generazione PDF con QuestPDF
- Sezioni per complicanze e classificazione diabete
- Supporto per note cliniche personalizzate

### Miglioramenti
- Ottimizzazione query database con JOIN multipli
- Gestione migliorata dei valori NULL dal database

---

## Versione 1.0.3 - [Data Precedente]

### Nuove Funzionalita'
- Connessione diretta al database PostgreSQL Millewin
- Estrazione automatica dati paziente e medico
- Rilevamento automatico tipo diabete (DM1/DM2)

### Miglioramenti
- Gestione centralizzata della connessione database (Singleton pattern)
- Sistema di logging con System.Diagnostics.Debug

---

## Versione 1.0.2 - [Data Precedente]

### Miglioramenti
- Lettura configurazione da Registry di Windows
- Supporto chiavi ODBC multiple (mille_MillePS, milleps)
- Fallback automatico HKEY_CURRENT_USER -> HKEY_LOCAL_MACHINE

---

## Versione 1.0.1 - [Data Precedente]

### Nuove Funzionalita'
- Interfaccia grafica WPF iniziale
- Struttura base del progetto

---

## Versione 1.0.0 - [Data Iniziale]

### Release Iniziale
- Progetto base creato
- Configurazione .NET 8
- Struttura cartelle del progetto

---

## Statistiche Progetto

- **Linguaggio**: C# (.NET 8)
- **Framework UI**: WPF
- **Database**: PostgreSQL (Millewin)
- **Librerie Principali**:
  - Npgsql 8.0.5 (Database)
  - QuestPDF 2024.10.3 (Generazione PDF)
  - SkiaSharp 3.119.1 (Grafica)

---

## Roadmap Futura

### Funzionalita' Pianificate
- [ ] Campi PDF modificabili dopo la generazione
- [ ] Esportazione dati in formato Excel
- [ ] Storico certificati generati
- [ ] Template PDF personalizzabili
- [ ] Sistema di aggiornamento automatico
- [ ] Backup automatico configurazione

### Miglioramenti Tecnici
- [ ] Cache locale per dati paziente
- [ ] Sistema di logging su file
- [ ] Telemetria errori (opzionale)
- [ ] Test automatizzati

---

## Informazioni Sviluppo

**Autore**: Dario Giorgio Zani
**Organizzazione**: MMG Lumezzane (BS)
**Licenza**: Proprietaria
**Repository**: Locale

---

## Supporto

Per segnalazioni bug o richieste di funzionalita':
- Contatta lo sviluppatore per assistenza tecnica

---

## Requisiti di Sistema

- **Sistema Operativo**: Windows 10/11 (64-bit)
- **Framework**: .NET 8.0 Runtime (incluso in distribuzione self-contained)
- **RAM**: Minimo 2GB
- **Spazio Disco**: 100MB
- **Software Richiesto**: Millewin (per accesso database PostgreSQL)
- **Permessi**: Accesso Registry di Windows

---

*Ultimo aggiornamento: 10 Dicembre 2024*
