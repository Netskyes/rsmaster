namespace RSMaster.Extensions
{
    internal static class ArrayExtension
    {
        public static T Take<T>(this T[] array, int index)
        {
            return index >= 0 && array.Length > index ? array[index] : default(T);
        }
    }
}
