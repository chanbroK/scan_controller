﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NTwain;
using NTwain.Data;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using scan_controller.Models;
using scan_controller.Models.Exception;
using scan_controller.Util;
using SixLabors.ImageSharp;

namespace scan_controller.Service
{
    public class ScanService
    {
        private static TwainSession? _session;
        private static DataSource? _curDataSource;
        private readonly List<DataSource> _dataSourceList = new List<DataSource>();
        private readonly List<Stream> _streamList = new List<Stream>();
        private ScanTask? _curTask;
        private bool _isScanEnd;
        private bool _isScannerErrorOccured;
        private int _state;

        public ScanService()
        {
            // Core에서 Encoding 방식이 Framework랑 다름 -> Assembly dll 추가해서 인코딩 방식 추가
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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
                _streamList.Add(e.GetNativeImageStream());
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "스캔 성공"
                    : "스캔 실패");
            };
            _session.TransferError += (s, e) =>
            {
                // 스캔 결과 전송 에러 발생
                Console.WriteLine("TransferError!!");
                _isScannerErrorOccured = true;
                _isScanEnd = true;
                Console.WriteLine(e.Exception.Message);
            };
            _session.SourceDisabled += (s, e) =>
            {
                // sate5 -> state4
                Console.WriteLine("스캔 완료");
                _isScanEnd = true;
            };
            _session.SourceChanged += (s, e) =>
            {
                // 사용하는 DS가 변경 
            };
            _session.DeviceEvent += (s, e) =>
            {
                Console.WriteLine("DeviceEvent!!");
                _isScannerErrorOccured = true;
                _isScanEnd = true;
                // DS의 자체 이벤트 발생
            };
            _session.PropertyChanged += (s, e) =>
            {
                // ? ? ? DS의 소유권 이전 ? 
            };
            _session.StateChanged += (s, e) =>
            {
                Console.WriteLine(_session.State);
                // state가 변경될 때 _session.state로 접근
            };

