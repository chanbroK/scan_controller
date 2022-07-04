using System.Net;

namespace scan_controller.Exception;

// 스캔 결과를 저장시 스캔 데이터 스트림이 없을 때
public class NoDataToSaveException : BusinessException
{
    public NoDataToSaveException()
    {
        StatusCode = (int) HttpStatusCode.NotFound;
        Message = "파일로 저장할 데이터가 없습니다.";
    }
}