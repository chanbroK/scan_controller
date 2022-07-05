using System.Net;

namespace scan_controller.Exception;

// ADF 방식 스캔 명령인데 ADF에 용지가 없을 때
public class NoPaperAdfException : BusinessException
{
    public NoPaperAdfException()
    {
        StatusCode = (int) HttpStatusCode.BadRequest;
        Message = "ADF에 용지가 없습니다.";
        Name = GetType().Name;
    }
}