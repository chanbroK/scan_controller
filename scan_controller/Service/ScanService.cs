using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NTwain;
using NTwain.Data;
using SixLabors.ImageSharp;

namespace scan_controller.Service
{
    public class ScanService
    {
        private static TwainSession _session;
        private static DataSource _dataSource;
        private static string _savePath = "D://";
        private static string _fileName = "default";

        public ScanService()
        {
            if (_session == null) InitSession();
        }

        private void InitSession()
        {
            Console.WriteLine(PlatformInfo.Current.IsApp64Bit ? "Server Running on 64bit" : "Server Running on 32Bit");
            // Set NTwain read twain_32.dll
            PlatformInfo.Current.PreferNewDSM = false;
            Console.WriteLine("Loaded DSM =" + PlatformInfo.Current.ExpectedDsmPath);
            // Create Twain Session
            _session = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                Assembly.GetExecutingAssembly()));
            // Twain Session Open 
            _session.Open();
            // Session 상태 별 handler 설정
            _session.TransferReady += (s, e) => { Console.WriteLine("Got xfer Ready"); };
            _session.DataTransferred += (s, e) =>
            {
                // TODO pdf로 저장 naps2 참고
                var stream = e.GetNativeImageStream();
                var img = Image.Load(stream);
                // TODO 파일 이름 정하기
                // _savePath 변수를 읽는 시점은 해당 이벤트가 발생하였을때
                img.Save(_savePath + _fileName + ".png");
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "SUCCESS! Got twain data"
                    : "FAILED! No twain data");
            };
            _session.SourceDisabled += (s, e) =>
            {
                Console.WriteLine("Source disabled");
                _session.CurrentSource.Close();
                // _session.Close();
            };
        }

        public List<DataSource> GetDataSourceList()
        {
            var result = _session.GetSources().ToList();
            return result;
        }

        public void SetDataSource(int id)
        {
            // 이전의 datasource close
            if (_dataSource != null && _dataSource.IsOpen) _dataSource.Close();
            _dataSource = _session.GetSources().ToList()[id];
            _dataSource.Open();
        }

        public string Scan(string fileName)
        {
            _fileName = fileName;
            return Scan();
        }

        public string Scan()
        {
            // TODO Thread 처리를 통해서 scan 중에서도 작업 가능하도록
            // TODO 기존 Task가 존재하면, 대기 
            if (_dataSource == null)
                // use default datasource
                _dataSource = _session.DefaultSource;
            if (!_dataSource.IsOpen) _dataSource.Open();
            ThreadPool.QueueUserWorkItem(
                o => { _dataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero); });
            return _savePath + _fileName + ".png";
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