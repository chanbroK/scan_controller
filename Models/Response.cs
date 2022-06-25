namespace scan_controller.Models
{
    public class Response
    {
        // response 객체
        public Response(int code, object result, string message)
        {
            this.code = code;
            this.result = result;
            this.message = message;
        }
        // 반환값이 있는 응답일 때 해당 필드에 저장
        public object result { get; set; }
        // 응답 코드
        public int code { get; set; }
        // 응답 메시지
        public string message { get; set; }
    }
}