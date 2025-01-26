namespace GateKeeper.Server.Models.Account;


    public class SessionModel
    {
        public string Id { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string VerificationId { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool Complete { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string SessionData { get; set; } = string.Empty;

        // Fields from joined Verification table (optional)
        public string? VerifyType { get; set; }
        public DateTime? VerificationExpiryDate { get; set; }
        public bool? VerificationComplete { get; set; }
        public bool? VerificationRevoked { get; set; }
    }

