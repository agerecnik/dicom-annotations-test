using Dicom;
using DicomAnnotationsDrawing.Helpers;
using DicomAnnotationsDrawing.Models;
using DicomAnnotationsDrawing.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace DicomAnnotationsDrawing.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DialogService _dialogService;
        private readonly ObservableCollection<Annotation> _annotations = new();
        private Annotation? _selected;
        private BitmapSource? _image;
        private double _canvasWidth, _canvasHeight;
        private DicomDataset? _sourceDataset;
        private int _idSeed = 1;

        private readonly RelayCommand _deleteSelectedCommand;
        public RelayCommand DeleteSelectedCommand => _deleteSelectedCommand;

        private readonly RelayCommand _openDicomCommand;
        public RelayCommand OpenDicomCommand => _openDicomCommand;

        private readonly RelayCommand _exportSrCommand;
        public RelayCommand ExportSrCommand => _exportSrCommand;

        private readonly RelayCommand _selectAnnotationCommand;
        public RelayCommand SelectAnnotationCommand => _selectAnnotationCommand;

        private readonly RelayCommand _addAnnotationCommand;
        public RelayCommand AddAnnotationCommand => _addAnnotationCommand;

        public MainViewModel() : this(new DialogService()) { }
        public MainViewModel(DialogService dialogService) {
            _dialogService = dialogService;

            _openDicomCommand = new RelayCommand(_ => OpenDicom());
            _deleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => Selected != null);
            _exportSrCommand = new RelayCommand(_ => ExportSr(), _ => _sourceDataset != null && _annotations.Any());
            _selectAnnotationCommand = new RelayCommand(a => Selected = a as Annotation);
            _addAnnotationCommand = new RelayCommand(p =>
            {
                if (p is System.Windows.Rect r)
                {
                    Add(r.X, r.Y, r.Width, r.Height);
                }
            });
        }

        public ObservableCollection<Annotation> Annotations => _annotations;

        public Annotation? Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                {
                    return;
                }
                if (_selected != null)
                {
                    _selected.IsSelected = false;
                }
                _selected = value;
                if (_selected != null)
                {
                    _selected.IsSelected = true;
                }
                OnPropertyChanged();
                DeleteSelectedCommand.RaiseCanExecuteChanged();
            }
        }

        public BitmapSource? Image
        {
            get => _image; set { _image = value; OnPropertyChanged(); }
        }

        public double CanvasWidth { get => _canvasWidth; set { _canvasWidth = value; OnPropertyChanged(); } }
        public double CanvasHeight { get => _canvasHeight; set { _canvasHeight = value; OnPropertyChanged(); } }

        public void OpenDicom()
        {
            var path = _dialogService.OpenDicom();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var (img, bmp) = DicomLoader.Load(path);
            _sourceDataset = img.Dataset;
            Image = bmp;
            CanvasWidth = bmp.PixelWidth;
            CanvasHeight = bmp.PixelHeight;

            _annotations.Clear();
            Selected = null;
            _idSeed = 1;
        }

        public Annotation Add(double x, double y, double w, double h)
        {
            var ann = new Annotation { Id = _idSeed++, X = x, Y = y, Width = w, Height = h };
            _annotations.Add(ann);
            Selected = ann;
            ExportSrCommand.RaiseCanExecuteChanged();
            return ann;
        }

        public void Remove(Annotation ann)
        {
            if (_annotations.Remove(ann))
            {
                if (ReferenceEquals(Selected, ann))
                {
                    Selected = null;
                }
                ExportSrCommand.RaiseCanExecuteChanged();
            }
        }

        public void DeleteSelected()
        {
            if (Selected is null)
            {
                return;
            }
            var a = Selected;
            Selected = null;
            _annotations.Remove(a);
            ExportSrCommand.RaiseCanExecuteChanged();
        }

        public string? ExportSr()
        {
            if (_sourceDataset == null)
            {
                return null;
            }
            var path = _dialogService.SaveSr($"{_sourceDataset.GetString(DicomTag.SOPInstanceUID)}.dcm");
            var result = SrExporter.ExportTID1500(_annotations, _sourceDataset, path);
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}