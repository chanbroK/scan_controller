namespace scan_controller.Models
{
    public class Response
    {
        public Response(int code, object body, string message)
        {
            this.code = code;
            this.body = body;
            this.message = message;
        }

        public object body { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
}