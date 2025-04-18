using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int UserID { get; set; }

    [Required, MaxLength(255)]
    public required string Username { get; set; }

    [Required, MaxLength(255)]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; } // Store hashed password

  
     // RSA, AES, Blowfish
}
