using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DMInps.Models;

namespace DMInps
{
    /// <summary>
    /// ViewModel per una singola complicanza con supporto per binding bidirezionale
    /// </summary>
    public class ComplicanzaViewModel : INotifyPropertyChanged
    {
        private string _isPresent;
        private string? _severity;
        private string? _notes;

        public string ComplicationType { get; set; }
        public DateTime? DateOpened { get; set; }

        // Valori originali per il ripristino
        public string OriginalIsPresent { get; set; }
        public string? OriginalSeverity { get; set; }
        public string? OriginalNotes { get; set; }

        public string IsPresent
        {
            get => _isPresent;
            set
            {
                _isPresent = value;
                OnPropertyChanged(nameof(IsPresent));
                OnPropertyChanged(nameof(IsPresentYes));
                OnPropertyChanged(nameof(IsPresentNo));
            }
        }

        public bool IsPresentYes
        {
            get => IsPresent?.ToLower() == "sì";
            set
            {
                if (value)
                {
                    IsPresent = "sì";
                    if (string.IsNullOrEmpty(Severity))
                        Severity = "Lieve";
                }
                OnPropertyChanged(nameof(IsPresentYes));
                OnPropertyChanged(nameof(IsPresentNo));
            }
        }

        public bool IsPresentNo
        {
            get => IsPresent?.ToLower() != "sì";
            set
            {
                if (value)
                {
                    IsPresent = "no";
                    Severity = null;
                }
                OnPropertyChanged(nameof(IsPresentYes));
                OnPropertyChanged(nameof(IsPresentNo));
            }
        }

        public string? Severity
        {
            get => _severity;
            set
            {
                _severity = value;
                OnPropertyChanged(nameof(Severity));
            }
        }

        public string? Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        // Opzioni per il dropdown del grado
        public List<string> GradoOptions { get; } = new List<string> { "", "Lieve", "Moderato", "Grave" };

        public ComplicanzaViewModel()
        {
            _isPresent = "no";
            ComplicationType = string.Empty;
            OriginalIsPresent = "no";
        }

        public ComplicanzaViewModel(DiabetesComplicationData data)
        {
            ComplicationType = data.ComplicationType;
            _isPresent = data.IsPresent ?? "no";
            _severity = data.Severity;
            _notes = data.Notes;
            DateOpened = data.DateOpened;

            // Salva i valori originali
            OriginalIsPresent = _isPresent;
            OriginalSeverity = _severity;
            OriginalNotes = _notes;
        }

        public void Reset()
        {
            IsPresent = OriginalIsPresent;
            Severity = OriginalSeverity;
            Notes = OriginalNotes;
        }

        public DiabetesComplicationData ToModel()
        {
            return new DiabetesComplicationData
            {
                ComplicationType = ComplicationType,
                IsPresent = IsPresent,
                Severity = IsPresentYes ? Severity : null,
                Notes = Notes,
                DateOpened = DateOpened
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Finestra per la modifica delle complicanze diabetiche prima della generazione PDF
    /// </summary>
    public partial class ComplicanzeEditorWindow : Window
    {
        public ObservableCollection<ComplicanzaViewModel> Complicanze { get; set; }
        public List<DiabetesComplicationData>? ResultComplicanze { get; private set; }
        public bool Confirmed { get; private set; }

        public ComplicanzeEditorWindow(List<DiabetesComplicationData> complications)
        {
            InitializeComponent();

            Complicanze = new ObservableCollection<ComplicanzaViewModel>();

            // Converti i dati in ViewModel
            if (complications != null && complications.Count > 0)
            {
                foreach (var c in complications)
                {
                    Complicanze.Add(new ComplicanzaViewModel(c));
                }
            }
            else
            {
                // Se non ci sono complicanze dal database, crea la lista standard
                CreateDefaultComplicanze();
            }

            ComplicanzeItemsControl.ItemsSource = Complicanze;
        }

        private void CreateDefaultComplicanze()
        {
            var defaultTypes = new[]
            {
                "Retinopatia diabetica",
                "Nefropatia diabetica",
                "Neuropatia diabetica",
                "Arteriopatia periferica",
                "Cardiopatia ischemica",
                "Piede diabetico",
                "Vasculopatia cerebrale"
            };

            foreach (var type in defaultTypes)
            {
                Complicanze.Add(new ComplicanzaViewModel
                {
                    ComplicationType = type,
                    IsPresent = "no",
                    Severity = null,
                    Notes = string.Empty,
                    OriginalIsPresent = "no",
                    OriginalSeverity = null,
                    OriginalNotes = string.Empty
                });
            }
        }

        private void BtnConferma_Click(object sender, RoutedEventArgs e)
        {
            ResultComplicanze = Complicanze.Select(c => c.ToModel()).ToList();
            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var c in Complicanze)
            {
                c.Reset();
            }
        }
    }
}
