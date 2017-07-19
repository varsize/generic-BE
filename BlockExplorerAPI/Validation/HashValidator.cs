using System.Text.RegularExpressions;

namespace BlockExplorerAPI.Validation
{
    public static class HashValidator
    {
        private static readonly Regex HashRegex = new Regex("^[a-fA-F0-9]{64}$");

        public static bool Validate(string input)
        {
            if (input == null || input.Length != 64)
                return false;
            return HashRegex.IsMatch(input);
        }
    }
}