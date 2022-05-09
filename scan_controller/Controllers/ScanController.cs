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
    // end point 는 대소문자 구분 X
    [RoutePrefix("api/scan")]
    public class ScanController : ApiController
    {
        private static ScanService _scanService;

        public ScanController()
        {
            if (_scanService == null) _scanService = new ScanService();
        }


        [Route("session")]
        [HttpDelete]
        public Response DeleteSession()
        {
            try
            {
                _scanService.DeleteSession();
                return new Response(200, null, "Success Delete Session");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Delete Session");
            }
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

        [Route("task")]
        [HttpPost]
        public string Task(ScanTask scanTask)
        {
            try
            {
                _scanService.SetCapability(scanTask.scanMode);
                return _scanService.Scan(scanTask.fileName, scanTask.fileExt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return "FAIL";
            }
        }

        [Route("task/continue")]
        [HttpPost]
        public void ContinueTask(ScanTask scanTask)
        {
            try
            {
                _scanService.SetCapability(scanTask.scanMode);
                _scanService.ScanContinuous(scanTask.fileName, scanTask.fileExt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        [Route("task/continue")]
        [HttpDelete]
        public List<string> ContinueTaskEnd()
        {
            try
            {
                return _scanService.EndContinuousScan();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        [Route("savepath")]
        [HttpGet]
        public string GetSavePath()
        {
            return _scanService.GetSavePath();
        }

        [Route("savepath")]
        [HttpPost]
        public string SetSavePath(SavePath savePath)
        {
            _scanService.SetSavePath(savePath.savePath);
            return savePath.savePath;
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