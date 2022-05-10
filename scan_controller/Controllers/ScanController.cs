using System;
using System.Collections.Generic;
using System.Web.Http;
using scan_controller.Models;
using scan_controller.Service;
using scan_controller.Util;

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
        public Response GetDatasource()
        {
            try
            {
                var sourceList = _scanService.GetDataSourceList();
                var sourceNameList = new List<string>();
                foreach (var ds in sourceList) sourceNameList.Add(ds.Name);
                return new Response(200, sourceNameList, "Success Get DataSource");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Get DataSource");
            }
        }

        [Route("datasource/{id:int}")]
        [HttpGet]
        public Response SetDatasource(int id)
        {
            try
            {
                Console.WriteLine(id);
                _scanService.SetDataSource(id);
                return new Response(200, null, "Success Set Datasource");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Set Datasource");
            }
        }

        [Route("task")]
        [HttpPost]
        public Response Task(ScanTask scanTask)
        {
            try
            {
                var taskId = HashUtil.GetMD5Id();
                _scanService.SetCapability(scanTask.scanMode);
                _scanService.OnceTask(taskId, scanTask.fileExt);

                return new Response(200, taskId, "Success Task");
            }
            catch (AlreadyUsingException e)
            {
                return new Response(409, e, "Failed Task[Scanner is Already Using]");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Task");
            }
        }


        [Route("task/continue")]
        [HttpPost]
        public void StartContinueTask(ScanTask scanTask)
        {
            try
            {
                var taskId = HashUtil.GetMD5Id();
                _scanService.SetCapability(scanTask.scanMode);
                _scanService.ContinueTask(taskId, scanTask.fileExt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        [Route("task/continue/{taskId}")]
        [HttpPut]
        public Response ContinueTask(string taskId)
        {
            try
            {
                // TODO
                _scanService.SetCapability(scanTask.scanMode);
                _scanService.ContinueTask(taskId, scanTask.fileExt);

                return new Response(200, taskId, "Success ContinueTask");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed ContinueTask");
            }
        }

        [Route("task/continue/{taskId}")]
        [HttpDelete]
        public List<string> DeleteContinueTask()
        {
            try
            {
                return _scanService.EndContinueScan();
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
        public Response GetSavePath()
        {
            try
            {
                return new Response(200, _scanService.GetSavePath(), "Success Get Save Path");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Get Save Path");
            }
        }

        [Route("savepath")]
        [HttpPost]
        public Response SetSavePath(SavePath savePath)
        {
            try
            {
                _scanService.SetSavePath(savePath.savePath);
                return new Response(200, savePath.savePath, "Success Set Save Path");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Set Save Path");
            }
        }

        [Route("spec/{id}")]
        [HttpGet]
        public Response GetScannerSpec(int id)
        {
            try
            {
                return new Response(200, _scanService.GetScannerSpec(id), "Success Get Scanner Spec");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Get Scanner Spec");
            }
        }
    }
}