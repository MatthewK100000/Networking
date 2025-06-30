#nullable enable
#pragma warning disable IDE0090 // 'new' expression can be simplified
#pragma warning disable IDE0063 // 'using' statement can be simplified
#pragma warning disable IDE0017 // Object initiation can be simplified
#pragma warning disable CA2208



using System.Security.Cryptography;
using System.Text;
using static System.Console;
using System.Numerics;
using static System.ArgumentNullException;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json.Serialization;

using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Networking.Common;

/*
Create new exception type for invalid signatures and inherit constructors from Exception class. 
Unlike ordinary methods, constructors are not inherited, so we must explicitly declare and call the base constructors instead.
We can add additional logic undeneath {} for each constructor, such as writing to a log or print statement etc. 
*/
public class InvalidSignatureException: Exception 
{
    public InvalidSignatureException() : base() { }
    public InvalidSignatureException(string message) : base(message) { }
    public InvalidSignatureException(string message, Exception innerException) : base(message, innerException) { }
}



// split definition of class across multiple files using partial
public partial class Person : Object
{
    public string? plaintextMessage;
    public string? hashedMessage;
    public byte[]? digitalSignature;

    private (BigInteger? n, BigInteger? d) rsaPrivateKeyInternal;
    public (BigInteger? n, BigInteger? e) rsaPublicKeyInternal;
    private RSA? rsaAlgorithmInstanceInternal;

    private (byte[]? Key, byte[]? IV) aesSymmetricKeys;
    private Aes? aesAlgorithmInstance;


    public JObject GetMessageEncryptedPipeline(string plaintextMessage, (BigInteger n, BigInteger e) rsaPublicKeyExternal) {
        // pipeline to perform encryption steps and return json object to send off in request body (if sent to Bob from Alice), else response (if sent to Alice from Bob)
        byte[] messageDigest = GetMessageDigestUsingSHA256(plaintextMessage);
        byte[] messageSignature = GenerateMessageSignatureUsingRSA(messageDigest);
        byte[] messageDigestAndSignatureEncrypted = EncryptSignatureAndMessageUsingAes(plaintextMessage, messageSignature);
        byte[] symmetricKeysEncrypted = EncryptSymmetricKeysUsingRSA(rsaPublicKeyExternal);
        JObject returnValue = new JObject { 
                {"encryptedSymmetricKeys", Convert.ToBase64String(symmetricKeysEncrypted)}, 
                {"encryptedMessageWithSignature", Convert.ToBase64String(messageDigestAndSignatureEncrypted)} 
                };

        return returnValue;
    }

    public string GetMessageDecryptedPipeline(JObject encodedResponse, (BigInteger n, BigInteger e) rsaPublicKeyExternal)
    {
        // pipeline to perform decryption, validation steps and return string

        // check if key is there first else throw error
        if (!encodedResponse.ContainsKey("encryptedSymmetricKeys"))
        {
            throw new Exception("Missing key: 'encryptedSymmetricKeys'");
        }

        if (!encodedResponse.ContainsKey("encryptedMessageWithSignature"))
        {
            throw new Exception("Missing key: 'encryptedMessageWithSignature'");
        }

        byte[] decodedEncryptedSymmetricKeys = Convert.FromBase64String((string)encodedResponse["encryptedSymmetricKeys"]!); // null-forgiving operator will let the compiler know to forget it as a warning (that it could return null), and throw an error just in case it is. 
        byte[] decodedEncryptedMessageWithSignature = Convert.FromBase64String((string)encodedResponse["encryptedMessageWithSignature"]!);

        DecryptSymmetricKeysUsingRSA(decodedEncryptedSymmetricKeys);
        JObject decryptedMessageWithSignature = DecryptSignatureAndMessageUsingAes(decodedEncryptedMessageWithSignature);

        string plaintextMessage = VerifySenderAuthAndMessageIntegrity(decryptedMessageWithSignature, rsaPublicKeyExternal);

        return plaintextMessage;
    }


    public void InitializeAsymmetricKeys()
    {
        //Generate a public/private key pair.  
        RSA rsa = RSA.Create();

        //Save the public key information to an RSAParameters structure.  
        RSAParameters rsaKeyInfo = rsa.ExportParameters(true);

        // Extract the two prime numbers p and q from the RSAParameters structure. 
        // byte[] p = rsaKeyInfo.P;
        // byte[] q = rsaKeyInfo.Q;

        rsaPublicKeyInternal = (
            new BigInteger(rsaKeyInfo.Modulus, isUnsigned: true, isBigEndian: true),
            new BigInteger(rsaKeyInfo.Exponent, isUnsigned: true, isBigEndian: true)
        );

        rsaPrivateKeyInternal = (
            new BigInteger(rsaKeyInfo.Modulus, isUnsigned: true, isBigEndian: true),
            new BigInteger(rsaKeyInfo.D, isUnsigned: true, isBigEndian: true)
        );

        rsaAlgorithmInstanceInternal = rsa;
    }

