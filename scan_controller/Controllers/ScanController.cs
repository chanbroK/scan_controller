﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using scan_controller.Models.DTO;
using scan_controller.Models.Exception;
using scan_controller.Service;
using scan_controller.Util;

namespace scan_controller.Controllers
{
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
                return new Response(404, e, "Failed Delete Session");
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
                var response = new Response(200, sourceNameList, "Success Get DataSource");
                return response;
            }
            catch (Exception e)
            {
                return new Response(404, e, "Failed Get DataSource");
            }
        }

        [Route("datasource")]
        [HttpPatch]
        public Response SetDatasource(int id)
        {
            try
            {
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
                if (scanTask.id == null)
                    scanTask.id = HashUtil.GetGuid();
                _scanService.StartTask(scanTask);

                return new Response(200, scanTask.id, "Success Task");
            }
            catch (AlreadyUsingException e)
            {
                return new Response(409, e,
                    "Failed Task[Scanner is Already Using] [new]" + e.newTaskId + "!=[old]" + e.oldTaskId);
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Task");
            }
        }

        //
        // [Route("task/continue")]
        // [HttpPost]
        // public Response StartContinueTask(ScanTask scanTask)
        // {
        //     try
        //     {
        //         var taskId = HashUtil.GetMD5Id();
        //         _scanService.SetCapability(scanTask.scanMode);
        //         _scanService.StartContinueTask(taskId, scanTask.fileExt);
        //         return new Response(200, taskId, "Success Start Continue Task");
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e.Message);
        //         Console.WriteLine(e.StackTrace);
        //         return new Response(500, e, "Failed Start Continue Task");
        //     }
        // }

        // [Route("task/continue")]
        // [HttpPost]
        // public Response ContinueTask([FromUri] string taskId, [FromBody] ScanTask scanTask)
        // {
        //     try
        //     {
        //         _scanService.SetCapability(scanTask.scanMode);
        //         _scanService.ContinueTask(taskId);
        //         // capability 적용을 안하면 초기화되서 default 값으로 실행됨
        //         return new Response(200, taskId, "Success ContinueTask");
        //     }
        //     catch (Exception e)
        //     {
        //         return new Response(500, e, "Failed ContinueTask");
        //     }
        // }

        [Route("task")]
        [HttpDelete]
        public Response DeleteContinueTask(string taskId)
        {
            try
            {
                _scanService.EndContinueScan(taskId);
                return new Response(200, taskId, "Success Delete Continue Task");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Delete Continue Task");
            }
        }

        // [Route("save_path")]
        // [HttpGet]
        // public Response GetSavePath()
        // {
        //     try
        //     {
        //         return new Response(200, _scanService.GetSavePath(), "Success Get Save Path");
        //     }
        //     catch (Exception e)
        //     {
        //         return new Response(500, e, "Failed Get Save Path");
        //     }
        // }
        //
        // [Route("save_path")]
        // [HttpPatch]
        // public Response SetSavePath(string savePath)
        // {
        //     try
        //     {
        //         _scanService.SetSavePath(savePath);
        //         return new Response(200, savePath, "Success Set Save Path");
        //     }
        //     catch (Exception e)
        //     {
        //         return new Response(500, e, "Failed Set Save Path");
        //     }
        // }

        [Route("spec")]
        [HttpGet]
        public Response GetScannerSpec(int id)
        {
            try
            {
                return new Response(200, _scanService.GetScannerCapability(id), "Success Get Scanner Spec");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Get Scanner Spec");
            }
        }

        [Route("state")]
        [HttpGet]
        // TODO Twain State 별 설명 
        public Response GetState()
        {
            try
            {
                return new Response(200, _scanService.getState(), "Success Get State");
            }
            catch (Exception e)
            {
                return new Response(500, e, "failed Get State");
            }
        }
    }
}