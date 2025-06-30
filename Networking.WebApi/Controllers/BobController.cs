#pragma warning disable IDE0052 // _logger is never used, but we need it for dependency injection
#pragma warning disable IDE0290 // avoiding new c#12 feature using primary constructor syntax
#pragma warning disable IDE0090 // 'new' expression can be simplified
#pragma warning disable IDE0059 // Unnecessary assignment of a value, compiler mistaken.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;

using static System.Console;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Numerics;

using Networking.Common;
using System.Net;


namespace Networking.WebApi.Controllers;


[ApiController]
[Route("api/v1/Bob")]
public class BobController : ControllerBase
{
    private readonly ILogger<BobController> _logger;
    private readonly IMemoryCache _memoryCache;

    private readonly WebApiClientService _webApiClient;

    public BobController(ILogger<BobController> logger, IMemoryCache memoryCache, WebApiClientService webApiClient)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _webApiClient = webApiClient;
    }

    [HttpPost("Conversation/send")]
    public async Task<ActionResult<object>> PostSample([FromBody] object body)
    {
        // WriteLine(body.GetType());
        
        JObject encryptedMessageFromSomeone = JObject.Parse(body.ToString()!);

        if (!_memoryCache.TryGetValue("cryptographicInstance", out Bob? bob)) {
            return NotFound("In-memory cache of Bob's cryptographic instance expired.");
        }

        if (bob is null) {
            return NotFound("Bob's cryptographic instance is null.");
        }

        string? alicePublicKeyText = null;

        try{
            alicePublicKeyText = await _webApiClient.Get(endpoint: "http://localhost:5210/api/v1/Alice/PublicKey/read");
        }
        catch (System.Net.Http.HttpRequestException e) {
            if (e.StatusCode == HttpStatusCode.NotFound) {
                throw new HttpRequestException("Unable to fetch Alice's public key.");
            } else {
                throw; // some other http error... 
            }
        }

        ArgumentNullException.ThrowIfNull(alicePublicKeyText);

        JObject alicePublicKeyJson = JObject.Parse(alicePublicKeyText);
        (BigInteger n, BigInteger e) alicePublicKey = (
                                                        BigInteger.Parse((string)alicePublicKeyJson["n"]!), 
                                                        BigInteger.Parse((string)alicePublicKeyJson["e"]!)
                                                    );

        string hopefullyDecryptedMessageFromAlice = bob.GetMessageDecryptedPipeline(
                                                            encodedResponse: encryptedMessageFromSomeone,
                                                            rsaPublicKeyExternal: alicePublicKey
                                                        );

        string bobSecretMessageForAlice = "Sorry, not sure I understand.";
        if (hopefullyDecryptedMessageFromAlice == "I love you!") {
            bobSecretMessageForAlice = "I know.";
        }

        JObject encryptedMessageForAlice = bob.GetMessageEncryptedPipeline(
            plaintextMessage: bobSecretMessageForAlice,
            rsaPublicKeyExternal: alicePublicKey
        );

        return new OkObjectResult(encryptedMessageForAlice.ToString());
    }


    [HttpGet("PublicKey/read")]
    public IActionResult GetPublicKey() {

        JObject publicKeyJson;

        if (!_memoryCache.TryGetValue("cryptographicInstance", out Bob? cryptographicInstance))
        {
            // WriteLine("Creating instance of Bob() and caching it in-memory.");
            Bob bob = new Bob();
            bob.InitializeAsymmetricKeys();

            cryptographicInstance = bob;

            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _memoryCache.Set("cryptographicInstance", cryptographicInstance, cacheEntryOptions);
        }

        if (cryptographicInstance is null || cryptographicInstance.rsaPublicKeyInternal.n is null || cryptographicInstance.rsaPublicKeyInternal.e is null)
        {
            return NotFound("Bob's cryptographic instance or public key is null.");
        }

        publicKeyJson = new JObject { 
            {"n", cryptographicInstance.rsaPublicKeyInternal.n.ToString()}, 
            {"e", cryptographicInstance.rsaPublicKeyInternal.e.ToString()} 
        };

        return new OkObjectResult(publicKeyJson.ToString());
    }

}
