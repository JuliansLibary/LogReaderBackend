using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LogReaderBackend.Services; // Stellen Sie sicher, dass der Namespace korrekt ist
using Newtonsoft.Json;
using LogReaderBackend.Models;

namespace LogReaderBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogReaderController : ControllerBase
    {
        private readonly LogProcessingService _logProcessingService;

        public LogReaderController(LogProcessingService logProcessingService)
        {
            _logProcessingService = logProcessingService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isAccessLog)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or null.");
            }

            try
            {
                if (isAccessLog)
                {
  
                     var result = _logProcessingService.ReadAccessLog(file, true);
                    var json = JsonConvert.SerializeObject(result);
                    return Ok(json);
                }
                else
                {
                    
                     var result = _logProcessingService.ReadErrorLog(file, true);
                    var json = JsonConvert.SerializeObject(result);
                    return Ok(json);
                }
                

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

    }
}