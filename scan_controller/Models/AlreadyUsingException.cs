using System;

namespace scan_controller.Models
{
    public class AlreadyUsingException : Exception
    {
        private string _taskId;

        public AlreadyUsingException(string taskId)
        {
            _taskId = taskId;
        }
    }
}