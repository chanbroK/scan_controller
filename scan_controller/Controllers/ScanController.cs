using System;
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
                return new Response(0, null, "Success Delete Session");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Delete Session");
            }
        }

        [Route("datasource")]
        [HttpGet]
        public Response GetDataSource()
        {
            try
            {
                var sourceList = ScanService.GetDataSourceList();
                var sourceNameList = new List<string>();
                foreach (var ds in sourceList) sourceNameList.Add(ds.Name);
                var response = new Response(0, sourceNameList, "Success Get DataSource");
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Get DataSource");
            }
        }

        [Route("datasource/refresh")]
        [HttpGet]
        public Response GetRefreshDataSource()
        {
            try
            {
                ScanService.LoadDataSource();
                var sourceList = ScanService.GetDataSourceList();
                var sourceNameList = new List<string>();
                foreach (var ds in sourceList) sourceNameList.Add(ds.Name);
                var response = new Response(0, sourceNameList, "Success Refresh DataSource");
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Refresh DataSource");
            }
        }

        [Route("datasource/spec")]
        [HttpGet]
        public Response GetDataSourceSpec(int id)
        {
            try
            {
                return new Response(0, ScanService.GetScannerCapability(id), "Success Get Datasource Spec");
            }
            catch (ArgumentOutOfRangeException e)
            {
                return new Response(1, null, "Failed Get Datasource Spec \n index is out of range");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Get Datasource Spec");
            }
        }

        [Route("datasource")]
        [HttpPatch]
        public Response SetDatasource(int id)
        {
            try
            {
                return new Response(0, ScanService.SetDataSource(id), "Success Set Datasource");
            }
            catch (ArgumentOutOfRangeException e)
            {
                return new Response(1, null, "Failed Set Datasource \n index is out of range");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Set Datasource");
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
                var isScannerErrorOccured = ScanService.StartScan(scanTask);
                if (isScannerErrorOccured)
                    return new Response(2, scanTask.id, "Failed task \n Datasource Error is occured");
                return new Response(0, scanTask.id, "Success task");
            }
            catch (ScanModeValueException e)
            {
                return new Response(3, null,
                    "Failed task \n" + e.ModeName + " is not apply " + e.Value);
            }
            catch (AlreadyUsingException e)
            {
                return new Response(1, null,
                    "Failed task \n [input task id]" + e.InputTaskId + "!=[current task id]" + e.CurTaskId);
            }
            catch (ConcurrentFileAccessException e)
            {
                return new Response(4, null, "Failed task \n Concurrently access save path" + e.DirName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed task");
            }
        }

        [Route("task")]
        [HttpDelete]
        public Response DeleteTask(string taskId)
        {
            try
            {
                ScanService.EndScan(taskId);
                return new Response(0, taskId, "Success delete Scan Task");
            }
            catch (NoTaskException e)
            {
                return new Response(1, null,
                    "Failed Delete Scan Task \n  no task in controller");
            }
            catch (NotMatchedTaskIdException e)
            {
                return new Response(2, null,
                    "Failed Delete Scan Task \n  [input task id]" + e.InputTaskId + "!=[current task id]" +
                    e.CurTaskId);
            }
            catch (ConcurrentFileAccessException e)
            {
                return new Response(3, null, "Failed task \n Concurrently Access Save Path" + e.DirName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "Failed Delete Scan Task");
            }
        }


        [Route("state")]
        [HttpGet]
        public Response GetState()
        {
            try
            {
                return new Response(0, ScanService.GetState(), "Success Get State");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Response(-1, e, "failed Get State");
            }
        }
    }
}