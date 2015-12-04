using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BtmI2p.MiscUtils;
using CommandLine;
using Ionic.Zip;
using Nito.AsyncEx;
using NLog;

namespace BtmI2p.BitMoneyClient.Updater
{
    public class BitMoneyUpdaterOptions
    {
        [Option("pid", Required = true)]
        public int PidProcessWaitClose { get; set; }
        [Option("old-version-archive", Required = false, DefaultValue = null)]
        public string OldVersionArchiveFile { get; set; }
        [Option("new-version-archive", Required = true)]
        public string NewVersionArchiveFile { get; set; }
        [Option("app-dir-path", Required = true)]
        public string AppicationDirPath { get; set; }
        [Option("execute-on-exit", Required = false, DefaultValue = null)]
        public string ExecuteOnClose { get; set; }
        [Option("execute-on-exit-args", Required = false, DefaultValue = null)]
        public string ExecuteOnCloseArguments { get; set; }
    }
    public class BitMoneyUpdaterProgram
    {
        private static readonly Logger _log 
            = LogManager.GetCurrentClassLogger();

        private static async Task MainAsync(string[] args)
        {
	        try
			{
				var options = new BitMoneyUpdaterOptions();
				if (Parser.Default.ParseArguments(args, options))
				{
					if (!Directory.Exists(options.AppicationDirPath))
					{
						_log.Error("app-dir-path");
						return;
					}
					if (
						!string.IsNullOrWhiteSpace(
							options.OldVersionArchiveFile
							) && !File.Exists(options.OldVersionArchiveFile)
						)
					{
						_log.Error("old-version-archive");
						return;
					}
					if (!File.Exists(options.NewVersionArchiveFile))
					{
						_log.Error("new-version-archive");
						return;
					}
					if (
						!string.IsNullOrWhiteSpace(options.ExecuteOnClose)
						&& !File.Exists(options.ExecuteOnClose)
						)
					{
						_log.Error("execute-on-exit");
						return;
					}
					while (true)
					{
						try
						{
							Process.GetProcessById(
								options.PidProcessWaitClose
								);
							await Task.Delay(500).ConfigureAwait(false);
						}
						catch (ArgumentException)
						{
							break;
						}
					}
					await Task.Delay(500).ConfigureAwait(false);
					if (!string.IsNullOrWhiteSpace(options.OldVersionArchiveFile))
					{
						using (
							var oldZipArchive = ZipFile.Read(
								options.OldVersionArchiveFile
								)
							)
						{
							foreach (
								var entry 
									in oldZipArchive.Entries
										.Where(x => !x.IsDirectory)
								)
							{
								var oldFilePath = Path.Combine(
									options.AppicationDirPath,
									entry.FileName
									);
								if (File.Exists(oldFilePath))
									File.Delete(oldFilePath);
							}
						}
					}
					using (
						var newZipArchive = ZipFile.Read(
							options.NewVersionArchiveFile
							)
						)
					{
						foreach (var entry in newZipArchive.Entries)
						{
							if (entry.IsDirectory)
							{
								var newDirPath = Path.Combine(
									options.AppicationDirPath,
									entry.FileName
									);
								if (!Directory.Exists(newDirPath))
								{
									Directory.CreateDirectory(newDirPath);
								}
							}
							else
							{
								var newFilePath = Path.Combine(
									options.AppicationDirPath,
									entry.FileName
									);
								if (File.Exists(newFilePath))
									File.Delete(newFilePath);
								entry.Extract(options.AppicationDirPath);
							}
						}
					}
					if (!string.IsNullOrWhiteSpace(options.ExecuteOnClose))
					{
						var processStartInfo = new ProcessStartInfo();
						var workingDirectory = Path.GetDirectoryName(
							options.ExecuteOnClose
							);
						if (string.IsNullOrWhiteSpace(workingDirectory))
							throw new ArgumentNullException(
								MyNameof.GetLocalVarName(() => workingDirectory));
						processStartInfo.WorkingDirectory = workingDirectory;
						processStartInfo.FileName = options.ExecuteOnClose;
						processStartInfo.Arguments =
							string.IsNullOrWhiteSpace(options.ExecuteOnCloseArguments)
								? ""
								: options.ExecuteOnCloseArguments;
						Process.Start(
							processStartInfo
							);
					}
				}
				else
				{
					throw new Exception("Parse command line arguments error");
				}
			}
			catch (Exception exc)
		    {
			    _log.Error("Unexpected error '{0}'", exc.ToString());
		    }
        }

        public static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }
    }
}
