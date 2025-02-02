namespace GateKeeper.Server.Models.Account.Notifications
{
    public class NotificationInsertResponse
    {
        public int NotificationId { get; set; }
        public string VerificationId { get; set; } = "";
    }
}
