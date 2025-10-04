using System.Text;
using System.Linq;

namespace Desafio_BT.Utils;

public static class LoggingUtils
{
    public static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[EMPTY]";
            
        if (input.Length > 100)
            input = input[..100];
            
        return string.Concat(input.Trim().Where(c => c >= 32 && c != 127));
    }
}