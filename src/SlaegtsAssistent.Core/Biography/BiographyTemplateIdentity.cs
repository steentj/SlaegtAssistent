using System.Security.Cryptography;
using System.Text;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyTemplateIdentity
{
    public static string ComputeHash(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
