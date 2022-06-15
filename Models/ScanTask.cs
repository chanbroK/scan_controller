namespace scan_controller.Models
{
    public class ScanTask
    {
        public string? fileExt { get; set; }
        public string? id { get; set; }
        public bool isContinue { get; set; }
        public string? savePath { get; set; }
        public ScanMode? scanMode { get; set; }

    }
}