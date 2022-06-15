namespace scan_controller.Models
{
    public class ScanMode
    {
        // 색상 방식 [ 흑백, 회색, 컬러]
        public string? colorMode { get; set; }

        // DPI
        public string? dpiMode { get; set; }

        // 급지 방식 [스캔(단면),자동급지(단면), 자동급지(양면)] 
        public string? feederMode { get; set; }

        // 용지 뒤집는 방식 [book, fanfold] 
        public string? flipMode { get; set; }

        // 용지 방향 [vertical, horizontal] 
        public string? paperDirection { get; set; }

        // 용지 크기 [ A3, A4, ... B3, B4...]
        public string? paperSizeMode { get; set; }

        // LegacyUI
        public bool showLegacyUI { get; set; }
    }
}