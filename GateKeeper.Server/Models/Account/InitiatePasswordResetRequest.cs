using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for initiating password reset.
/// </summary>
public class InitiatePasswordResetRequest
{
    [Required]
    public string EmailOrUsername { get; set; }

    [Required]
    public string Website { get; set; } 
}