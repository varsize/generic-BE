using System.Text.RegularExpressions;

namespace BlockExplorerAPI.Validation
{
    public static class AddressValidator
    {
        private static readonly Regex AddressRegex = new Regex("^P[a-km-zA-HJ-NP-Z1-9]{26,33}$");

        public static bool Validate(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Length < 26 || input.Length > 34)
                return false;

            return AddressRegex.IsMatch(input);
        }
    }
}