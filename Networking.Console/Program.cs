#pragma warning disable IDE0090 // 'new' expression can be simplified
#pragma warning disable CA1050 // Declare types in namespaces
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;


using static System.Console;

using Networking.Common;
using System.Diagnostics.CodeAnalysis;

// have folder opened, then add project in cntrl+shift+p omnisharp command, then select this file

public class Program {
    public static async Task Main(string[] args)
    {
        // foreach (string arg in args) {
        //     WriteLine(arg);
        // }

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        File.Delete("Logs/console-log.txt");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
            path: "Logs/console-log.txt", 
            rollingInterval: RollingInterval.Infinite,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger: Log.Logger);   

        try {
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<WebApiClientService>();
            IHost host = builder.Build();
            WebApiClientService client = host.Services.GetRequiredService<WebApiClientService>();

            string aliceSecretMessageToBob = "I love you!";


            Alice alice = new Alice();
            alice.InitializeAsymmetricKeys();
            alice.InitializeSymmetricKeys();

            JObject alicePublicKeyJson = new JObject 
                                            { 
                                                {"n", alice.rsaPublicKeyInternal.n.ToString()}, 
                                                {"e", alice.rsaPublicKeyInternal.e.ToString()} 
                                            };

            await client.Post(
                            endpoint: "http://localhost:5210/api/v1/Alice/PublicKey/create", 
                            bodyContent: alicePublicKeyJson
                            );

            // string alicePublicKeyText = await client.Get(
            //                 endpoint: "http://localhost:5210/api/v1/Alice/PublicKey/read"
            // );

            // WriteLine(alicePublicKeyText);

            string bobPublicKeyText = await client.Get(
                endpoint: "http://localhost:5210/api/v1/Bob/PublicKey/read"
            );

            JObject bobPublicKeyJson = JObject.Parse(bobPublicKeyText);
            (BigInteger n, BigInteger e) bobPublicKey = (
                                                            BigInteger.Parse((string)bobPublicKeyJson["n"]!), 
                                                            BigInteger.Parse((string)bobPublicKeyJson["e"]!)
                                                        );

            WriteLine($"Alice: {aliceSecretMessageToBob}");

            JObject encryptedMessageForBob = alice.GetMessageEncryptedPipeline(
                                    plaintextMessage: aliceSecretMessageToBob, 
                                    rsaPublicKeyExternal: bobPublicKey);

            string encryptedReplyFromBobText = await client.Post(
                            endpoint: "http://localhost:5210/api/v1/Bob/Conversation/send", 
                            bodyContent: encryptedMessageForBob
                            );

            JObject encryptedReplyFromBob = JObject.Parse(encryptedReplyFromBobText!);

            string hopefullyDecryptedMessageFromBob = alice.GetMessageDecryptedPipeline(
                encodedResponse: encryptedReplyFromBob,
                rsaPublicKeyExternal: bobPublicKey
            );

            WriteLine($"Bob: {hopefullyDecryptedMessageFromBob}");
        }
        catch (Exception ex) {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally {
            Log.CloseAndFlush();
        }      
    }
}



// private static field shared across instances?


