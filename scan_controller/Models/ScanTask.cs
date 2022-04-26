namespace scan_controller.Models
{
    public class ScanTask
    {
        public ScanMode scanMode;

        public string fileName { get; set; } = "";

        public string fileExt { get; set; } = "";


        // TODO scan spec에서 선택한 옵션들을 저장
    }
}