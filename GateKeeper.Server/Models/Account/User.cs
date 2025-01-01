namespace GateKeeper.Server.Models.Account;
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string? Password { get; set; }
    public string Phone { get; set; }
    public string? Salt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new List<string>();

    public Task ClearPHIAsync()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        Username = string.Empty;
        Phone = string.Empty;
        Password = string.Empty;
        Salt = string.Empty;
        Roles.Clear();
        return Task.CompletedTask;
    }
}