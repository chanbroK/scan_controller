using System;

namespace scan_controller.Util
{
    public static class EnumUtil<T>
    {
        public static T Parse(string s)
        {
            return (T) Enum.Parse(typeof(T), s);
        }
    }
}