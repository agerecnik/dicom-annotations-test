using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DicomAnnotationsDrawing.Models
{
    public sealed class Annotation : INotifyPropertyChanged
    {
        private int _id;
        private double _x, _y, _width, _height;
        private bool _isSelected;

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); } }
        public double X { get => _x; set { _x = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); } }
        public double Y { get => _y; set { _y = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); } }
        public double Width { get => _width; set { _width = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); } }
        public double Height { get => _height; set { _height = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        public string DisplayText => $"Box #{Id}: ({(int)X}, {(int)Y}), {Width:0}×{Height:0}";
        public override string ToString() => DisplayText;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
