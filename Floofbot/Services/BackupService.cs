using Floofbot.Configs;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    class BackupService
    {
        private TimeSpan _backupTime = new TimeSpan(2, 0, 0);
        
        public void Start()
        {
            if (string.IsNullOrEmpty(BotConfigFactory.Config.BackupOutputPath) || string.IsNullOrEmpty(BotConfigFactory.Config.BackupScript))
            {
                Log.Error("Backups not properly configured in the config file. Backups will not be taken for this session.");
                return;
            }

            Log.Information("Automatic backups enabled! Backups will be saved to " + BotConfigFactory.Config.BackupOutputPath + " at " + _backupTime);

            RunBackups();
        }

        private async void RunBackups()
        {
            while (true)
            {
                var targetDelay = _backupTime.TotalSeconds - DateTime.UtcNow.TimeOfDay.TotalSeconds;
                
                if (targetDelay < 0)
                {
                    targetDelay += 86400;
                }
                
                await Task.Delay((int)targetDelay * 1000);
                
                var backupProcess = new System.Diagnostics.Process();
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    backupProcess.StartInfo.FileName = "powershell.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    backupProcess.StartInfo.FileName = "/bin/sh";
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
                    
                    var output = await backupProcess.StandardOutput.ReadToEndAsync(); //The output result
                    
                    backupProcess.WaitForExit();
                    
                    Log.Information(output);
                    
                    if (backupProcess.ExitCode != 0)
                    {
                        Log.Error("Backup script failed. Process returned exit code " + backupProcess.ExitCode);
                    }
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

                // Safety sleep to ensure backups only run once per day
                await Task.Delay(5000);
            }
        } 
    }
}
