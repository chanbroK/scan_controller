namespace scan_controller.Exception;

// 정의란 에러에 대한 상위 Exception
public class BusinessException : System.Exception
{
    public new string? Message;
    public int StatusCode;
}