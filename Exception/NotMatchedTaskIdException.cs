namespace scan_controller.Models.Exception
{
    public class NotMatchedTaskIdException : System.Exception
    {
        public readonly string CurTaskId;
        public readonly string InputTaskId;


        public NotMatchedTaskIdException(string inputTaskId, string curTaskId)
        {
            InputTaskId = inputTaskId;
            CurTaskId = curTaskId;
        }
    }
}