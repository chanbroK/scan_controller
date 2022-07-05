using System.Net;

namespace scan_controller.Exception;

// 요청한 스캔 모드의 값으로 잘못된 값이 입력되었을때
public class ScanModeValueException : BusinessException
{
    public ScanModeValueException(string modeName, string value)
    {
        StatusCode = (int) HttpStatusCode.BadRequest;
        Message = "ScanMode에 입력된 " + modeName + "이 " + value + "값을 지원하지 않습니다.";
        Name = GetType().Name;
    }
}