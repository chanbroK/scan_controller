using System.Net;

namespace scan_controller.Exception;

// 서버에서 수행중인 Task가 없을 때
public class NoTaskException : BusinessException
{
    public NoTaskException()
    {
        StatusCode = (int) HttpStatusCode.NotFound;
        Message = "서버에서 수행중인 Task가 없습니다.";
        Name = GetType().Name;
    }
}