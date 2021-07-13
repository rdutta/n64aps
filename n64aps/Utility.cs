using System;

namespace n64aps
{
    internal class Utility
    {
        public static void PrintSecondsElapsed(TimeSpan ts)
        {
            string elapsedTime = string.Format("Completed in {0} seconds", ts.TotalSeconds);

            Console.WriteLine(elapsedTime);
        }

        /* https://stackoverflow.com/a/1132559 */
        public static void OpenDirectory(string path)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = @path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        /* https://gist.github.com/crozone/06c4aa41e13be89def1352ba0d378b0f */
        public static void HexEncodeBytes(ReadOnlySpan<byte> inputBytes, Span<char> hexEncodedChars)
        {
            Span<char> hexAlphabet = stackalloc char[] {
                '0',
                '1',
                '2',
                '3',
                '4',
                '5',
                '6',
                '7',
                '8',
                '9',
                'A',
                'B',
                'C',
                'D',
                'E',
                'F'
            };

            for (int i = 0; i < inputBytes.Length; i++)
            {
                hexEncodedChars[i * 2] = hexAlphabet[inputBytes[i] >> 4];
                hexEncodedChars[(i * 2) + 1] = hexAlphabet[inputBytes[i] & 0xF];
            }
        }

        /* https://gist.github.com/crozone/06c4aa41e13be89def1352ba0d378b0f */
        public static string BytesToHexString(ReadOnlySpan<byte> inputBytes)
        {
            int finalLength = inputBytes.Length * 2;
            Span<char> encodedChars = finalLength < 2048 ? stackalloc char[finalLength] : new char[finalLength];
            HexEncodeBytes(inputBytes, encodedChars);
            return new string(encodedChars);
        }
    }
}
