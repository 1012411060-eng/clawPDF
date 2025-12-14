using System.Diagnostics;
using System.IO;
using clawSoft.clawPDF.Core.Jobs;
using clawSoft.clawPDF.Core.Settings;
using NLog;

namespace clawSoft.clawPDF.Workflow
{
    internal class UiEmailCommandResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    internal class UiEmailCommandExecutor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public UiEmailCommandResult Execute(IJob job, UiEmailButtonSettings settings)
        {
            var commandPath = job.TokenReplacer.ReplaceTokens(settings.CommandPath ?? "");
            var commandArguments = job.TokenReplacer.ReplaceTokens(settings.CommandArguments ?? "");

            if (string.IsNullOrWhiteSpace(commandPath))
            {
                const string message = "No command configured for UI email button.";
                Logger.Error(message);
                return new UiEmailCommandResult { Success = false, ErrorMessage = message };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = commandPath,
                Arguments = commandArguments,
                UseShellExecute = false,
                CreateNoWindow = settings.HideWindow,
                WindowStyle = settings.HideWindow ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };

            var workingDirectory = Path.GetDirectoryName(commandPath);
            if (!string.IsNullOrEmpty(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            Logger.Info("Launching UI email command: {0} {1}", startInfo.FileName, startInfo.Arguments);

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.EnableRaisingEvents = true;

                    if (!process.Start())
                    {
                        const string message = "Failed to start UI email command.";
                        Logger.Error(message);
                        return new UiEmailCommandResult { Success = false, ErrorMessage = message };
                    }

                    if (settings.WaitForExit)
                    {
                        process.WaitForExit();
                        Logger.Info("UI email command exited with code {0}", process.ExitCode);

                        if (process.ExitCode != 0)
                        {
                            var errorMessage = "UI email command exited with code " + process.ExitCode + ".";
                            return new UiEmailCommandResult { Success = false, ErrorMessage = errorMessage };
                        }
                    }
                    else
                    {
                        Logger.Info("UI email command started without waiting for completion.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception while executing UI email command.");
                return new UiEmailCommandResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }

            return new UiEmailCommandResult { Success = true };
        }
    }
}
