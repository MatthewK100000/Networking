#pragma warning disable IDE0052 // _logger is never used, but we need it for dependency injection
#pragma warning disable IDE0290 // avoiding new c#12 feature using primary constructor syntax

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;

using static System.Console;
using System.Threading.Tasks;
using System.Text.Json;


using Networking.Common;


namespace Networking.WebApi.Controllers;


[ApiController]
[Route("api/v1/Alice")]
public class AliceController : ControllerBase
{
    private readonly ILogger<BobController> _logger;
    private readonly IMemoryCache _memoryCache;

    public AliceController(ILogger<BobController> logger, IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;
    }

    [HttpPost("PublicKey/create")]
    public IActionResult PostPublicKey([FromBody] object publicKeyBody)
    {   
        ArgumentNullException.ThrowIfNull(publicKeyBody, nameof(publicKeyBody));

        if (!_memoryCache.TryGetValue("publicKey", out string? _))
        {
            
            string publicKeyCached = publicKeyBody.ToString()!;

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _memoryCache.Set("publicKey", publicKeyCached, cacheEntryOptions);
        }

        return new OkResult();
    }

    [HttpGet("PublicKey/read")]
    public IActionResult GetPublicKey() {
        if (!_memoryCache.TryGetValue("publicKey", out string? publicKey))
        {
            return new NotFoundResult();
        }
        return new OkObjectResult(publicKey);
    }

    
}