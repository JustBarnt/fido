using System.CommandLine;
using System.Diagnostics;

namespace fido;

static class Program
{
    private static Option<FileInfo> CreateFileOption() =>
    new Option<FileInfo>("--file", "-f")
    {
        Description = "Path to an existing file",
        Required = true,
        CustomParser = result =>
        {
            string? raw = result.Tokens.SingleOrDefault()?.Value;

            if (raw is null)
            {
                result.AddError("A file was not provided");
                return null!;
            }

            var fi = new FileInfo(raw);

            if (!fi.Exists)
            {
                result.AddError($"File not found: '{raw}'");
                return null!;
            }

            return fi;
        }
    };
    private static int Main(string[] args)
    {
        RootCommand rootCommand = BuildRootCommand();
        return rootCommand.Parse(args).Invoke();
    }
    private static RootCommand BuildRootCommand()
    {
        var fileOption = CreateFileOption();

        // I want this to use the value from fileOption if one is not given for --info
        var versionInfoOption = new Option<FileVersionInfo>("--info", "-i")
        {
            Description = "Returns infomation about the given file.",
            CustomParser = result => 
            {
                string? raw = result.Tokens.SingleOrDefault()?.Value;

                if (raw is null)
                {
                    result.AddError("A file was not provided");
                    return null!;
                }

                return FileVersionInfo.GetVersionInfo(raw);
            },
            DefaultValueFactory = result =>
            {
                FileInfo? file = result.GetValue(fileOption);
                if (file is null || !file.Exists) return null!;

                return FileVersionInfo.GetVersionInfo(file.FullName);
            }
        };

        var rootCommand = new RootCommand("fido - a cli tool for viewing FileInfo")
        { fileOption, versionInfoOption };

        rootCommand.SetAction(parseResult =>
        {
                FileInfo        file = parseResult.GetValue(fileOption)!;
                FileVersionInfo fvi  = parseResult.GetValue(versionInfoOption)!;

                Console.WriteLine("FILE INFO");
                Console.WriteLine($"  File Name    : {file.Name} ({file.Length} bytes)");
                Console.WriteLine($"  File Size    : {FormatFileSize(file.Length)}");
                Console.WriteLine($"  Creation     : {file.CreationTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Accessed     : {file.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Modified     : {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Extension    : {file.Extension}");
                Console.WriteLine($"  Read-only    : {file.IsReadOnly}");
                Console.WriteLine($"  Attributes   : {file.Attributes}");

                Console.WriteLine("FILE VERSION INFO");
                Console.WriteLine($"  Company Name : {fvi.CompanyName}");
                Console.WriteLine($"  Product Name : {fvi.ProductName}");
                Console.WriteLine($"  Major        : {fvi.ProductMajorPart}");
                Console.WriteLine($"  Minor        : {fvi.ProductMinorPart}");
                Console.WriteLine($"  Build        : {fvi.ProductBuildPart}");
                Console.WriteLine($"  Version      : {fvi.ProductVersion}");
        });

        return rootCommand;
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024               => $"{bytes} B",
        < 1024 * 1024        => $"{bytes / 1024.0:N2} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):N2} MB",
        _                    => $"{bytes / (1024.0 * 1024 * 1024):N2} GB"
    };
}
