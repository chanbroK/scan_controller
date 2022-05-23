namespace scan_controller.Models.DTO
{
    public class Response
    {
        public Response(int code, object result, string message)
        {
            this.code = code;
            this.result = result;
            this.message = message;
        }

        public object result { get; set; }

        public int code { get; set; }

        public string message { get; set; }
    }
}