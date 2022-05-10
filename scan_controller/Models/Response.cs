namespace scan_controller.Models
{
    public class Response
    {
        public Response(int code, object body, string message)
        {
            _code = code;
            _body = body;
            _message = message;
        }

        public object _body { get; set; }
        public int _code { get; set; }
        public string _message { get; set; }
    }
}