    public void InitializeSymmetricKeys(){
        Aes aes = Aes.Create();

        aesSymmetricKeys = (aes.Key, aes.IV);
        aesAlgorithmInstance = aes;
    }

    private byte[] GetMessageDigestUsingSHA256(string plaintextMessage) {
        //Convert the string into an array of bytes.
        byte[] messageBytes = Encoding.UTF8.GetBytes(plaintextMessage);

        //Create the hash value from the array of bytes.
        byte[] hashValue = SHA256.HashData(messageBytes);

        // record hash for later public inspection
        hashedMessage = Convert.ToHexString(hashValue);

        return hashValue;
    }

    private byte[] GenerateMessageSignatureUsingRSA(byte[] messageDigest) {

        if (rsaAlgorithmInstanceInternal is null) {
            throw new ArgumentNullException(nameof(rsaAlgorithmInstanceInternal), "RSA algorithm instance is not initialized.");
        }

        RSAPKCS1SignatureFormatter rsaFormatter = new(rsaAlgorithmInstanceInternal);
        rsaFormatter.SetHashAlgorithm(nameof(SHA256));

        digitalSignature = rsaFormatter.CreateSignature(messageDigest);

        return digitalSignature;

    }

    private byte[] EncryptSignatureAndMessageUsingAes(string plaintextMessage, byte[] messageSignature)
    {       
        if (aesAlgorithmInstance is null) {
            throw new ArgumentNullException(nameof(aesAlgorithmInstance), "AES algorithm instance is not initialized.");
        }

        // // Create an encryptor to perform the stream transform.
        ICryptoTransform encryptor = aesAlgorithmInstance.CreateEncryptor(
            aesAlgorithmInstance.Key,
            aesAlgorithmInstance.IV
            );

        // write to in memory stream
        MemoryStream? msEncrypt = null;
        byte[]? encrypted = null;

        // using statement are try-finally blocks that automatically call Dispose() on the object when the block is exited, even if an exception occurs.
        // This is useful for cleaning up resources like streams, files, or database connections that need to be closed or disposed of properly.
        try{
            msEncrypt = new MemoryStream();
            
            // encrypt byte array stream
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                // convert string stream to byte array stream
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    // convert class/type to json stream
                    using (JsonWriter jsonEncrypt = new JsonTextWriter(swEncrypt)) // json to string stream, not buffered in memory!
                    {
                        jsonEncrypt.Formatting = Formatting.Indented;
                        jsonEncrypt.WriteStartObject();
                        jsonEncrypt.WritePropertyName("plaintextMessage");
                        jsonEncrypt.WriteValue(plaintextMessage);
                        jsonEncrypt.WritePropertyName("messageSignature");
                        jsonEncrypt.WriteValue(Convert.ToBase64String(messageSignature));
                        jsonEncrypt.WriteEndObject();

                    } // using statements automatically call Dispose() and Close()
                }
            }

