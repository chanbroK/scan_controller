namespace scan_controller.Models
{
    public class ScanMode
        // Response로 전송될때 enum은 int 형식으로 변환된다. -> client 를 위해 string 형식으로 저장
    {
        // 색상 방식 [ 흑백, 회색, 컬러]
        public string colorMode;

        // DPI
        public string dpiMode;

        // 급지 방식 [스캔(단면),자동급지(단면), 자동급지(양면)] 
        public string feederMode;

        // 용지 뒤집는 방식 [book, fanfold] 
        public string flipMode;

        // 용지 크기 [ A3, A4, ... B3, B4...]
        public string paperSizeMode;
    }
}