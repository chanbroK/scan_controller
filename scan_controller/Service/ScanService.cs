﻿using System;
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
        private static string _savePath = "D://";
        private static string _fileName = "default";
        private static string _fileExt = ".pdf";

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
            // Session 상태 별 handler 설정 (추후 close 되어도 유지된다.)
            _session.TransferReady += (s, e) => { Console.WriteLine("DataSource[Scan Ready]"); };
            _session.DataTransferred += (s, e) =>
            {
                Console.WriteLine("DataSource[Scan Start]");
                var stream = e.GetNativeImageStream();
                if (_fileExt == ".pdf")
                    SaveToPdf(stream);
                else
                    SaveToImage(stream);
                Console.WriteLine(e.NativeData != IntPtr.Zero
                    ? "DataSource[Scan SUCCESS!]"
                    : "DataSource[Scan FAILED!]");
            };
            _session.SourceDisabled += (s, e) =>
            {
                Console.WriteLine("DataSource[Scan End]");
                _session.CurrentSource.Close();
                // _session.Close();
            };


            // default datasource 설정 TODO remove
            OpenSession();
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

        private void SaveToImage(Stream stream)
        {
            var img = Image.Load(stream);
            img.Save(_savePath + _fileName + _fileExt);
        }

        private void SaveToPdf(Stream stream)
        {
            //https://www.c-sharpcorner.com/blogs/pdf-sharp-use-to-image-to-pdf-covert2
            var doc = new PdfDocument();
            doc.Pages.Add(new PdfPage());
            var xgr = XGraphics.FromPdfPage(doc.Pages[0]);
            var img = XImage.FromStream(stream);
            xgr.DrawImage(img, 0, 0);
            doc.Save(_savePath + _fileName + _fileExt);
            doc.Close();
        }
    }
}