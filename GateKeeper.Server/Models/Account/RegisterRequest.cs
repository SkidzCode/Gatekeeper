using System.ComponentModel.DataAnnotations;
using GateKeeper.Server.Models.Attributes;

namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Website { get; set; } = string.Empty;

    [Required]
    [MustBeTrue(ErrorMessage = "You must agree to the user license agreement.")]
    public bool UserLicAgreement { get; set; } = false;

    [Required]
    public bool ReceiveEmails { get; set; } = false;
}
