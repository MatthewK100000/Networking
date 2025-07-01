# About
This project is a demonstration of secure, cryptographically authenticated messaging between two parties: Alice and Bob. It enhances HTTP plaintext communication over the web (e.g. between two machines) by satisfying 1. secrecy, 2. sender authentication, and 3. message integrity using principles of 
symmetric and asymmetric key cryptography. In other words, it puts the S in HTTPS.

In the background, the following steps are performed:

1. Alice posts her RSA public key for Bob (and the whole world) to see. Her ASP.NET Core Web Api server caches it if not yet available. Any future traffic to her public key endpoint taps into this in-memory cache. 
3. Alice reads Bob's RSA public key by calling his ASP.NET Core Web Api server. Bob computes it if not yet available, or fetches it from the in-memory cache.
4. Alice encrypts her message, which is meant to be secret once network-bound. It is mathematically near-impossible to decrypt it by today's standards (for e.g. by reading network packets using a tool like Wireshark).
5. Bob tries to decrypt the incoming traffic, hopefully from Alice, then encrypts his reply before sending it back to the recipient (only if it did indeed originate from Alice, undisturbed).
6. Alice reads the incoming reply, hopefully from Bob, then attempts to decrypt it (only if it did indeed originate from Bob, undisturbed).

# Going Deeper...

![image](https://github.com/user-attachments/assets/c69ab35f-a4bb-4012-bfdb-9e180d883a47)
*Source: "Computer Networking: A Top Down Approach 7th Ed"*

The above diagram illustrates what is happening behind the scenes at every encryption step, both Bob and Alice are essentially perfoming the following:

1. Encode the message, $m$, as an array of bytes, *UTF-8 encoded*.
    - The UTF-8 encoding is how to process each chain of bytes so it makes a "word", you can think of each encoding standard as essentially forming a different language and is interpreted differently. Analogous to a word in German being two words in English. 
3. Wrap said message byte array using a hash function, $H(\cdot)$ (the SHA256 algorithm in this case).
    - At this point, data is *encoded*, anyone can simply *decode* it if they know the hash algorithm used in this case.
    - Also known as a message digest, it helps because the encryption step in the asymmetric cryptography (specifically the RSA algorithm) is expensive.
    - The hash algorithm is essentially a one-to-one mapping. Any slight variation in the input will generate a completely different hash.
4. Using her RSA private key, which is known only to her, and no-one else, Alice passes the message digest and forms a *digital signature*. 
    - The digital signature is mathematically equivalent to a decryption step, denoted by the function $K^{-}_A(\cdot)$.
    - When you *verify a signature*, you are essentially obtaining the original message digest $h$, through an encryption step on top of the decryption step, $K^{+}_A(K^{-}_A(H(m)))=H(m)=h$. Then comparing $h$ with your own computed hash of the plaintext message (that came along with the signature) by computing if $H(m) = h$.
    - If the comparison succeeds, you can conclude that 1. the message did indeed originate from Alice, and 2. the message was unaltered in transit.
    - If the comparison fails, then either 1. the message did not originate from Alice, or 2. the message was altered in transit. 
5. Alice packs the digital signature, along with the original message, serializing it into a JSON string, before encoding it into a byte array and encrypting it again, using a symmetric cryptography (AES algorithm in this case).
    - AES is considered less expensive than RSA for long inputs. However, the drawback is that both parties must both have the same key before any communication is sent over the wire.
    - Note that the byte array format of the digital signature must be converted into a format safe for transmission (before being wrapped as a JSON key) such as *Base64 encoding*, which converts a byte array into a string. 
6. Alice packs the encrypted message+signature byte array, now base64 encoded into a string, along with the AES public key (encrypted using Bob's public RSA key so that only Bob can read it) byte array, now base64 encoded as a string, forming a JSON with two keys.
    - This technique of delivering the AES key RSA encrypted so that only Bob can read it, means Bob and Alice both can have it, which means they can just rely on symmetric cryptography instead. Again, less expensive for long inputs than asymmetric cryptography.

Decryption is the above process, but in reverse, with a check that verifies the digital signature before returning the underlying secret message. 

# Technology

- The ``Networking.Common`` folder houses the .NET class library for both Bob and Alice, inheriting from class Person. Also the Web Api Client/Service which is registered through dependency injection in the console application under ``Networking.Console``. 
- The Web Api Client/Service is essentially just a thin wrapper/client around the raw HTTP requests, allowing you to make calls using methods without worrying about disposing unmanaged resources and is recommended by Microsoft for making HTTP requests because each request must use a brand new HTTP Client.
- ``Networking.Console`` folder houses the main console application, where Alice starts (and ends) a conversation with Bob.
- ``Networking.WebApi`` folder houses ASP.NET Core Web Api Server, which is meant to be running in the background as Alice and Bob exchange words. The Controllers define the HTTP endpoints used for displaying the RSA public keys through GET, as well as processing the incoming messages and returning through POST.
- Alice and Bob have their own web api servers. They can even be hosted on different machines with some slight alterations to the the host and port instead of using localhost. 
- The ``Networking.WebApi`` folder also has the Progam.cs entrypoint used for configuring the HTTP pipeline using dependency injection before actually starting to run the server. Loggers and in memory cache are both injected into the controllers.
- ``run_all.sh`` is a bash/shell script so you don't have to worry about your console being cluttered with HTTP information/warnings along with the actual content of interest to be produced (conversation between Alice and Bob), and so that you don't have to run the web api server in a separate window apart from the console application.


# Why I Wrote It
I wrote it mainly because I wanted to fill in some knowledge gaps regarding basic networking and cryptography, as well as learn a new language/technology: C#/.NET. This basic project seemed like a great fit for lifting those compound muscles! I hope you enjoy it!

# How to Use
