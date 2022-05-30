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
        private static readonly ScanService ScanService = new ScanService();


        [Route("session")]
        [HttpDelete]
        public Response DeleteSession()
        {
            try
            {
                ScanService.DeleteSession();
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
                var sourceList = ScanService.GetDataSourceList();
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
                return new Response(200, ScanService.SetDataSource(id), "Success Set Datasource");
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
                ScanService.StartTask(scanTask);

                return new Response(0, scanTask.id, "Success Task");
            }
            catch (AlreadyUsingException e)
            {
                return new Response(409, e,
                    "Failed Task[Scanner is Already Using] [new]" + e.newTaskId + "!=[old]" + e.oldTaskId);
            }
            catch (Exception e)
            {
                return new Response(1, e, "Failed Task");
            }
        }

        [Route("task")]
        [HttpDelete]
        public Response DeleteTask(string taskId)
        {
            try
            {
                ScanService.EndScan(taskId);
                return new Response(200, taskId, "Success Delete Task");
            }
            catch (Exception e)
            {
                return new Response(500, e, "Failed Delete Task");
            }
        }

        [Route("spec")]
        [HttpGet]
        public Response GetScannerSpec(int id)
        {
            try
            {
                return new Response(200, ScanService.GetScannerCapability(id), "Success Get Scanner Spec");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(500, e, "Failed Get Scanner Spec");
            }
        }

        [Route("state")]
        [HttpGet]
        public Response GetState()
        {
            try
            {
                return new Response(200, ScanService.GetState(), "Success Get State");
            }
            catch (Exception e)
            {
                return new Response(500, e, "failed Get State");
            }
        }
    }
}