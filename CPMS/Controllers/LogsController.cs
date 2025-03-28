using CPMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace CPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LogsController : Controller
{
    private readonly KafkaLogReaderService _kafkaLogReaderService;

    public LogsController(KafkaLogReaderService kafkaLogReaderService)
    {
        _kafkaLogReaderService = kafkaLogReaderService;
    }
    
    // GET
    [HttpGet("all")]
    public async Task<IActionResult> GetLogsAll()
    {
        var list = await _kafkaLogReaderService.ReadAllLogsAsync();

        return Ok(new {success = true, logs = list});
    }
    
    [HttpGet("recent/{lastOffset?}")]
    public async Task<IActionResult> GetLogs(long lastOffset = 0)
    {
        try
        {
            var response = await _kafkaLogReaderService.GetRecentLogsAsync(
                10,
                lastOffset
            );

            return Ok(new { data = response, success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"{ex}" });
        }
    }

}