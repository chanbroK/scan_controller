namespace scan_controller.Models.Exception
{
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