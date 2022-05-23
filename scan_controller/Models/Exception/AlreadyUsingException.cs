namespace scan_controller.Models.Exception
{
    public class AlreadyUsingException : System.Exception
    {
        public string newTaskId;
        public string oldTaskId;


        public AlreadyUsingException(string newTaskId, string oldTaskId)
        {
            this.newTaskId = newTaskId;
            this.oldTaskId = oldTaskId;
        }
    }
}