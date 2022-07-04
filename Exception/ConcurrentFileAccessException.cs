using System.Net;

namespace scan_controller.Exception;

public class ConcurrentFileAccessException : BusinessException
{
    public ConcurrentFileAccessException(string dirName)
    {
        StatusCode = (int) HttpStatusCode.Locked;
        Message = dirName + "에 저장하는 파일을 접근할 수 없습니다.";
    }
}