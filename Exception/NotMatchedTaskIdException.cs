using System.Net;

namespace scan_controller.Exception;

// 요청한 Task와 현재 작업 중인 Task의 id가 일치하지 않을 때
public class NotMatchedTaskIdException : BusinessException
{
    public NotMatchedTaskIdException(string inputTaskId, string curTaskId)
    {
        StatusCode = (int) HttpStatusCode.Conflict;
        Message = "입력한 Task의 id[" + inputTaskId + "]가 현재 서버에서 작업 중인 Task의 id[" + curTaskId + "]와 일치하지 않습니다.";
        Name = GetType().Name;
    }
}