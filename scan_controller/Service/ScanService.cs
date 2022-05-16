using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NTwain;
using NTwain.Data;
using PdfSharp;
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
        private readonly List<Stream> _streamList = new List<Stream>();
        private ScanTask _curTask;

        public ScanService()
        {
            Console.WriteLine(PlatformInfo.Current.IsApp64Bit
                ? "Server Running on 64bit"
                : "Server Running on 32Bit");

            // Set NTwain read twain_32.dll
            // PlatformInfo.Current.PreferNewDSM = false;

            // Set NTwain read twaindsm.dll
            PlatformInfo.Current.PreferNewDSM = true;

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
                _streamList.Add(stream);
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
                if (_curTask != null && !_curTask.isContinue)
                {
                    SaveToFile();
                    _dataSource.Close();
                }
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
            _dataSource = _session.GetSources().ToList()[2];
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


        public void StartTask(ScanTask newTask)
        {
            if (_curTask != null && _curTask.id != newTask.id) throw new AlreadyUsingException(newTask.id, _curTask.id);
            _curTask = newTask;
            Scan();
        }

        public void EndContinueScan(string taskId)
        {
            if (_curTask.id != taskId)
                throw new ArgumentException(_curTask.id + "!=" + taskId, nameof(taskId));
            SaveToFile();
        }

        private void Scan()
        {
            if (!_session.IsDsmOpen) OpenSession();
            if (!_dataSource.IsOpen) _dataSource.Open();

            SetCapability();

            ThreadPool.QueueUserWorkItem(
                o => { _dataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero); });
        }

        public ScannerSpec GetScannerCapability(int id)
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


            // 용지 방향
            spec.paperDirection.Add("vertical");
            spec.paperDirection.Add("horizontal");
            _dataSource.Close();
            return spec;
        }

        public int getState()
        {
            return _session.State;
        }

        private void SetCapability()
        {
            if (!_session.IsDsmOpen) OpenSession();
            if (!_dataSource.IsOpen) _dataSource.Open();

            var caps = _dataSource.Capabilities;

            // Legacy UI 삭제
            caps.CapIndicators.SetValue(EnumUtil<BoolType>.Parse(_curTask.scanMode.showLegacyUI.ToString()));

            // 색상 방식
            caps.ICapPixelType.SetValue(EnumUtil<PixelType>.Parse(_curTask.scanMode.colorMode));

            // DPI 설정 (NOT Enum)
            var dpi = int.Parse(_curTask.scanMode.dpiMode);
            caps.ICapXResolution.SetValue(dpi);
            caps.ICapYResolution.SetValue(dpi);

            // 급지 방식
            if (_curTask.scanMode.feederMode == "flated")
            {
                // 스캔
                caps.CapFeederEnabled.SetValue(BoolType.False);
            }
            else
            {
                // ADF
                caps.CapFeederEnabled.SetValue(BoolType.True);
                if (_curTask.scanMode.feederMode.Contains("one-side"))
                    // 단면 ADF
                    caps.CapDuplexEnabled.SetValue(BoolType.False);

                if (_curTask.scanMode.feederMode.Contains("two-side"))
                {
                    // 양면 ADF
                    caps.CapDuplexEnabled.SetValue(BoolType.True);
                    //용지 뒤집는 방식
                    if (caps.ICapFlipRotation.IsSupported)
                        caps.ICapFlipRotation.SetValue(EnumUtil<FlipRotation>.Parse(_curTask.scanMode.flipMode));
                }
            }

            // 용지 크기

            var size = EnumUtil<SupportedSize>.Parse(_curTask.scanMode.paperSizeMode);
            var direction = _curTask.scanMode.paperDirection;
            float width = 0, height = 0;
            switch (size)
            {
                case SupportedSize.USLetter:
                    width = 8.5f;
                    height = 11;
                    break;
                case SupportedSize.USLegal:
                    width = 8.5f;
                    height = 14;
                    break;
                case SupportedSize.A3:
                    width = 420 / 25.4f;
                    height = 297 / 25.4f;
                    break;
                case SupportedSize.A4:
                    if (direction.Equals("vertical"))
                    {
                        width = 210 / 25.4f;
                        height = 297 / 25.4f;
                    }
                    else
                    {
                        width = 297 / 25.4f;
                        height = 210 / 25.4f;
                    }

                    break;
                case SupportedSize.A5:
                    width = 148 / 25.4f;
                    height = 210 / 25.4f;
                    break;
                case SupportedSize.IsoB4:
                    width = 250 / 25.4f;
                    height = 353 / 25.4f;
                    break;
                case SupportedSize.IsoB5:
                    if (direction.Equals("vertical"))
                    {
                        width = 176 / 25.4f;
                        height = 250 / 25.4f;
                    }
                    else
                    {
                        width = 250 / 25.4f;
                        height = 176 / 25.4f;
                    }

                    break;
            }

            // set DS unit to inch
            _dataSource.Capabilities.ICapUnits.SetValue(Unit.Inches);
            _dataSource.DGImage.ImageLayout.Get(out var imageLayout);
            // create new TWFrame
            imageLayout.Frame = new TWFrame
            {
                Left = 0,
                Right = width,
                Top = 0,
                Bottom = height
            };
            _dataSource.DGImage.ImageLayout.Set(imageLayout);
        }

        private void SaveToFile()
        {
            // Dir 생성
            var dir = new DirectoryInfo(_curTask.savePath + _curTask.id);
            if (dir.Exists == false) dir.Create();
            // Dir에 저장
            if (_curTask.fileExt == ".pdf")
            {
                var doc = new PdfDocument();
                // pdfSize setting 
                var pdfSize = PageSize.A4;
                switch (_curTask.scanMode.paperSizeMode)
                {
                    case "USLetter":
                        pdfSize = PageSize.Letter;
                        break;
                    case "USLegal":
                        pdfSize = PageSize.Legal;
                        break;
                    case "A3":
                        pdfSize = PageSize.RA3;
                        break;
                    case "A4":
                        pdfSize = PageSize.A4;
                        break;
                    case "A5":
                        pdfSize = PageSize.A5;
                        break;
                    case "IsoB4":
                        pdfSize = PageSize.B4;
                        break;
                    case "IsoB5":
                        pdfSize = PageSize.B4;
                        break;
                }

                // pdfDirection setting
                var pdfDirection = PageOrientation.Portrait;
                if (_curTask.scanMode.paperDirection == "horizontal") pdfDirection = PageOrientation.Landscape;
                for (var i = 0; i < _streamList.Count; i++)
                {
                    // http://pdfsharp.net/wiki/PageSizes-sample.ashx
                    var page = doc.AddPage();
                    page.Size = pdfSize;
                    page.Orientation = pdfDirection;
                    var xgr = XGraphics.FromPdfPage(page);
                    var img = XImage.FromStream(_streamList[i]);
                    xgr.DrawImage(img, 0, 0);
                }

                doc.Save(_curTask.savePath + _curTask.id + "/000000" + _curTask.fileExt);
                doc.Close();
            }
            else
            {
                for (var i = 0; i < _streamList.Count; i++)
                {
                    var fileName = _curTask.savePath + _curTask.id + "/" + i + _curTask.fileExt;
                    Image.Load(_streamList[i]).Save(fileName);
                }
            }

            _streamList.Clear();
            _curTask = null;
        }
    }
}