using System.IO;
using System.Reflection;
using System.Text;
using NTwain;
using NTwain.Data;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using scan_controller.Exception;
using scan_controller.Models;
using scan_controller.Util;
using SixLabors.ImageSharp;

namespace scan_controller.Service;

public class ScanService
{
    // Server State
    private const int SESSION_OPENED = 0;
    private const int SCANNER_OPENED = 1;
    private const int SCANNER_SCANNING = 2;
    private const int WAIT_CONTINUE_SCAN = 3;
    private const int SAVING_FILE = 4;

    // twain driver와 연결 세션
    private static TwainSession? _session;

    // 현재 연결된 DS
    private static DataSource? _curScanner;

    // 연결 가능한 DS 목록
    private readonly List<DataSource> _enableScannerList = new();

    // DS를 통해 받은 데이터 스트림 리스트
    private readonly List<Stream> _streamList = new();

    // 현재 작업중인 Task
    private ScanTask? _curTask;

    // DS의 스캔 작업이 완료 되었는지 여부
    private bool _isScanEnd;

    // DS의 스캔 작업 중 에러가 발생하였는지 여부
    private bool _isScannerErrorOccured;

    // 현재 서버의 상태 값
    private int _state;

    public ScanService()
    {
        // .NET Core에서 Encoding 방식이 .NET Framework랑 다름 -> PDF 저장을 위해 Assembly dll 추가해서 인코딩 방식 추가
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.WriteLine(PlatformInfo.Current.IsApp64Bit
            ? "Server Running on 64bit"
            : "Server Running on 32Bit");

        // NTwain이 twain_32.dll 을 Load하도록 지정
        // PlatformInfo.Current.PreferNewDSM = false;

        // NTwain이 twaindsm.dll 을 Load하도록 지정
        PlatformInfo.Current.PreferNewDSM = true;

        Console.WriteLine("Loaded DSM =" + PlatformInfo.Current.ExpectedDsmPath);

        // twain driver와 session 생성
        _session = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
            Assembly.GetExecutingAssembly()));

        // Session 상태 별 handler 설정
        // 추후 세션이 close 되어도 유지된다. -> 다시 open 해서 사용하면 됨.
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
            Console.WriteLine("스캔 결과 전송 오류 발생");
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
            Console.WriteLine("기기에 이벤트 발생");
            _isScannerErrorOccured = true;
            _isScanEnd = true;
            // DS의 자체 이벤트 발생
        };
        _session.PropertyChanged += (s, e) =>
        {
            // DS의 소유권 이전
        };
        _session.StateChanged += (s, e) =>
        {
            // state가 변경될 때 
            // _session.state로 접근 가능
        };

        OpenSession();
        LoadEnableScannerList();
        SetScanner(0);
    }

    // 생성된 twain driver와의 연결 Open
    private void OpenSession()
    {
        _session.Open();
        _state = SESSION_OPENED;
        Console.WriteLine("Session[Open]");
    }

    // 생성된 twain driver와의 연결 Close 및 연결된 DS와의 연결도 Close
    public void DeleteSession()
    {
        if (_curScanner.IsOpen) _curScanner.Close();
        if (_session.IsDsmOpen) _session.Close();
        Console.WriteLine("Session[Released]");
    }

    // 연결 가능한 DS 목록 갱신
    public void LoadEnableScannerList()
    {
        _enableScannerList.Clear();

        if (!_session.IsDsmOpen) OpenSession();

        foreach (var ds in _session.GetSources().ToList())
            try
            {
                // DS와의 연결을 시도해봄으로써 정상 작동하는 DS인지 확인
                ds.Open();
                if (_session.State == 4) _enableScannerList.Add(ds);
                ds.Close();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Not Supported " + ds.Name);
            }
    }

    // 연결 가능한 DS 목록 반환
    public List<DataSource> GetScannerList()
    {
        return _enableScannerList;
    }

    // 사용할 DS 설정
    // id : DS 목록의 인덱스 번호
    // return : 설정한 DS 이름 반환
    public string SetScanner(int id)
    {
        // 이전에 연결되어 있던 DS 닫기
        if (_curScanner != null && _curScanner.IsOpen) _curScanner.Close();
        // 
        try
        {
            _curScanner = _enableScannerList[id];
            _curScanner.Open();
            _state = SCANNER_OPENED;
            return _curScanner.Name;
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new ScannerIndexOutOfRangeException(id, _enableScannerList.Count);
        }
    }

    // Scan Task 시작
    // newTask -> ScanTask이 담긴 객체 
    // return -> 스캔 과정에서 오류가 발생했는지 여부 반환
    public bool StartScan(ScanTask newTask)
    {
        _isScannerErrorOccured = false;
        // 이미 작업 중인 Task가 존재하는데 입력된 스캔 명령의 id가 일치하지 않으면 에러 반환(연속 스캔 작업일 경우 일치하는 id를 ScanTask객체에 넣어야 한다.)
        if (_curTask != null && _curTask.id != newTask.id) throw new NotMatchedTaskIdException(newTask.id, _curTask.id);

        _curTask = newTask;

        Scan();

        return _isScannerErrorOccured;
    }

    // Scan Task 종료
    // taskId -> 종료할 ScanTask의 id
    public void EndScan(string taskId)
    {
        // 현재 작업 중인 Task 가 없으면 에러 반환
        if (_curTask == null)
            throw new NoTaskException();
        // 현재 작업 중인 Task와 id가 일치하지 않으면 에러 반환
        if (_curTask.id != taskId)
            throw new NotMatchedTaskIdException(taskId, _curTask.id);
        // 현재 데이터 스트림 저장 (연속 스캔 작업일 경우)
        if (_curTask.isContinue)
            SaveToFile();
        else 
            _curTask = null;
    }

    private void Scan()
    {
        _state = SCANNER_SCANNING;

        if (!_session.IsDsmOpen) OpenSession();
        if (!_curScanner.IsOpen) _curScanner.Open();

        // ADF에 용지가 있는지 확인
        if (_curTask.scanMode.feederMode != "flatbed")
            if (_curScanner.Capabilities.CapFeederLoaded.GetCurrent() == BoolType.False)
            {
                _curTask = null;
                throw new NoPaperAdfException();
            }
                

        // ScanMode 설정
        SetScannerSpec();


        _isScanEnd = false;

        // ThreadPool 이용하여 스캔 명령
        // ThreadPool.QueueUserWorkItem(
        // o => { _dataSource.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero); });

        // 스캔 명령
        _curScanner.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);


        while (!_isScanEnd)
        {
            // 스캔 완료 대기
        }

        if (!_curTask.isContinue)
            SaveToFile();
        else
            _state = WAIT_CONTINUE_SCAN;
    }

    // 지정한 DS의 Spec 반환
    // id : 연결 가능한 DS 목록의 인덱스
    // return : 지정한 스캐너의 ScannerSpec
    public ScannerSpec GetScannerSpec(int id)
    {
        if (!_session.IsDsmOpen) OpenSession();
        if (!_curScanner.IsOpen) _curScanner.Open();


        var spec = new ScannerSpec();
        DataSource targetDataSource;
        try
        {
            targetDataSource = _enableScannerList[id];
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new ScannerIndexOutOfRangeException(id, _enableScannerList.Count);
        }

        var caps = targetDataSource.Capabilities;

        // 스캐너 이름

        spec.name = _enableScannerList[id].Name;

        // 색상 방식
        foreach (var v in caps.ICapPixelType.GetValues()) spec.colorMode.Add(v.ToString());


        // DPI 설정
        foreach (var v in caps.ICapXResolution.GetValues())
            spec.dpiMode.Add(v.ToString());


        Console.WriteLine(caps.CapAutomaticSenseMedium.IsSupported);
        Console.WriteLine(caps.CapAutomaticSenseMedium.GetCurrent());
        // 급지 방식
        // twain 2.1 이상 지원이면 flatbed & ADF 둘 다 지원할 경우 True 값을 가짐
        // caps.CapAutomaticSenseMedium.IsSupported

        if (caps.CapFeederEnabled.IsSupported)
        {
            spec.feederMode.Add("flatbed");
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

    // 서버의 상태 반환
    // return : 서버의 상태
    public int GetState()
    {
        return _state;
    }

    // curTask의 ScanMode를 DS의 작업 Spec으로 설정
    private void SetScannerSpec()
    {
        var caps = _curScanner.Capabilities;

        // Legacy UI 삭제 true가 아니면 모두 false로 처리됨 ex) asdfasdf -> false
        caps.CapIndicators.SetValue(EnumUtil<BoolType>.Parse(_curTask.scanMode.showLegacyUI.ToString()));

        // 색상 방식
        try
        {
            caps.ICapPixelType.SetValue(EnumUtil<PixelType>.Parse(_curTask.scanMode.colorMode));
        }
        catch (System.Exception e)
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
        catch (System.Exception e)
        {
            throw new ScanModeValueException("dpiMode", _curTask.scanMode.dpiMode);
        }

        // 급지 방식
        if (_curTask.scanMode.feederMode == "flatbed")
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
            catch (System.Exception e)
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

            // DS의 단위를 인치로 설정
            _curScanner.Capabilities.ICapUnits.SetValue(Unit.Inches);
            _curScanner.DGImage.ImageLayout.Get(out var imageLayout);

            // TWFrame 생성 및 설정
            imageLayout.Frame = new TWFrame
            {
                Left = 0,
                Right = width,
                Top = 0,
                Bottom = height
            };
            _curScanner.DGImage.ImageLayout.Set(imageLayout);
        }
        catch (System.Exception e)
        {
            if (_curTask.scanMode.paperDirection != "vertical" && _curTask.scanMode.paperDirection != "horizontal")
                throw new ScanModeValueException("paperDirection", _curTask.scanMode.paperDirection);
            throw new ScanModeValueException("paperSizeMode", _curTask.scanMode.paperSizeMode);
        }
    }

    // 스캔 결과 파일 저장 
    private void SaveToFile()
    {
        Console.WriteLine("스캔 결과 저장 시작");
        try
        {
            _state = SAVING_FILE;
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

                doc.Save(_curTask.savePath + _curTask.id + "/" + "0".PadLeft(6, '0') + _curTask.fileExt);
                doc.Close();
            }
            else
            {
                for (var i = 0; i < _streamList.Count; i++)
                {
                    var fileName = _curTask.savePath + _curTask.id + "/" + i.ToString().PadLeft(6, '0') +
                                   _curTask.fileExt;
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
        finally
        {
            _streamList.Clear();
            _curTask = null;
            Console.WriteLine("스캔 결과 저장 완료");
            _state = SCANNER_OPENED;
        }
    }
}