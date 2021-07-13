using ShellProgressBar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace n64aps
{
    internal class Handler
    {
        private static readonly ProgressBarOptions s_progressBarOptions = new()
        {
            ForegroundColor = ConsoleColor.Green,
            ForegroundColorDone = ConsoleColor.White,
            BackgroundColor = ConsoleColor.Red,
            ProgressCharacter = '─',
            ProgressBarOnBottom = true
        };

        internal static void CreateSingle(FileInfo rom, FileInfo patchedRom, DirectoryInfo outDir)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Patcher.Create(rom, patchedRom, outDir);

            sw.Stop();

            Utility.PrintSecondsElapsed(sw.Elapsed);

            Utility.OpenDirectory(outDir.FullName);
        }

        internal static void CreateMulti(DirectoryInfo romDir, DirectoryInfo patchedDir, DirectoryInfo outDir)
        {

            ConcurrentQueue<Exception> exQueue = new();

            IEnumerable<FileInfo>? roms = patchedDir.EnumerateFiles();

            using ProgressBar pbar = new(roms.Count(), $"Writing patches to {outDir.FullName}", s_progressBarOptions);

            Parallel.ForEach(roms, patchedRom =>
            {
                try
                {
                    FileInfo rom = new(Path.Combine(romDir.FullName, patchedRom.Name));

                    if (!rom.Exists)
                    {
                        throw new ArgumentException($"Could not find file:", rom.FullName);
                    }

                    Patcher.Create(rom, patchedRom, outDir);

                    pbar.Tick();
                }
                catch (Exception ex)
                {
                    exQueue.Enqueue(ex);
                }
            });

            Utility.OpenDirectory(outDir.FullName);

            if (!exQueue.IsEmpty)
            {
                throw new AggregateException(exQueue);
            }
        }

        internal static void ApplySingle(FileInfo rom, FileInfo patch, DirectoryInfo outDir)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Patcher.Apply(rom, patch, outDir);

            sw.Stop();

            Utility.PrintSecondsElapsed(sw.Elapsed);

            Utility.OpenDirectory(outDir.FullName);
        }

        internal static void ApplyMulti(DirectoryInfo romDir, DirectoryInfo patchDir, DirectoryInfo outDir)
        {
            ConcurrentQueue<Exception> exQueue = new();

            IEnumerable<FileInfo>? patches = patchDir.EnumerateFiles();

            using ProgressBar pbar = new(patches.Count(), $"Writing roms to {outDir.FullName}", s_progressBarOptions);

            Parallel.ForEach(patches, patch =>
            {
                try
                {
                    FileInfo rom = new(Path.Combine(romDir.FullName, patch.Name.Replace(Patcher.s_extAps, Patcher.s_extZ64)));

                    if (!rom.Exists)
                    {
                        throw new ArgumentException($"Could not find file:", rom.FullName);
                    }

                    Patcher.Apply(rom, patch, outDir);

                    pbar.Tick();
                }
                catch (Exception ex)
                {
                    exQueue.Enqueue(ex);
                }
            });

            Utility.OpenDirectory(outDir.FullName);

            if (!exQueue.IsEmpty)
            {
                throw new AggregateException(exQueue);
            }
        }

        internal static void RenameSingle(FileInfo patch, DirectoryInfo outDir)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Patcher.Rename(patch, outDir);

            sw.Stop();

            Utility.PrintSecondsElapsed(sw.Elapsed);

            Utility.OpenDirectory(outDir.FullName);
        }

        internal static void RenameMulti(DirectoryInfo patchDir, DirectoryInfo outDir)
        {
            ConcurrentQueue<Exception> exQueue = new();

            IEnumerable<FileInfo>? patches = patchDir.EnumerateFiles();

            Stopwatch sw = Stopwatch.StartNew();

            Parallel.ForEach(patches, patch =>
            {
                try
                {
                    Patcher.Rename(patch, outDir);
                }
                catch (Exception ex)
                {
                    exQueue.Enqueue(ex);
                }
            });

            sw.Stop();

            Utility.PrintSecondsElapsed(sw.Elapsed);

            Utility.OpenDirectory(outDir.FullName);

            if (!exQueue.IsEmpty)
            {
                throw new AggregateException(exQueue);
            }
        }
    }
}
