namespace scan_controller.Models
{
    public class ExceptionResponse
    {
        public ExceptionResponse(string message, string stackTrace)
        {
            this.message = message;
            this.stackTrace = stackTrace;
        }

        public string message { get; set; }
        public string stackTrace { get; set; }
    }
}