namespace GateKeeper.Server.Interface
{
    public interface IStringDataProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedData);
    }
}
