using System.Collections.Generic;
using NTwain;
using NTwain.Data;

namespace scan_controller.Models
{
    public class ScannerSpec
        // Response로 전송될때 enum은 int 형식으로 변환된다. -> client 를 위해 string 형식으로 저장
    {
        // 색상 방식 [ 흑백, 회색, 컬러]
        public List<string> colorMode = new List<string>();

        // DPI
        public List<string> dpiMode = new List<string>();

        // 급지 방식 [스캔(단면),자동급지(단면), 자동급지(양면)] 
        public List<string> feederMode = new List<string>();

        // 용지 뒤집는 방식 [book, fanfold] 
        public List<string> flipMode = new List<string>();

        // scanner name 
        public string name;


        // 용지 크기 [ A3, A4, ... B3, B4...]
        public List<string> paperSizeMode = new List<string>();

        public ScannerSpec(DataSource ds)
        {
            var caps = ds.Capabilities;

            // 스캐너 이름
            name = ds.Name;
            // 색상 방식
            foreach (var v in caps.ICapPixelType.GetValues()) colorMode.Add(v.ToString());

            // DPI 방식
            foreach (var v in caps.ICapXResolution.GetValues())
                // X,Y 값이 다를 수 있음 주의
                dpiMode.Add(v.ToString());

            // 급지 방식
            // TODO
            feederMode.Add("flated");
            if (caps.CapFeederEnabled.IsSupported)
            {
                feederMode.Add("ADF(one-side)");
                if (caps.CapDuplexEnabled.IsSupported) feederMode.Add("ADF(two-side)");
            }

            // 용지 뒤집는 방식

            if (caps.ICapFlipRotation.IsSupported)
            {
                flipMode.Add(FlipRotation.Book.ToString());
                flipMode.Add(FlipRotation.Fanfold.ToString());
            }

            // 용지 크기
            foreach (var v in caps.ICapSupportedSizes.GetValues())
                if (!v.Equals(SupportedSize.None))
                    paperSizeMode.Add(v.ToString());
        }
    }
}