using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserImage
{
    [Key]
    public int ImageID { get; set; }

    [ForeignKey("User")]
    public int UserID { get; set; }

    // 🔄 Stores the full original CAPTCHA before encryption or splitting
    // [Required]
    // public byte[] OriginalCaptcha { get; set; } = Array.Empty<byte>();

    // 🧩 Optional: Preprocessed or temp version of the CAPTCHA, or remove if unused
    [Required]
    public byte[] RawCaptcha { get; set; } = Array.Empty<byte>();

    // 🔐 Encrypted halves (from client/server)
    [Required]
    public byte[] UserHalfEncrypted { get; set; } = Array.Empty<byte>();

    [Required]
    public byte[] ServerHalfEncrypted { get; set; } = Array.Empty<byte>();

    [Required]
    public string EncryptionMethod { get; set; } = string.Empty;

    // 🔐 Asymmetric key pairs (Base64 encoded)
    public string UserPublicKey { get; set; } = string.Empty;
    public string UserPrivateKey { get; set; } = string.Empty; // Needed for decryption

    public string ServerPublicKey { get; set; } = string.Empty;
    public string ServerPrivateKey { get; set; } = string.Empty;

    // 🔑 Symmetric key info (AES, etc.)
    public string UserHalfKey { get; set; } = string.Empty;
    public string UserHalfIV { get; set; } = string.Empty;
    public string ServerHalfKey { get; set; } = string.Empty;
    public string ServerHalfIV { get; set; } = string.Empty;

    public User User { get; set; }
}
