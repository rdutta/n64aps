using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace n64aps
{
    internal class Program
    {
        /// <summary>
        /// Main class.
        /// Entrypoint of the program.
        /// </summary>
        /// <example>n64aps single create -r ./Game.z64 -p ./PatchedGame.z64 -o ./out</example>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code.</returns>
        public static async Task<int> Main(string[] args)
        {
            return await Args.GetParser().InvokeAsync(args);
        }
    }
}
