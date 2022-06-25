using Microsoft.AspNetCore.Mvc;
using NTwain;
using scan_controller.Models;
using scan_controller.Models.Exception;
using scan_controller.Service;
using scan_controller.Util;


namespace scan_controller.Controllers
{
    [ApiController]
    [Route("api/scan")]
    public class ScanController : ControllerBase
    {
        private static readonly ScanService ScanService = new ScanService();
        
        [Route("session")]
        [HttpDelete]
        // twain driver와 Session 삭제(스캐너와 연결도 끊는다.)
        public Response DeleteSession()
        {
            try
            {
                ScanService.DeleteSession();
                return new Response(0, 1, "Success Delete Session");
            }
            catch (Exception e)
            {
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Delete Session");
            }
        }

        [Route("datasource")]
        [HttpGet]
        // 연결 가능한 스캐너 목록 반환
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
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Get DataSource");
            }
        }

        [Route("datasource/refresh")]
        [HttpGet]
        // 사용 가능한 스캐너 목록 갱신하고 반환
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
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Refresh DataSource");
            }
        }

        [Route("datasource/spec/{id}")]
        [HttpGet]
        // 연결 가능한 데이터 소스의 Spec 반환
        public Response GetDataSourceSpec(int id)
        {
            try
            {
                Console.WriteLine(id);
                ScannerSpec scannerSpec = ScanService.GetScannerCapability(id);
                Console.Write(scannerSpec.colorMode.Count);
                return new Response(0, scannerSpec, "Success Get Datasource Spec");
            }
            catch (ArgumentOutOfRangeException e)
            {
                return new Response(1, null, "Failed Get Datasource Spec \n index is out of range");
            }
            catch (Exception e)
            {
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Get Datasource Spec");
            }
        }

        [Route("datasource/{id}")]
        [HttpPatch]
        // 사용할 데이터 소스 설정
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
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Set Datasource");
            }
        }

        [Route("task")]
        [HttpPost]
        // 스캔 명령 Task 
        public Response Task([FromBody]ScanTask scanTask)
        {
            try
            {
                // 연속 스캔 작업이 아닐 경우 id가 없으므로 새로운 GUID 생성
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
            catch (NoPaperADFException e)
            {
                return new Response(5, null, "Failed task \n No paper in ADF");
            }
            catch (NoDataToSaveException e)
            {
                return new Response(6, null, "Failed task \n No data to save");
            }
            catch (Exception e)
            {
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed task");
            }
        }

        [Route("task/{taskId}")]
        [HttpDelete]
        // 작업 중인 Task 삭제, 작업 중이던 Task가 연속 스캔이라면 연속 스캔을 종료하고 저장 
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
            catch (NoDataToSaveException e)
            {
                return new Response(4, null, "Failed task \n No data to save");
            }
            catch (Exception e)
            {
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Delete Scan Task");
            }
        }


        [Route("state")]
        [HttpGet]
        // 해당 서버의 상태 값 반환
        public Response GetState()
        {
            try
            {
                return new Response(0, ScanService.GetState(), "Success Get State");
            }
            catch (Exception e)
            {
                ExceptionResponse exceptionResponse = new ExceptionResponse(e.Message,e.StackTrace);
                return new Response(-1, exceptionResponse, "Failed Delete Scan Task");
            }
        }
    }
}