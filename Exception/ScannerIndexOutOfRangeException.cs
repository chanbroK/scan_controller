using System.Net;

namespace scan_controller.Exception;

// 스캐너 리스트 범위의 인덱스를 벗어난 입력값인 경우 
public class ScannerIndexOutOfRangeException : BusinessException
{
    public ScannerIndexOutOfRangeException(int index, int size)
    {
        StatusCode = (int) HttpStatusCode.BadRequest;
        Message = "사용가능한 스캐너의 개수는 " + size + "개이므로 " + index + "는 잘못되었습니다.";
    }
}