namespace scan_controller.Models.Exception
{
    public class AlreadyUsingException : System.Exception
    {
        public readonly string CurTaskId;
        public readonly string InputTaskId;


        public AlreadyUsingException(string inputTaskId, string curTaskId)
        {
            InputTaskId = inputTaskId;
            CurTaskId = curTaskId;
        }
    }
}