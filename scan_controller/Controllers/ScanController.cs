using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using scan_controller.Models;
using scan_controller.Service;

namespace scan_controller.Controllers
{
    // 해당 컨트롤러가 호출될때 객체가 생성된다.
    [RoutePrefix("api/scan")]
    public class ScanController : ApiController
    {
        private static ScanService _scanService;

        public ScanController()
        {
            if (_scanService == null) _scanService = new ScanService();
        }

        [Route("test")]
        [HttpGet]
        public ScannerSpec Test()
        {
            return null;
        }

        [Route("session")]
        [HttpDelete]
        public void DeleteSession()
        {
            _scanService.DeleteSession();
        }

        [Route("datasource")]
        [HttpGet]
        public List<string> GetDatasource()
        {
            try
            {
                var sourceList = _scanService.GetDataSourceList();
                var sourceNameList = new List<string>();
                foreach (var ds in sourceList) sourceNameList.Add(ds.Name);
                return sourceNameList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        [Route("datasource/{id:int}")]
        [HttpGet]
        public HttpResponseMessage SetDatasource(int id)
        {
            try
            {
                Console.WriteLine(id);
                _scanService.SetDataSource(id);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        [Route("task/{fileName}")]
        [HttpGet]
        public string Scan(string fileName)
        {
            try
            {
                return _scanService.Scan(fileName, ".pdf");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return "FAIL";
            }
        }

        [Route("task")]
        [HttpPost]
        public string Task(ScanTask scanTask)
            // TODO flat 방식 연속 출력(하나의 pdf로 저장)을 위해 end point 분리 (once, continue, finish)
        {
            try
            {
                _scanService.SetCapability(scanTask.ScanModeDto);
                return _scanService.Scan(scanTask.fileName, scanTask.fileExt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return "FAIL";
            }
        }

        [Route("savePath")]
        [HttpGet]
        public string GetSavePath()
        {
            return _scanService.GetSavePath();
        }

        [Route("savePath")]
        [HttpPost]
        public string SetSavePath(SavePathDTO savePathDto)
        {
            _scanService.SetSavePath(savePathDto.savePath);
            return savePathDto.savePath;
        }

        [Route("spec/{id}")]
        [HttpGet]
        public ScannerSpec GetScannerSpec(int id)
        {
            var spec = _scanService.GetScannerSpec(id);
            return spec;
        }
    }
}