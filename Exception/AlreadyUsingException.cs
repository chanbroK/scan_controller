namespace scan_controller.Models.Exception
{
    // 해당 서버에 이미 다른 Task가 있어서 명령 실행이 불가능할때
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