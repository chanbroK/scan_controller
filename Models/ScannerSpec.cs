using System.Collections.Generic;

namespace scan_controller.Models
{
    public class ScannerSpec
        // Response로 전송될때 enum은 int 형식으로 변환된다. -> client 를 위해 string 형식으로 저장
    {
        // 색상 방식 [ 흑백, 회색, 컬러]
        public List<string> colorMode { get; set; }

        // DPI
        public List<string> dpiMode { get; set; }

        // 급지 방식 [스캔(단면),자동급지(단면), 자동급지(양면)] 
        public List<string> feederMode { get; set; }

        // 용지 뒤집는 방식 [book, fanfold] 
        public List<string> flipMode { get; set; }

        // scanner name 
        public string? name { get; set; }

        // 용지 방향 [vertical, horizontal]
        public List<string> paperDirection { get; set; }
        // 용지 크기 [ A3, A4, ... B3, B4...]
        public List<string> paperSizeMode { get; set; }

        public ScannerSpec()
        {
            colorMode = new List<string>();
            dpiMode = new List<string>();
            feederMode = new List<string>();
            flipMode = new List<string>();
            paperDirection = new List<string>();
            paperSizeMode = new List<string>();
        }
    }
}