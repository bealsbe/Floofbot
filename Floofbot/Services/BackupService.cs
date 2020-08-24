using Floofbot.Configs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    class BackupService
    {
        private int backupIndex = 0;
        private DateTime backupTime = new DateTime(1, 1, 1, 12, 0, 0);
        public BackupService()
        {
        }
        public async void Run()
        {
            await Task.Run(() =>
            {
                if (backupIndex == BotConfigFactory.Config.NumberOfBackups)
                    backupIndex = 0;
                else
                    backupIndex++;

                if (DateTime.Now.TimeOfDay == backupTime.TimeOfDay)
                {
                    Log.Information("Begining scheduled database backup...");

                    System.Diagnostics.Process backupProcess = new System.Diagnostics.Process();
                    backupProcess.StartInfo.FileName = BotConfigFactory.Config.BackupScript;
                    backupProcess.StartInfo.Arguments = BotConfigFactory.Config.BackupOutputPath + " " + backupIndex; //argument
                    backupProcess.StartInfo.UseShellExecute = false;
                    backupProcess.StartInfo.RedirectStandardOutput = true;
                    backupProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    backupProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                    backupProcess.Start();
                    string output = backupProcess.StandardOutput.ReadToEnd(); //The output result
                    backupProcess.WaitForExit();
                    Log.Information(output);
                }
            });
        }
    }
}
