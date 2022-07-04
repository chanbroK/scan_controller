using System;
using System.Security.Cryptography;
using System.Text;

namespace scan_controller.Util
{
    public static class HashUtil
    {
        /*
        UUID와 같이 식별 값 생성
        "-"이 포함되기 때문에 replace 해서 사용함.
        return -> 생성된 GUID 반환
        */
        public static string GetGuid()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
    }
}