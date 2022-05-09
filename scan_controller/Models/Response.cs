namespace scan_controller.Models
{
    public class Response
    {
        private object _body;
        private int _code;
        private string _message;

        public Response(int code, object body, string message)
        {
            _code = code;
            _body = body;
            _message = message;
        }

        public object GetBody()
        {
            return _body;
        }

        public void SetBody(object body)
        {
            _body = body;
        }

        public int GetCode()
        {
            return _code;
        }

        public void SetCode(int code)
        {
            _code = code;
        }

        public string GetMessage()
        {
            return _message;
        }

        public void SetMessage(string message)
        {
            _message = message;
        }
    }
}