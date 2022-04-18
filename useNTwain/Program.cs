using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using NTwain;
using NTwain.Data;

namespace useNTwain
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(PlatformInfo.Current.IsApp64Bit ? "System is 64 bit" : "System is 32 bit");
            var twain = InitTwain();
            var code = twain.Open();

            var dataSource = GetSourceList(twain)[0];
            Console.WriteLine(code.ToString());
            dataSource.Open();
            dataSource.Capabilities.ICapPixelType.SetValue(PixelType.Gray);


            Scan(dataSource);

            // 작업 마무리 후 close해서 연결 해제
            Console.ReadLine();
            Console.WriteLine("Main Thread 종료");

            // dataSource.Close();
            twain.Close();
        }


        private static void Scan(DataSource ds)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    ds.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            });
        }

        private static List<string> GetCapabilities(DataSource ds)
        {
            var result = new List<string>
            {
                ds.Capabilities.ICapPixelType.GetCurrent().ToString(),
                ds.Capabilities.ICapFilter.GetCurrent().ToString()
            };
            // Capabilities로 객체화 되어있음
            // getter, setter를 이용하여 값 변경
            return result;
        }

        private static List<DataSource> GetSourceList(TwainSession twain)
        {
            var dataSource = twain.GetSources().ToList();
            return dataSource;
        }


        private static TwainSession InitTwain()
        {
            var twain = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                Assembly.GetExecutingAssembly()));
            // 상태 확인을 위한 handler? 추가
            twain.TransferReady += (s, e) => { Console.WriteLine("Got xfer Ready"); };
            twain.DataTransferred += (s, e) =>
            {
                var stream = e.GetNativeImageStream();
                var img = Image.FromStream(stream);
                img.Save("D://test.png");
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "SUCCESS! Got twain data"
                    : "FAILED! No twain data");
            };
            twain.SourceDisabled += (s, e) =>
            {
                Console.WriteLine("Source disabled");
                twain.CurrentSource.Close();
                twain.Close();
            };
            return twain;
        }
    }
}