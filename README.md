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
* Source: "Computer Networking: A Top Down Approach 7th Ed" *

The above diagram illustrates what is happening behind the scenes at every encryption step, both Bob and Alice are essentially perfoming the following:

1. Encode the message, $m$, as an array of bytes, UTF-8 encoded.
2. Wrap said message byte array using a hash function, $H(\cdot)$ (the SHA256 algorithm in this case).
    - At this point, data is *encoded*, anyone can simply *decode* it if they know the hash algorithm used in this case.
    - Also known as a message digest, it helps because the encryption step in the asymmetric cryptography (specifically the RSA algorithm) is expensive.
    - The hash algorithm is essentially a one-to-one mapping. Any slight variation in the input will generate a completely different hash.
4. Using her RSA private key, which is known only to her, and no-one else, Alice passes the message digest and forms a *digital signature*. 
    - The digital signature is mathematically equivalent to a decryption step, denoted by the function $K^{-}_A(\cdot)$.
    - When you *verify a signature*, you are essentially obtaining the original message digest $h$, through an encryption step on top of the decryption step, $K^{+}_{A}(K^{-}_{A}(H(m))) = H(m) = h$. Then comparing $h$ with your own computed hash of the plaintext message (that came along with the signature) by computing if $H(m) = h$.
    - If the comparison succeeds, you can conclude that 1. the message did indeed originate from Alice, and 2. the message was unaltered in transit. 
Asymmetric and Symmetric Cryptography: Implements RSA for public/private key encryption and AES for symmetric encryption of messages.
Digital Signatures: Uses RSA to sign messages, allowing the receiver to verify the sender's authenticity and message integrity.
Web API Communication: Provides ASP.NET Core Web API endpoints for Alice and Bob to exchange public keys and encrypted messages.
In-Memory Caching: Utilizes in-memory caching to store cryptographic keys and instances for efficient, stateless API interactions.
Client-Server Workflow: The console app acts as a client, orchestrating the secure message exchange by interacting with the web API endpoints.
Message Pipeline: Includes end-to-end encryption, signing, transmission, decryption, and signature verification for secure communication.

# Why I Wrote It
I wrote it mainly because I wanted to fill in some knowledge gaps regarding basic networking and cryptography, as well as learn a new language/technology: C#/.NET. This basic project seemed like a great fit for lifting those compound muscles! I hope you enjoy it!

# How to Use
