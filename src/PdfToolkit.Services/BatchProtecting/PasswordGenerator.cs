using System.Security.Cryptography;
using System.Text;

namespace PdfToolkit.Services.BatchProtecting;

public static class PasswordGenerator
{
    private const string Lower   = "abcdefghjkmnpqrstuvwxyz";
    private const string Upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits  = "23456789";
    private const string Symbols = "!@#$%&*?+-";

    public static string Generate(int length = 12,
        bool useLower = true, bool useUpper = true,
        bool useDigits = true, bool useSymbols = false)
    {
        var pool = new StringBuilder();
        if (useLower)   pool.Append(Lower);
        if (useUpper)   pool.Append(Upper);
        if (useDigits)  pool.Append(Digits);
        if (useSymbols) pool.Append(Symbols);
        if (pool.Length == 0) pool.Append(Lower + Digits);

        var p = pool.ToString();
        var buf = new byte[length * 4];
        RandomNumberGenerator.Fill(buf);
        var result = new char[length];
        int j = 0;
        for (int i = 0; i < length; i++)
        {
            // Rejection sampling to avoid modulo bias
            int limit = (256 / p.Length) * p.Length;
            while (j < buf.Length && buf[j] >= limit) j++;
            if (j >= buf.Length) { RandomNumberGenerator.Fill(buf); j = 0; }
            result[i] = p[buf[j++] % p.Length];
        }
        return new string(result);
    }
}
