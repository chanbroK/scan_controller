using System;

namespace scan_controller.Models
{
    public class AlreadyUsingException : Exception
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