using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NTwain;
using NTwain.Data;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using scan_controller.Models;
using scan_controller.Util;
using SixLabors.ImageSharp;

namespace scan_controller.Service
{
    public class ScanService
    {
        private static TwainSession _session;
        private static DataSource _dataSource;
        private static string _savePath = "D://scan_controller_test/";
        private static string _fileName = "default";
        private static string _fileExt = ".pdf";
        private readonly List<Stream> streamList = new List<Stream>();


        public ScanService()
        {
            Console.WriteLine(PlatformInfo.Current.IsApp64Bit
                ? "Server Running on 64bit"
                : "Server Running on 32Bit");
            // Set NTwain read twain_32.dll
            PlatformInfo.Current.PreferNewDSM = false;
            Console.WriteLine("Loaded DSM =" + PlatformInfo.Current.ExpectedDsmPath);
            // Create Twain Session
            _session = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                Assembly.GetExecutingAssembly()));

            // Session 상태 별 handler 설정
            // DSM이 load되어 Session이 생성될때 handler가 등록됨(추후 close 되어도 유지된다.)
            _session.TransferReady += (s, e) => { Console.WriteLine("스캔 시작"); };
            _session.DataTransferred += (s, e) =>
            {
                Console.WriteLine("스캔 결과 전송 시작");
                var stream = e.GetNativeImageStream();
                streamList.Add(stream);
                //
                // if (_fileExt == ".pdf")
                //     SaveToPdf(stream);
                // else
                //     SaveToImage(stream);
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "스캔 성공"
                    : "스캔 실패");
            };
            _session.TransferError += (s, e) =>
            {
                // 스캔 결과 전송 에러 발생
            };
            _session.SourceDisabled += (s, e) =>
            {
                // sate5 -> state4
                Console.WriteLine("스캔 결과 전송 완료");
                SaveToFile();
                _dataSource.Close();
            };
            _session.SourceChanged += (s, e) =>
            {
                // 사용하는 DS가 변경 
            };
            _session.DeviceEvent += (s, e) =>
            {
                // DS의 자체 이벤트 발생
            };
            _session.PropertyChanged += (s, e) =>
            {
                // ? ? ? DS의 소유권 이전 ? 
            };
            _session.StateChanged += (s, e) =>
            {
                // state가 변경될 때 _session.state로 접근
            };

            OpenSession();
            // default datasource 설정 TODO remove
            _dataSource = _session.GetSources().ToList()[0];
        }

        private void OpenSession()
        {
            // Twain Session Open 
            _session.Open();
            Console.WriteLine("Session[Open]");
        }

        public void DeleteSession()
        {
            if (_dataSource.IsOpen) _dataSource.Close();
            if (_session.IsDsmOpen) _session.Close();
            Console.WriteLine("Session[Released]");
        }

        public List<DataSource> GetDataSourceList()
        {
            if (!_session.IsDsmOpen) OpenSession();
            var result = _session.GetSources().ToList();
            return result;
        }

        public void SetDataSource(int id)
        {
            // 이전의 datasource close
            if (!_session.IsDsmOpen) OpenSession();
            if (_dataSource != null && _dataSource.IsOpen) _dataSource.Close();
            _dataSource = _session.GetSources().ToList()[id];
            _dataSource.Open();
        }

        public string Scan(string fileName, string fileExt)
        {
            _fileName = fileName;
            _fileExt = fileExt;
            return Scan();
        }

        public string Scan()
        {
            if (!_session.IsDsmOpen) OpenSession();
            if (!_dataSource.IsOpen) _dataSource.Open();
            ThreadPool.QueueUserWorkItem(
                o => { _dataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero); });
            return _savePath + _fileName + _fileExt;
        }

        public string GetSavePath()
        {
            return _savePath;
        }

        public void SetSavePath(string newPath)
        {
            _savePath = newPath;
        }

        public ScannerSpec GetScannerSpec(int id)
        {
            if (!_session.IsDsmOpen) OpenSession();
            _dataSource = _session.GetSources().ToList()[id];
            if (!_dataSource.IsOpen) _dataSource.Open();


            var spec = new ScannerSpec();

            var caps = _dataSource.Capabilities;

            // 스캐너 이름
            spec.name = _dataSource.Name;
            // 색상 방식
            foreach (var v in caps.ICapPixelType.GetValues()) spec.colorMode.Add(v.ToString());

            // DPI 설정
            foreach (var v in caps.ICapXResolution.GetValues())
                // X,Y 값이 다를 수 있음 주의
                spec.dpiMode.Add(v.ToString());

            // 급지 방식
            spec.feederMode.Add("flated");
            if (caps.CapFeederEnabled.IsSupported)
            {
                spec.feederMode.Add("ADF(one-side)");
                if (caps.CapDuplexEnabled.IsSupported) spec.feederMode.Add("ADF(two-side)");
            }

            // 용지 뒤집는 방식

            if (caps.ICapFlipRotation.IsSupported)
            {
                spec.flipMode.Add(FlipRotation.Fanfold.ToString());
                spec.flipMode.Add(FlipRotation.Book.ToString());
            }

            // 용지 크기
            foreach (var v in caps.ICapSupportedSizes.GetValues())
                if (!v.Equals(SupportedSize.None))
                    spec.paperSizeMode.Add(v.ToString());

            _dataSource.Close();
            return spec;
        }

        public void SetCapability(ScanMode scanMode)
        {
            if (!_session.IsDsmOpen) OpenSession();
            if (!_dataSource.IsOpen) _dataSource.Open();

            var caps = _dataSource.Capabilities;

            // 색상 방식
            caps.ICapPixelType.SetValue(EnumUtil<PixelType>.Parse(scanMode.colorMode));

            // DPI 설정 (NOT Enum)
            var dpi = short.Parse(scanMode.dpiMode);
            var t = new TWFix32();
            t.Whole = dpi;
            caps.ICapXResolution.SetValue(t);

            // caps.ICapXResolution.SetValue(EnumUtil<TWFix32>.Parse(scanMode.dpiMode));

            // 급지 방식
            if (scanMode.feederMode == "flated")
            {
                // 스캔
                caps.CapFeederEnabled.SetValue(BoolType.False);
            }
            else
            {
                // ADF
                caps.CapFeederEnabled.SetValue(BoolType.True);
                if (scanMode.feederMode.Contains("one-side"))
                    // 단면 ADF
                    caps.CapDuplexEnabled.SetValue(BoolType.False);

                if (scanMode.feederMode.Contains("two-side"))
                {
                    // 양면 ADF
                    caps.CapDuplexEnabled.SetValue(BoolType.True);
                    //용지 뒤집는 방식
                    if (caps.ICapFlipRotation.IsSupported)
                        caps.ICapFlipRotation.SetValue(EnumUtil<FlipRotation>.Parse(scanMode.flipMode));
                }
            }

            // 용지 크기
            caps.ICapSupportedSizes.SetValue(EnumUtil<SupportedSize>.Parse(scanMode.paperSizeMode));

            // _dataSource.Close();
        }


        private List<string> SaveToFile()
        {
            var fileNameList = new List<string>();
            Console.WriteLine(streamList.Count);
            if (_fileExt == ".pdf")
            {
                var doc = new PdfDocument();
                for (var i = 0; i < streamList.Count; i++)
                {
                    doc.Pages.Add(new PdfPage());
                    var xgr = XGraphics.FromPdfPage(doc.Pages[i]);
                    var img = XImage.FromStream(streamList[i]);
                    xgr.DrawImage(img, 0, 0);
                }

                doc.Save(_savePath + _fileName + _fileExt);
                fileNameList.Add(_savePath + _fileName + _fileExt);
                doc.Close();
            }
            else
            {
                for (var i = 0; i < streamList.Count; i++)
                {
                    var fileName = _savePath + _fileName + "_" + i + _fileExt;
                    Image.Load(streamList[i]).Save(fileName);
                    fileNameList.Add(fileName);
                }
            }
            streamList.Clear();
            return fileNameList;
        }


        // private void SaveToImage()
        // {
        //     var img = Image.Load(stream);
        //     img.Save(_savePath + _fileName + _fileExt);
        // }
        //
        // private void SaveToPdf()
        // {
        //     //https://www.c-sharpcorner.com/blogs/pdf-sharp-use-to-image-to-pdf-covert2
        //     var doc = new PdfDocument();
        //     doc.Pages.Add(new PdfPage());
        //     var xgr = XGraphics.FromPdfPage(doc.Pages[0]);
        //     var img = XImage.FromStream(stream);
        //     xgr.DrawImage(img, 0, 0);
        //     doc.Save(_savePath + _fileName + _fileExt);
        //     doc.Close();
        // }
    }
}