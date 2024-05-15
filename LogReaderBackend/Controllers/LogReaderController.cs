using Microsoft.AspNetCore.Mvc;
using LogReaderBackend.Services; // Stellen Sie sicher, dass der Namespace korrekt ist
using Newtonsoft.Json;
using LiteDB;
using Microsoft.AspNetCore.Cors;
using System.IO.Compression;

namespace LogReaderBackend.Controllers
{
    [EnableCors()]
    [Route("api/[controller]")]
    [ApiController]
    public class LogReaderController : ControllerBase
    {
        private readonly LogProcessingService _logProcessingService;

        public LogReaderController(LogProcessingService logProcessingService)
        {
            _logProcessingService = logProcessingService;
        }

        private readonly string _dbPath = Path.Combine(Environment.CurrentDirectory, "AppData.db");

        public class AppState
        {
            public int Id { get; set; } 
            public string SessionId { get; set; }
            public string StateJson { get; set; }

            public AppState()
            {
                SessionId = Guid.NewGuid().ToString(); 
            }
        }
        public class AppStateSting
        {
            public string StateJson { get; set; }
        }


        [HttpPost("saveState")]
        public IActionResult SaveState([FromBody] AppStateSting stateJson) 
        {
            var appState = new AppState { StateJson = stateJson.StateJson }; 

            using (var db = new LiteDatabase(_dbPath))
            {
                var states = db.GetCollection<AppState>("states");
                states.Insert(appState);
                return Ok(new { sessionId = appState.SessionId });
            }
        }


        [HttpGet("getState/{sessionId}")]
        public IActionResult GetState(string sessionId)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var states = db.GetCollection<AppState>("states");
                var state = states.FindOne(s => s.SessionId == sessionId);
                if (state == null)
                {
                    return NotFound();
                }
                return Ok(state.StateJson);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isAccessLog, [FromForm] string startTime, [FromForm] string endTime)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or null.");
            }

            try
            {
                using (var compressedStream = file.OpenReadStream())
                using (var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    object result;
                    if (isAccessLog)
                    {
                        // Stellen Sie sicher, dass ReadAccessLogAsync und ReadErrorLogAsync Streams direkt verarbeiten können
                        result = await _logProcessingService.ReadAccessLogAsync(decompressedStream, true, startTime, endTime);
                    }
                    else
                    {
                        result = await _logProcessingService.ReadErrorLogAsync(decompressedStream, true, startTime, endTime);
                    }
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