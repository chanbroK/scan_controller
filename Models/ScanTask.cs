namespace scan_controller.Models
{
    public class ScanTask
    {
        // 스캔 결과 파일의 확장자 명
        public string? fileExt { get; set; }
        // Task 아이디
        public string? id { get; set; }
        // 해당 Task가 연속 스캔인지 여부
        public bool isContinue { get; set; }
        // 스캔 결과 저장 위치
        public string? savePath { get; set; }
        // 스캔 모드 
        public ScanMode? scanMode { get; set; }

    }
}