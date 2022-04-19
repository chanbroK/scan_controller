using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using NTwain;
using NTwain.Data;
using SixLabors.ImageSharp;


namespace scan_controller.Service
{
    public class ScanService
    {
        private static TwainSession _session;

        private static string _savePath = "D://test.png";
        
        
        public ScanService()
        {
            if (_session == null)
            {
                initSession();
            }
        }

        private void initSession()
        {
            Console.WriteLine(PlatformInfo.Current.IsApp64Bit ? "Server Running on 64bit" : "Server Running on 32Bit");
            // Set NTwain read twain_32.dll
            PlatformInfo.Current.PreferNewDSM = false;
            Console.WriteLine("Loaded DSM =" + PlatformInfo.Current.ExpectedDsmPath);
            // Create Twain Session
            _session = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                Assembly.GetExecutingAssembly()));
            // Session 상태 별 handler 설정
            
            _session.TransferReady += (s, e) => { Console.WriteLine("Got xfer Ready"); };
            _session.DataTransferred += (s, e) =>
            {
                // TODO 파일 경로에 대한 변수 참조가 언제 일어나는지? 해당 handler가 선언될때 생성되는지 혹은 호출될때 생성되는지
                // TODO pdf naps2 참고
                var stream = e.GetNativeImageStream();
                var img = Image.Load(stream);
                img.Save(_savePath);
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "SUCCESS! Got twain data"
                    : "FAILED! No twain data");
            };
            _session.SourceDisabled += (s, e) =>
            {
                Console.WriteLine("Source disabled");
                _session.CurrentSource.Close();
                _session.Close();
            };
        }

        public List<DataSource> getDataSourceList()
        {
            _session.Open();
            var result = _session.GetSources().ToList();
            _session.Close();
            return result;
        }

        public string getSavePath()
        {
            return _savePath;
        }

        public void setSavePath(string newPath)
        {
            _savePath = newPath;
        }
    }
}