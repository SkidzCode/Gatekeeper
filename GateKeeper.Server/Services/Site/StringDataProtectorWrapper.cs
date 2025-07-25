using GateKeeper.Server.Interface;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

namespace GateKeeper.Server.Services.Site
{
    public class StringDataProtectorWrapper(IDataProtector dataProtector) : IStringDataProtector
    {
        private readonly IDataProtector _dataProtector = dataProtector ?? throw new ArgumentNullException(nameof(dataProtector));

        public string Protect(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = _dataProtector.Protect(plaintextBytes);
            return Convert.ToBase64String(protectedBytes);
        }

        public string Unprotect(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData))
                return protectedData;
            try
            {
                var protectedBytes = Convert.FromBase64String(protectedData);
                var plaintextBytes = _dataProtector.Unprotect(protectedBytes);
                return Encoding.UTF8.GetString(plaintextBytes);
            }
            catch (FormatException)
            {
                // Handle cases where the input is not a valid Base64 string
                // This might indicate tampering or an invalid cookie format.
                // Depending on policy, you might log this or return null/throw.
                // For now, returning null to indicate failure to unprotect.
                return null;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // This exception is often thrown if the data cannot be unprotected
                // (e.g., tampered, wrong key, etc.)
                return null;
            }
        }
    }
}
