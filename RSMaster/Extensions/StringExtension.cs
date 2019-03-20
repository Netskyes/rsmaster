namespace RSMaster.Extensions
{
    public static class StringExtension
    {
        public static bool HasValue(this string input)
        {
            return !string.IsNullOrEmpty(input);
        }
    }
}
