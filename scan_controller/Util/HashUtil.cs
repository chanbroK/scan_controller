using System;
using System.Security.Cryptography;
using System.Text;

namespace scan_controller.Util
{
    public static class HashUtil
    {
        public static string GetRandomId(int size)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var idArray = new char[8];
            var random = new Random();

            for (var i = 0; i < idArray.Length; i++) idArray[i] = characters[random.Next(characters.Length)];

            return new string(idArray);
        }

        public static string GetMD5Id()
        {
            var curTimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            var md5Str = new StringBuilder();
            var byteArr = Encoding.ASCII.GetBytes(curTimeStamp);
            var resultArr = new MD5CryptoServiceProvider().ComputeHash(byteArr);

            foreach (var t in resultArr) md5Str.Append(t.ToString("X2"));
            return md5Str.ToString();
        }

        public static string GetGuid()
        {
            // TODO remove 
            // return Guid.NewGuid().ToString().Replace("-", "");
            return "test";
        }
    }
}