            encrypted = msEncrypt.ToArray();
        }
        finally {
            msEncrypt?.Dispose(); // same as: if (msEncrypt != null) { msEncrypt.Dispose(); }
        }

        return encrypted ?? throw new ArgumentNullException(nameof(encrypted));
    }

    private byte[] EncryptSymmetricKeysUsingRSA((BigInteger n, BigInteger e) rsaPublicKeyExternal) {

        if (aesSymmetricKeys.Key is null || aesSymmetricKeys.IV is null) {
            throw new ArgumentNullException(nameof(aesSymmetricKeys), "AES symmetric keys are not initialized.");
        }

        RSA rsa = RSA.Create();

        RSAParameters rsaKeyInfo = new RSAParameters();

        rsaKeyInfo.Modulus = rsaPublicKeyExternal.n.ToByteArray(isUnsigned: true, isBigEndian: true);
        rsaKeyInfo.Exponent = rsaPublicKeyExternal.e.ToByteArray(isUnsigned: true, isBigEndian: true);

        rsa.ImportParameters(rsaKeyInfo);

        byte[] toEncrypt;

        using (MemoryStream ms = new MemoryStream())
        {
            using (StreamWriter sw = new StreamWriter(ms))
            {
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;
                    jw.WriteStartObject();
                    jw.WritePropertyName("aesKey");
                    jw.WriteValue(Convert.ToBase64String(aesSymmetricKeys.Key));
                    jw.WritePropertyName("aesIV");
                    jw.WriteValue(Convert.ToBase64String(aesSymmetricKeys.IV));
                    jw.WriteEndObject();

                } // using statements automatically call Dispose() and Close()
            }
            toEncrypt = ms.ToArray();
        }

        byte[] encrypted = rsa.Encrypt(toEncrypt, RSAEncryptionPadding.Pkcs1);

        return encrypted;
    }

    private void DecryptSymmetricKeysUsingRSA(byte[] encryptedSymmetricKeys) {

        if (rsaAlgorithmInstanceInternal is null) {
            throw new ArgumentNullException(nameof(rsaAlgorithmInstanceInternal), "RSA algorithm instance is not initialized.");
            }

        RSA rsa = rsaAlgorithmInstanceInternal;

        byte[] decrypted = rsa.Decrypt(encryptedSymmetricKeys, RSAEncryptionPadding.Pkcs1);

        JObject json;

        using (MemoryStream ms = new MemoryStream(decrypted)) {
            using (StreamReader sr = new StreamReader(ms)) {
                json = JObject.Parse(sr.ReadToEnd());
            }
        }

        if (!json.ContainsKey("aesKey")) { throw new Exception("Missing key: 'aesKey'"); }
        if (!json.ContainsKey("aesIV")) { throw new Exception("Missing key: 'aesIV'"); }


        byte[] aesKey = Convert.FromBase64String((string)json["aesKey"]!);
        byte[] aesIV = Convert.FromBase64String((string)json["aesIV"]!);

        Aes aesAlg = Aes.Create();
        aesAlg.Key = aesKey;
        aesAlg.IV = aesIV;

        aesAlgorithmInstance = aesAlg;
        aesSymmetricKeys = (aesKey, aesIV);

        // if key and IV is already present, then use it, else instantiate it
    }

    private JObject DecryptSignatureAndMessageUsingAes(byte[] encryptedMessageWithSignature) {
        if (aesAlgorithmInstance is null) {
            throw new ArgumentNullException(nameof(aesAlgorithmInstance), "AES algorithm instance is not initialized.");
        }

        ICryptoTransform decryptor = aesAlgorithmInstance.CreateDecryptor(
            aesAlgorithmInstance.Key,
            aesAlgorithmInstance.IV
            );

        JObject json;

        using (MemoryStream ms = new MemoryStream(encryptedMessageWithSignature)) {
            using (CryptoStream cs= new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                using (StreamReader sr = new StreamReader(cs)) {
                    json = JObject.Parse(sr.ReadToEnd());
                }
            }
        }

        return json;
    }   

    private string VerifySenderAuthAndMessageIntegrity(JObject decryptedMessageWithSignature, (BigInteger n, BigInteger e) rsaPublicKeyExternal) {
        if (!decryptedMessageWithSignature.ContainsKey("plaintextMessage")) { throw new Exception("Missing key: 'plaintextMessage'"); }
        if (!decryptedMessageWithSignature.ContainsKey("messageSignature")) { throw new Exception("Missing key: 'messageSignature'"); }

        byte[] messageSignatureDecoded = Convert.FromBase64String((string)decryptedMessageWithSignature["messageSignature"]!);
        string plaintextMessage = (string)decryptedMessageWithSignature["plaintextMessage"]!;
        byte[] plaintextMessageHashed = GetMessageDigestUsingSHA256(plaintextMessage);


        RSA rsa = RSA.Create();

        RSAParameters rsaKeyInfo = new RSAParameters();

        rsaKeyInfo.Modulus = rsaPublicKeyExternal.n.ToByteArray(isUnsigned: true, isBigEndian: true);
        rsaKeyInfo.Exponent = rsaPublicKeyExternal.e.ToByteArray(isUnsigned: true, isBigEndian: true);

        rsa.ImportParameters(rsaKeyInfo);

        RSAPKCS1SignatureDeformatter rsaDeformatter = new(rsa);
        rsaDeformatter.SetHashAlgorithm(nameof(SHA256));

        if (!rsaDeformatter.VerifySignature(plaintextMessageHashed, messageSignatureDecoded))
        {
            throw new InvalidSignatureException("Either message has been tampered with, or message did not originate from true external/sender (or wrong external public key).");
        }

        return plaintextMessage; // we already performed the validation on the json argument

    }
}