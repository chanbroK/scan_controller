using System;

namespace scan_controller.Util
{
    public static class RandomUtil
    {
        public static string GetRandomID(int size)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var idArray = new char[8];
            var random = new Random();

            for (var i = 0; i < idArray.Length; i++) idArray[i] = characters[random.Next(characters.Length)];

            return new string(idArray);
        }
    }
}