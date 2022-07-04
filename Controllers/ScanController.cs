using Microsoft.AspNetCore.Mvc;
using scan_controller.Exception;
using scan_controller.Models;
using scan_controller.Service;
using scan_controller.Util;

namespace scan_controller.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private static readonly ScanService ScanService = new();

    [Route("session")]
    [HttpDelete]
    // twain driver와 Session 삭제(스캐너와 연결도 끊는다.)
    public void DeleteSession()
    {
        ScanService.DeleteSession();
    }

    [Route("scanner/list")]
    [HttpGet]
    // 연결 가능한 스캐너 목록 반환
    public List<string> GetScannerList()
    {
        var scannerList = ScanService.GetScannerList();
        var scannerNameList = new List<string>();
        foreach (var ds in scannerList) scannerNameList.Add(ds.Name);
        return scannerNameList;
    }

    [Route("scanner/list/refresh")]
    [HttpGet]
    // 사용 가능한 스캐너 목록 갱신하고 반환
    public List<string> RefreshScannerList()
    {
        ScanService.LoadEnableScannerList();
        var scannerList = ScanService.GetScannerList();
        var scannerNameList = new List<string>();
        foreach (var ds in scannerList) scannerNameList.Add(ds.Name);
        return scannerNameList;
    }

    [Route("scanner/{id:int}/spec")]
    [HttpGet]
    // 연결 가능한 데이터 소스의 Spec 반환
    public ScannerSpec GetScannerSpec(int id)
    {
        return ScanService.GetScannerSpec(id);
    }

    [Route("scanner/{id:int}")]
    [HttpPatch]
    // 사용할 데이터 소스 설정
    public string SetScanner(int id)
    {
        return ScanService.SetScanner(id);
    }

    [Route("task")]
    [HttpPost]
    // 스캔 명령 Task 
    public string Task([FromBody] ScanTask scanTask)
    {
        // 연속 스캔 작업이 아닐 경우 id가 없으므로 새로운 GUID 생성
        if (scanTask.id == null)
            scanTask.id = HashUtil.GetGuid();

        var isScannerErrorOccured = ScanService.StartScan(scanTask);

        if (isScannerErrorOccured)
            throw new ScannerErrorException();
        return scanTask.id;
    }

    [Route("task/{taskId}")]
    [HttpDelete]
    // 작업 중인 Task 삭제, 작업 중이던 Task가 연속 스캔이라면 연속 스캔을 종료하고 저장 
    public void DeleteTask(string taskId)
    {
        ScanService.EndScan(taskId);
    }


    [Route("state")]
    [HttpGet]
    // 해당 서버의 상태 값 반환
    public int GetState()
    {
        return ScanService.GetState();
    }
}