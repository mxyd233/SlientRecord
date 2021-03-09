using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NHotkey;
using NHotkey.WindowsForms;
using NAudio;
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Runtime.Serialization;
using Config.Net;
using System.Diagnostics;
using System.Linq;

namespace SlientRecord
{

    class Program
    {
        /// <summary>
        /// 导入模拟键盘的方法
        /// </summary>
        /// <param name="bVk" >按键的虚拟键值</param>
        /// <param name= "bScan" >扫描码，一般不用设置，用0代替就行</param>
        /// <param name= "dwFlags" >选项标志：0：表示按下，2：表示松开</param>
        /// <param name= "dwExtraInfo">一般设置为0</param>
        [DllImport("user32.dll")]
        private static extern void keybd_event(Keys bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private static bool recording = false;

        private static IConfig Cfg = null;

        private static void toggleCapsLock()
        {
            keybd_event(Keys.CapsLock, 0, 0, 0);
            keybd_event(Keys.CapsLock, 0, 2, 0);
        }
        private static void toggleMute()
        {
            keybd_event(Keys.VolumeMute, 0, 0, 0);
            keybd_event(Keys.VolumeMute, 0, 2, 0);
        }

        private static int GetRecordDeviceNumber()
        {
            if (string.IsNullOrWhiteSpace(Cfg.PartOfMicName))
                Cfg.PartOfMicName = WaveIn.GetCapabilities(0).ProductName;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var device = WaveIn.GetCapabilities(i);
                if (device.ProductName.Contains(Cfg.PartOfMicName))
                    return i;

            }
            return -1;
        }

        static void Main(string[] args)
        {
            var runningProcess = Process.GetProcesses();

            var curProcess = Process.GetCurrentProcess();
            var curFileName = curProcess.MainModule.FileName;

            if (runningProcess.Any(p => p.ProcessName == curProcess.ProcessName && p.Id != curProcess.Id))
                Environment.Exit(0);

            var execPath = Path.GetDirectoryName(curFileName);
            var cfgFileName = Path.Combine(execPath, "Config.ini");
            Cfg = new Config.Net.ConfigurationBuilder<IConfig>()
                .UseIniFile(cfgFileName)
                .Build();

            HotkeyManager.Current.AddOrReplace("record", Keys.Control | Keys.Alt | Keys.NumPad1, true, RecordKeyTriggered);

            Application.Run(new Form_HotkeyHandler());
        }
        private static WaveInEvent waveInEvent = null;
        private static WaveFileWriter writer = null;
        private static void RecordKeyTriggered(object sender, HotkeyEventArgs e)
        {

            if (!recording)
            {
                recording = true;
                Task.Run(() =>
                {
                    for (int i = 0; i < 8; i++)
                    {
                        toggleCapsLock();
                        Task.Delay(100).Wait();
                    }
                });
                waveInEvent = new();
                waveInEvent.WaveFormat = new WaveFormat(Cfg.Rate, Cfg.Bit, Cfg.Channels);
                waveInEvent.DeviceNumber = GetRecordDeviceNumber();


                var now = DateTime.Now;
                var savePath = Cfg.SaveTo;
                var folder = Path.Combine(savePath, now.ToShortDateString());
                var filename = Path.Combine(folder, now.ToString("HH.mm.ss") + ".wav");

                Directory.CreateDirectory(folder);


                writer = new(filename, waveInEvent.WaveFormat);
                waveInEvent.DataAvailable += (sender, e) =>
                {
                    writer.Write(e.Buffer, 0, e.BytesRecorded);
                };
                waveInEvent.RecordingStopped += (sender, e) =>
                {
                    writer?.Dispose();
                    writer = null;
                    waveInEvent.Dispose();
                    waveInEvent = null;
                };
                waveInEvent.StartRecording();
            }
            else
            {
                recording = false;
                Task.Run(() =>
                {
                    for (int i = 0; i < 8; i++)
                    {
                        toggleMute();
                        Task.Delay(100).Wait();
                    }
                });
                waveInEvent.StopRecording();

            }
        }
    }
}
