namespace scan_controller.Models.Exception
{
    // 요청한 Task와 현재 작업 중인 Task의 id가 일치하지 않을 때
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