            OpenSession();
            LoadDataSource();
            // TODO remove set Default DataSource
            SetDataSource(0);
        }

        private void OpenSession()
        {
            // Twain Session Open 
            _session.Open();
            _state = 0;
            Console.WriteLine("Session[Open]");
        }

        public void DeleteSession()
        {
            if (_curDataSource.IsOpen) _curDataSource.Close();
            if (_session.IsDsmOpen) _session.Close();
            Console.WriteLine("Session[Released]");
        }

        public void LoadDataSource()
        {
            _dataSourceList.Clear();
            Console.WriteLine(_session.GetSources().ToList().Count);
         
            foreach (var ds in _session.GetSources().ToList())
                try
                {
                    ds.Open();
                    if (_session.State == 4) _dataSourceList.Add(ds);
                    Console.WriteLine(ds.Name);
                    ds.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Not Supported " + ds.Name);
                }
        }

        public List<DataSource> GetDataSourceList()
        {
            return _dataSourceList;
        }

        public string SetDataSource(int id)
        {
            if (_curDataSource != null && _curDataSource.IsOpen) _curDataSource.Close();
            _curDataSource = _dataSourceList[id];
            _curDataSource.Open();
            _state = 1;
            return _curDataSource.Name;
        }


        public bool StartScan(ScanTask newTask)
        {
            _isScannerErrorOccured = false;

            if (_curTask != null && _curTask.id != newTask.id) throw new AlreadyUsingException(newTask.id, _curTask.id);
            _curTask = newTask;
            Scan();

            return _isScannerErrorOccured;
        }

        public void EndScan(string taskId)
        {
            if (_curTask == null)
                throw new NoTaskException();
            if (_curTask.id != taskId)
                throw new NotMatchedTaskIdException(taskId, _curTask.id);
            SaveToFile();
        }

        private void Scan()
        {
            _state = 2;
            if (!_session.IsDsmOpen) OpenSession();
            if (!_curDataSource.IsOpen) _curDataSource.Open();

            SetCapability();

            // ThreadPool.QueueUserWorkItem(
            // o => { _dataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero); });

            // ADF check
            if (_curTask.scanMode.feederMode != "flated")
                if (_curDataSource.Capabilities.CapFeederLoaded.GetCurrent() == BoolType.False)
                    throw new NoPaperADFException();


            _isScanEnd = false;
            _curDataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);


            while (!_isScanEnd)
            {
                // 스캔 대기
            }

            if (!_curTask.isContinue)
                SaveToFile();
            else
                _state = 3;
        }

        public ScannerSpec GetScannerCapability(int id)
        {
            // if (!_session.IsDsmOpen) OpenSession();
            // if (!_curDataSource.IsOpen) _curDataSource.Open();


            var spec = new ScannerSpec();

            var targetDataSource = _dataSourceList[id];
            var caps = targetDataSource.Capabilities;

            // 스캐너 이름

            spec.name = _dataSourceList[id].Name;
            
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

            return spec;
        }

        public int GetState()
        {
            return _state;
        }

        private void SetCapability()
        {
            // if (!_session.IsDsmOpen) OpenSession();
            // if (!_curDataSource.IsOpen) _curDataSource.Open();

            var caps = _curDataSource.Capabilities;

            // Legacy UI 삭제 true가 아니면 모두 false로 처리됨 ex) asdfasdf -> false
            caps.CapIndicators.SetValue(EnumUtil<BoolType>.Parse(_curTask.scanMode.showLegacyUI.ToString()));

            // 색상 방식
            try
            {
                caps.ICapPixelType.SetValue(EnumUtil<PixelType>.Parse(_curTask.scanMode.colorMode));
            }
            catch (Exception e)
            {
                throw new ScanModeValueException("colorMode", _curTask.scanMode.colorMode);
            }

            // DPI 설정 (NOT Enum)
            try
            {
                var dpi = int.Parse(_curTask.scanMode.dpiMode);
                caps.ICapXResolution.SetValue(dpi);
                caps.ICapYResolution.SetValue(dpi);
            }
            catch (Exception e)
            {
                throw new ScanModeValueException("dpiMode", _curTask.scanMode.dpiMode);
            }

            // 급지 방식
            if (_curTask.scanMode.feederMode == "flated")
            {
                // 스캔
                caps.CapFeederEnabled.SetValue(BoolType.False);
            }
            else if (_curTask.scanMode.feederMode == "ADF(one-side)")
            {
                // ADF
                caps.CapFeederEnabled.SetValue(BoolType.True);
                // 단면
                caps.CapDuplexEnabled.SetValue(BoolType.False);
            }
            else if (_curTask.scanMode.feederMode == "ADF(two-side)")
            {
                // ADF
                caps.CapFeederEnabled.SetValue(BoolType.True);
                // 양면
                caps.CapDuplexEnabled.SetValue(BoolType.True);
                //용지 뒤집는 방식
                try
                {
                    if (caps.ICapFlipRotation.IsSupported)
                        caps.ICapFlipRotation.SetValue(EnumUtil<FlipRotation>.Parse(_curTask.scanMode.flipMode));
                }
                catch (Exception e)
                {
                    throw new ScanModeValueException("flipMode", _curTask.scanMode.flipMode);
                }
            }
            else
            {
                throw new ScanModeValueException("feederMode", _curTask.scanMode.feederMode);
            }


            // 용지 크기

            try
            {
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
                _curDataSource.Capabilities.ICapUnits.SetValue(Unit.Inches);
                _curDataSource.DGImage.ImageLayout.Get(out var imageLayout);
                // create new TWFrame
                imageLayout.Frame = new TWFrame
                {
                    Left = 0,
                    Right = width,
                    Top = 0,
                    Bottom = height
                };
                _curDataSource.DGImage.ImageLayout.Set(imageLayout);
            }
            catch (Exception e)
            {
                if (_curTask.scanMode.paperDirection == "vertical" || _curTask.scanMode.paperDirection == "horizontal")
                    throw new ScanModeValueException("paperDirection", _curTask.scanMode.paperDirection);
                throw new ScanModeValueException("paperSizeMode", _curTask.scanMode.paperSizeMode);
            }
        }

        private void SaveToFile()
        {
            Console.WriteLine("스캔 결과 저장 시작");
            try
            {
                _state = 4;
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
            }
            catch (NullReferenceException e)
            {
                throw new NoDataToSaveException();
            }
            catch (InvalidOperationException e)
            {
                throw new NoDataToSaveException();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw new ConcurrentFileAccessException(_curTask.fileExt);
            }
            finally

            {
                _streamList.Clear();
                _curTask = null;
                Console.WriteLine("스캔 결과 저장 완료");
                _state = 1;
            }
        }
    }
}