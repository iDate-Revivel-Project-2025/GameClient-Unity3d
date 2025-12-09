using System;
using System.Text;

namespace iDate.Network
{
    public static class CryptoHelper
    {
        /// <summary>
        /// Scrambles/Unscrambles the login string based on the iDate swapping algorithm.
        /// Algorithm: Swap(i, i+2) for every 4-block indices 0,1.
        /// </summary>
        public static string Scramble(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            char[] chars = input.ToCharArray();
            int len = chars.Length;

            for (int i = 0; i < len - 2; i++)
            {
                // Pattern: Swap 0 with 2, 1 with 3. Skip 2, 3. Swap 4 with 6, 5 with 7.
                if (i % 4 < 2)
                {
                    char temp = chars[i];
                    chars[i] = chars[i + 2];
                    chars[i + 2] = temp;
                }
            }

            return new string(chars);
        }
    }
}
