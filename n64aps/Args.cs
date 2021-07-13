using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;

namespace n64aps
{
    internal class Args
    {
        internal static Parser GetParser()
        {
            Option<FileInfo> optRom = new Option<FileInfo>(
                aliases: new string[] { "--rom", "-r" },
                description: "Path of rom file"
            ).LegalFilePathsOnly().ExistingOnly();
            optRom.ArgumentHelpName = "rom";
            optRom.IsRequired = true;

            Option<FileInfo> optPatchedRom = new Option<FileInfo>(
                aliases: new string[] { "--patched-rom", "-p" },
                description: "Path of patched rom file"
            ).LegalFilePathsOnly().ExistingOnly();
            optPatchedRom.ArgumentHelpName = "rom";
            optPatchedRom.IsRequired = true;

            Option<FileInfo> optPatch = new Option<FileInfo>(
                aliases: new string[] { "--patch", "-p" },
                description: "Path of aps patch"
            ).LegalFilePathsOnly().ExistingOnly();
            optPatch.ArgumentHelpName = "patch";
            optPatch.IsRequired = true;

            Option<DirectoryInfo> optRomDir = new Option<DirectoryInfo>(
                aliases: new string[] { "--rom-dir", "-r" },
                description: "Path of roms dir"
            ).LegalFilePathsOnly().ExistingOnly();
            optRomDir.ArgumentHelpName = "dir";
            optRomDir.IsRequired = true;

            Option<DirectoryInfo> optPatchedDir = new Option<DirectoryInfo>(
                aliases: new string[] { "--patched-dir", "-p" },
                description: "Path of patched roms dir"
            ).LegalFilePathsOnly().ExistingOnly();
            optPatchedDir.ArgumentHelpName = "dir";
            optPatchedDir.IsRequired = true;

            Option<DirectoryInfo> optPatchDir = new Option<DirectoryInfo>(
                aliases: new string[] { "--patch-dir", "-p" },
                description: "Path of patches dir"
            ).LegalFilePathsOnly().ExistingOnly();
            optPatchDir.ArgumentHelpName = "dir";
            optPatchDir.IsRequired = true;

            Option<DirectoryInfo> optOutDir = new Option<DirectoryInfo>(
                aliases: new string[] { "--out-dir", "-o" },
                description: "Path of out dir",
                getDefaultValue: () => new DirectoryInfo(".")
            ).LegalFilePathsOnly().ExistingOnly();
            optOutDir.ArgumentHelpName = "dir";
            optOutDir.IsRequired = false;

            Command commandCreateSingle = new CommandBuilder(new Command("create", "Create patch"))
                .AddOption(optRom)
                .AddOption(optPatchedRom)
                .AddOption(optOutDir)
                .Command;
            commandCreateSingle.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Handler.CreateSingle);

            Command commandApplySingle = new CommandBuilder(new Command("apply", "Apply patch"))
                .AddOption(optRom)
                .AddOption(optPatch)
                .AddOption(optOutDir)
                .Command;
            commandApplySingle.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Handler.ApplySingle);

            Command commandRenameSingle = new CommandBuilder(new Command("rename", "Rename patch (CRC HI)"))
                .AddOption(optPatch)
                .AddOption(optOutDir)
                .Command;
            commandRenameSingle.Handler = CommandHandler.Create<FileInfo, DirectoryInfo>(Handler.RenameSingle);

            Command commandCreateMulti = new CommandBuilder(new Command("create", "Create patches"))
                .AddOption(optRomDir)
                .AddOption(optPatchedDir)
                .AddOption(optOutDir)
                .Command;
            commandCreateMulti.Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo, DirectoryInfo>(Handler.CreateMulti);

            Command commandApplyMulti = new CommandBuilder(new Command("apply", "Apply patches"))
                .AddOption(optRomDir)
                .AddOption(optPatchDir)
                .AddOption(optOutDir)
                .Command;
            commandApplyMulti.Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo, DirectoryInfo>(Handler.ApplyMulti);

            Command commandRenameMulti = new CommandBuilder(new Command("rename", "Rename patches (CRC HI)"))
                .AddOption(optPatchDir)
                .AddOption(optOutDir)
                .Command;
            commandRenameMulti.Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo>(Handler.RenameMulti);

            Command commandSingle = new CommandBuilder(new Command("single", "Process single rom and/or patch"))
                .AddCommand(commandCreateSingle)
                .AddCommand(commandApplySingle)
                .AddCommand(commandRenameSingle)
                .Command;

            Command commandMulti = new CommandBuilder(new Command("multi", "Process multiple roms and/or patches"))
                .AddCommand(commandCreateMulti)
                .AddCommand(commandApplyMulti)
                .AddCommand(commandRenameMulti)
                .Command;

            Command commandRoot = new CommandBuilder(new RootCommand())
                .AddCommand(commandSingle)
                .AddCommand(commandMulti)
                .Command;

            static void ExHandler(Exception exception, InvocationContext context)
            {
                if (exception is not OperationCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    if (exception is AggregateException ae)
                    {
                        foreach (Exception ex in ae.Flatten().InnerExceptions)
                        {
                            Console.Error.WriteLine(exception.Message);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(exception.InnerException?.Message ?? exception.ToString());
                    }
                }

                context.ExitCode = 1;
            }

            return new CommandLineBuilder(commandRoot).UseDefaults().UseExceptionHandler(ExHandler).Build();
        }
    }
}
