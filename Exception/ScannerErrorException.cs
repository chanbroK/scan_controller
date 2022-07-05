using System.Net;

namespace scan_controller.Exception;

// 스캔 도중 기기에 이상이 발생
public class ScannerErrorException : BusinessException
{
    public ScannerErrorException()
    {
        StatusCode = (int) HttpStatusCode.InternalServerError;
        Message = "스캔 도중 기기에 이상이 발생하였습니다.";
    }
}