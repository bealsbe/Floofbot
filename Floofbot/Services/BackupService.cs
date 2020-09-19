using Floofbot.Configs;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    class BackupService
    {
        private TimeSpan backupTime = new TimeSpan(2, 0, 0);
        public void Start()
        {
            if (string.IsNullOrEmpty(BotConfigFactory.Config.BackupOutputPath) || string.IsNullOrEmpty(BotConfigFactory.Config.BackupScript))
            {
                Log.Error("Backups not properly configured in the config file. Backups will not be taken for this session.");
                return;
            }
            else
            {
                Log.Information("Automatic backups enabled! Backups will be saved to " + BotConfigFactory.Config.BackupOutputPath + " at " + backupTime);
            }

            RunBackups();
        }
        public async void RunBackups()
        {
            while (true)
            {
                double targetDelay = DateTime.UtcNow.TimeOfDay.TotalSeconds - backupTime.TotalSeconds;
                if (targetDelay < 0)
                {
                    targetDelay += 86400;
                }
                await Task.Delay((int)targetDelay * 1000);
                System.Diagnostics.Process backupProcess = new System.Diagnostics.Process();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    backupProcess.StartInfo.FileName = "powershell.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    backupProcess.StartInfo.FileName = "/bin/bash";
                }
                else
                {
                    Log.Error("Backups can only be performed on linux or windows machines.");
                    return;
                }

                backupProcess.StartInfo.Arguments = BotConfigFactory.Config.BackupScript + " "
                                                    + BotConfigFactory.Config.DbPath + " "
                                                    + BotConfigFactory.Config.BackupOutputPath + " "
                                                    + BotConfigFactory.Config.NumberOfBackups; //arguments
                backupProcess.StartInfo.UseShellExecute = false;
                backupProcess.StartInfo.RedirectStandardOutput = true;
                backupProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                backupProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                try
                {
                    backupProcess.Start();
                    string output = backupProcess.StandardOutput.ReadToEnd(); //The output result
                    backupProcess.WaitForExit();
                    Log.Information(output);
                }
                catch (FileNotFoundException)
                {
                    Log.Error("The backup script file could not be found. Backup not successful.");
                    return;
                }
                catch (Exception e)
                {
                    Log.Fatal("Exception occured when trying to backup the the database: " + e);
                }
            }
        } 
    }
}
