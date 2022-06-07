namespace scan_controller.Models.Exception
{
    public class ConcurrentFileAccessException : System.Exception
    {
        public readonly string DirName;


        public ConcurrentFileAccessException(string dirName)
        {
            DirName = dirName;
        }
    }
}