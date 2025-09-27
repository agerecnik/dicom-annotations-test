using Microsoft.Win32;

namespace DicomAnnotationsDrawing.Services
{
    public class DialogService
    {
        public string? OpenDicom()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open DICOM Image",
                Filter = "DICOM files (*.dcm;*.dicom)|*.dcm;*.dicom|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string SaveSr(string fileName)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save DICOM SR (TID 1500)",
                Filter = "DICOM (*.dcm)|*.dcm",
                FileName = fileName
            };
            return dlg.ShowDialog() == true ? dlg.FileName : fileName;
        }
    }
}