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
        [HttpGet]
        public string Scan()
        {
            try
            {
                return _scanService.Scan();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        [Route("savePath")]
        [HttpGet]
        public string GetSavePath()
        {
            return _scanService.getSavePath();
        }

        [Route("savePath")]
        [HttpPost]
        public string SetSavePath(Path path)
        {
            _scanService.setSavePath(path.path);
            return path.path;
        }
    }
}