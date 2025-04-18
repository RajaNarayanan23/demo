#pragma warning disable CA1416
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Microsoft.AspNetCore.Mvc.Infrastructure;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }



    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (user == null)
        {
            return BadRequest(new { message = "User data is missing." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginWithCaptcha([FromForm] string email, [FromForm] string password, [FromForm] IFormFile captchaHalf)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || captchaHalf == null)
            return BadRequest(new { message = "Missing credentials or CAPTCHA half!" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password!" });

        var userImage = await _context.UserImages.FirstOrDefaultAsync(img => img.UserID == user.UserID);
        if (userImage == null)
            return NotFound(new { message = "No CAPTCHA record found for this user." });

        byte[] uploadedHalf;
        using (var ms = new MemoryStream())
        {
            await captchaHalf.CopyToAsync(ms);
            uploadedHalf = ms.ToArray();
        }

        byte[] decryptedUserHalf = null;
        byte[] decryptedServerHalf = null;

        try
        {
            switch (userImage.EncryptionMethod)
            {
                case "AES":
                    decryptedUserHalf = AESDecrypt(uploadedHalf, userImage.UserHalfKey, userImage.UserHalfIV);
                    decryptedServerHalf = AESDecrypt(userImage.ServerHalfEncrypted, userImage.ServerHalfKey, userImage.ServerHalfIV);
                    break;

                case "RSA":
                    decryptedUserHalf = RSADecrypt(uploadedHalf, null, userImage.UserPrivateKey); // Use user's private key
                    decryptedServerHalf = RSADecrypt(userImage.ServerHalfEncrypted, null, userImage.ServerPrivateKey); // Use server's private key
                    break;

                case "Blowfish":
                    decryptedUserHalf = BlowfishDecrypt(
                        uploadedHalf,
                        Convert.FromBase64String(userImage.UserHalfKey),
                        Convert.FromBase64String(userImage.UserHalfIV)
                    );

                    decryptedServerHalf = BlowfishDecrypt(
                        userImage.ServerHalfEncrypted,
                        Convert.FromBase64String(userImage.ServerHalfKey),
                        Convert.FromBase64String(userImage.ServerHalfIV)
                    );
                    break;

                default:
                    return StatusCode(500, new { message = "Unsupported encryption method." });
            }

            // Combine both halves
            byte[] combined = CombineHalves(decryptedUserHalf, decryptedServerHalf);

            // Compare with stored raw CAPTCHA
            bool match = combined.SequenceEqual(userImage.RawCaptcha);
            if (!match)
                return Unauthorized(new { message = "CAPTCHA verification failed!" });

            return Ok(new { message = "Login successful!", userId = user.UserID });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Decryption error: {ex.Message}" });
        }
    }

   private byte[] AESDecrypt(byte[] encryptedData, string base64Key, string base64IV)
    {
        using Aes aes = Aes.Create();
        aes.Key = Convert.FromBase64String(base64Key);
        aes.IV = Convert.FromBase64String(base64IV);

        using MemoryStream ms = new MemoryStream(encryptedData);
        using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using MemoryStream output = new MemoryStream();
        cs.CopyTo(output);
        return output.ToArray();
    }

    private byte[] RSADecrypt(byte[] encryptedData, string _ignored, string privateKeyB64)
    {
        string privateKeyXml = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyB64));
        using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(privateKeyXml);

        int chunkSize = 256; // For 2048-bit key with OAEP
        List<byte> decrypted = new List<byte>();

        for (int i = 0; i < encryptedData.Length; i += chunkSize)
        {
            byte[] chunk = encryptedData.Skip(i).Take(chunkSize).ToArray();
            decrypted.AddRange(rsa.Decrypt(chunk, true)); // OAEP padding = true
        }

        return decrypted.ToArray();
    }


    private byte[] BlowfishDecrypt(byte[] encryptedData, byte[] key, byte[] iv)
    {
        BufferedBlockCipher cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new BlowfishEngine()));
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));

        return ProcessCipher(cipher, encryptedData);
    }

    private byte[] CombineHalves(byte[] left, byte[] right)
    {
        using var leftImage = new Bitmap(new MemoryStream(left));
        using var rightImage = new Bitmap(new MemoryStream(right));

        int width = leftImage.Width + rightImage.Width;
        int height = leftImage.Height;

        using Bitmap combined = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(combined);
        g.DrawImage(leftImage, 0, 0);
        g.DrawImage(rightImage, leftImage.Width, 0);

        using MemoryStream ms = new MemoryStream();
        combined.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
 

    [HttpGet("generateCaptcha")]
    public IActionResult GenerateCaptcha(string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Email is required!" });

        try
        {
            Console.WriteLine($"📢 Generating CAPTCHA for: {email}");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            string captchaText = GenerateRandomCaptchaText();
            Bitmap captchaImage = GenerateCaptchaImage(captchaText);
            byte[] rawCaptchaBytes = BitmapToBytes(captchaImage);

            var (leftHalf, rightHalf) = SplitCaptchaImage(rawCaptchaBytes);

            string encryptionMethod = new[] { "AES", "RSA", "Blowfish" }[new Random().Next(3)];
            Console.WriteLine($"🔐 Selected Encryption Method: {encryptionMethod}");

            byte[] encryptedUserHalf = null;
            byte[] encryptedServerHalf = null;

            string userPublicKey = string.Empty, userPrivateKey = string.Empty;
            string serverPublicKey = string.Empty, serverPrivateKey = string.Empty;

            string userKey = string.Empty, userIV = string.Empty;
            string serverKey = string.Empty, serverIV = string.Empty;

            switch (encryptionMethod)
            {
                case "RSA":
                    var rsaUserResult = RSAEncrypt(leftHalf);
                    var rsaServerResult = RSAEncrypt(rightHalf);

                    encryptedUserHalf = rsaUserResult.EncryptedData;
                    encryptedServerHalf = rsaServerResult.EncryptedData;

                    userPublicKey = rsaUserResult.PublicKey;
                    userPrivateKey = rsaUserResult.PrivateKey;
                    serverPublicKey = rsaServerResult.PublicKey;
                    serverPrivateKey = rsaServerResult.PrivateKey;
                    break;


                case "AES":
                    var aesUserResult = AESEncrypt(leftHalf);
                    var aesServerResult = AESEncrypt(rightHalf);

                    encryptedUserHalf = aesUserResult.EncryptedData;
                    encryptedServerHalf = aesServerResult.EncryptedData;

                    userKey = aesUserResult.Key;
                    userIV = aesUserResult.IV;
                    serverKey = aesServerResult.Key;
                    serverIV = aesServerResult.IV;
                    break;

                case "Blowfish":
                    byte[] blowUserKey = Encoding.UTF8.GetBytes("BlowKeyUser123456");
                    byte[] blowUserIV = new byte[8];
                    byte[] blowServerKey = Encoding.UTF8.GetBytes("BlowKeyServer1234");
                    byte[] blowServerIV = new byte[8];

                    encryptedUserHalf = BlowfishEncrypt(leftHalf, blowUserKey, blowUserIV).EncryptedData;
                    encryptedServerHalf = BlowfishEncrypt(rightHalf, blowServerKey, blowServerIV).EncryptedData;

                    userKey = Convert.ToBase64String(blowUserKey);
                    userIV = Convert.ToBase64String(blowUserIV);
                    serverKey = Convert.ToBase64String(blowServerKey);
                    serverIV = Convert.ToBase64String(blowServerIV);
                    break;

                default:
                    throw new Exception("Unsupported encryption method!");
            }

            var userImage = new UserImage
            {
                UserID = user.UserID,
                RawCaptcha = rawCaptchaBytes,
                UserHalfEncrypted = encryptedUserHalf,
                ServerHalfEncrypted = encryptedServerHalf,
                EncryptionMethod = encryptionMethod,

                UserPublicKey = userPublicKey,
                UserPrivateKey = userPrivateKey,      // 👈 Store it
                ServerPublicKey = serverPublicKey,
                ServerPrivateKey = serverPrivateKey,

                UserHalfKey = userKey,
                UserHalfIV = userIV,
                ServerHalfKey = serverKey,
                ServerHalfIV = serverIV
            };


            _context.UserImages.Add(userImage);
            _context.SaveChanges();

            // 🧠 Combine both files into a ZIP archive
            using var memoryStream = new MemoryStream();
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var rawEntry = zip.CreateEntry("captcha_raw.png");
                using (var rawStream = rawEntry.Open())
                {
                    rawStream.Write(rawCaptchaBytes, 0, rawCaptchaBytes.Length);
                }

                var encryptedEntry = zip.CreateEntry("captcha_half.png");
                using (var encryptedStream = encryptedEntry.Open())
                {
                    encryptedStream.Write(encryptedUserHalf, 0, encryptedUserHalf.Length);
                }
            }

            return File(memoryStream.ToArray(), "application/zip", "captcha_bundle.zip");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔥 ERROR: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { message = $"Internal Server Error while generating CAPTCHA: {ex.Message}" });
        }
    }


    private (byte[], byte[]) SplitCaptchaImage(byte[] rawImage)
    {
        using (MemoryStream ms = new MemoryStream(rawImage))
        using (Bitmap original = new Bitmap(ms))
        {
            int width = original.Width;
            int height = original.Height;
            int halfWidth = width / 2;

            Bitmap leftHalf = original.Clone(new Rectangle(0, 0, halfWidth, height), original.PixelFormat);
            Bitmap rightHalf = original.Clone(new Rectangle(halfWidth, 0, halfWidth, height), original.PixelFormat);

            using (MemoryStream msLeft = new MemoryStream())
            using (MemoryStream msRight = new MemoryStream())
            {
                leftHalf.Save(msLeft, ImageFormat.Png);
                rightHalf.Save(msRight, ImageFormat.Png);
                return (msLeft.ToArray(), msRight.ToArray());
            }
        }
    }

    private int GetUserIdByEmail(string email)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        return user?.UserID ?? throw new Exception("User not found");
    }

    private string GenerateRandomCaptchaText()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[new Random().Next(chars.Length)]).ToArray());
    }

    private Bitmap GenerateCaptchaImage(string text)
    {
        int width = 200;
        int height = 80;
        Bitmap bitmap = new Bitmap(width, height);
        Random rand = new Random();

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
            for (int i = 0; i < 1000; i++)
            {
                int x = rand.Next(width);
                int y = rand.Next(height);
                bitmap.SetPixel(x, y, Color.Gray);
            }

            for (int i = 0; i < 10; i++)
            {
                Pen pen = new Pen(Color.LightGray, 2);
                g.DrawLine(pen, rand.Next(width), rand.Next(height), rand.Next(width), rand.Next(height));
            }

            using (Font font = new Font("Arial", 28, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                float angle = rand.Next(-15, 15);
                g.TranslateTransform(50, 30);
                g.RotateTransform(angle);
                g.DrawString(text, font, brush, new PointF(0, 0));
                g.ResetTransform();
            }
        }

        return bitmap;
    }

    private byte[] BitmapToBytes(Bitmap bitmap)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    private CaptchaEncryptionResult EncryptCaptcha(byte[] data, string encryptionMethod)
    {
        switch (encryptionMethod)
        {
            case "AES":
                return AESEncrypt(data);
            case "RSA":
                return RSAEncrypt(data);
            case "Blowfish":
                byte[] key = Encoding.UTF8.GetBytes("BlowKeyUser123456"); // or generate securely
                byte[] iv = new byte[8]; // 8-byte IV for Blowfish
                byte[] encryptedData = BlowfishEncrypt(data, key, iv).EncryptedData;

                return new CaptchaEncryptionResult
                {
                    EncryptedData = encryptedData,
                    Key = Convert.ToBase64String(key),
                    IV = Convert.ToBase64String(iv)
                };
            default:
                throw new Exception("Invalid encryption method!");
        }
    }



    private CaptchaEncryptionResult AESEncrypt(byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();

        return new CaptchaEncryptionResult
        {
            EncryptedData = ms.ToArray(),
            Key = Convert.ToBase64String(aes.Key),
            IV = Convert.ToBase64String(aes.IV)
        };
    }



    private CaptchaEncryptionResult RSAEncrypt(byte[] data)
    {
        using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);

        string publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ToXmlString(false)));
        string privateKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ToXmlString(true)));

        int maxLength = 214;
        List<byte> encryptedData = new List<byte>();

        for (int i = 0; i < data.Length; i += maxLength)
        {
            byte[] chunk = data.Skip(i).Take(maxLength).ToArray();
            byte[] encryptedChunk = rsa.Encrypt(chunk, true);
            encryptedData.AddRange(encryptedChunk);
        }

        return new CaptchaEncryptionResult
        {
            EncryptedData = encryptedData.ToArray(),
            PublicKey = publicKey,
            PrivateKey = privateKey
        };
    }



    private CaptchaEncryptionResult BlowfishEncrypt(byte[] data, byte[] key, byte[] iv)
    {
        try
        {
            BufferedBlockCipher cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new BlowfishEngine()));
            cipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));

            byte[] output = new byte[cipher.GetOutputSize(data.Length)];
            int length = cipher.ProcessBytes(data, 0, data.Length, output, 0);
            cipher.DoFinal(output, length);

            return new CaptchaEncryptionResult
            {
                EncryptedData = output,
                Key = Convert.ToBase64String(key),
                IV = Convert.ToBase64String(iv)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Blowfish encryption failed: {ex.Message}");
            return new CaptchaEncryptionResult();
        }
    }


    private byte[] ProcessCipher(IBufferedCipher cipher, byte[] data)
    {
        byte[] output = new byte[cipher.GetOutputSize(data.Length)];
        int length = cipher.ProcessBytes(data, 0, data.Length, output, 0);
        cipher.DoFinal(output, length);
        return output;
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CaptchaEncryptionResult
{
    public byte[] EncryptedData { get; set; }

    // For AES & Blowfish
    public string Key { get; set; }
    public string IV { get; set; }

    // For RSA
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}

#pragma warning restore CA1416
