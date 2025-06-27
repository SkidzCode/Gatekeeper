using System.ComponentModel.DataAnnotations;

﻿namespace GateKeeper.Server.Models.Account;

public class Role
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(50, ErrorMessage = "Role name cannot be longer than 50 characters.")]
    public string RoleName { get; set; }
}