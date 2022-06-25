namespace scan_controller.Models.Exception
{
    // 요청한 스캔 모드의 값으로 잘못된 값이 입력되었을때
    public class ScanModeValueException : System.Exception
    {
        public readonly string ModeName;
        public readonly string Value;


        public ScanModeValueException(string modeName, string value)
        {
            ModeName = modeName;
            Value = value;
        }
    }
}