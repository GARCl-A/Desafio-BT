using System.Text;

namespace Desafio_BT.Utils;

public static class LoggingUtils
{
    public static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[EMPTY]";
            
        if (input.Length > 100)
            input = input[..100];
            
        var result = new StringBuilder(input.Length);
        foreach (char c in input.Trim())
        {
            if (c >= 32 && c != 127)
                result.Append(c);
        }
        return result.ToString();
    }
}