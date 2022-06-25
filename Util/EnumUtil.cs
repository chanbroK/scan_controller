using System;

namespace scan_controller.Util
{
    public static class EnumUtil<T>
    {
        /*
        String 값과 동일한 이름의 Enum(T) 값으로 매핑한다.
        T -> 변환할 Enum 대상
        s -> String value
        return Enum 객체 반환
        */
        public static T Parse(string s)
        {
            return (T) Enum.Parse(typeof(T), s);
        }
    }
}