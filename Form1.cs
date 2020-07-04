﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace DungeonsHelper
{
    public partial class Form1 : Form
    {

        private String gamePath = "";
        private String pakFile = "";

        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        private static class NativeMethods
        {
            internal const uint GW_OWNER = 4;

            internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
            internal static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern bool IsWindowVisible(IntPtr hWnd);
        }

        public IntPtr GetMainWindowHandle(int processId)
        {
            IntPtr MainWindowHandle = IntPtr.Zero;

            NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((hWnd, lParam) =>
            {
                IntPtr PID;
                NativeMethods.GetWindowThreadProcessId(hWnd, out PID);

                if (PID == lParam &&
                    NativeMethods.IsWindowVisible(hWnd) &&
                    NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER) == IntPtr.Zero)
                {
                    MainWindowHandle = hWnd;
                    return false;
                }

                return true;

            }), new IntPtr(processId));

            return MainWindowHandle;
        }

        public Process LaunchGame()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("explorer", "shell:AppsFolder\\Microsoft.Lovika_8wekyb3d8bbwe!Game");

            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Process process = Process.Start(startInfo);

            return process;
        }

        public Process FindGameHide()
        {
            Process[] processes = Process.GetProcessesByName("Dungeons");
            if (processes != null && processes.Length > 0)
            {
                IntPtr handle = GetMainWindowHandle(processes[0].Id);
                if ((int)handle > 0)
                {
                    NativeMethods.ShowWindow(handle, 0);
                }
                else
                {
                    return null;
                }

                return processes[0];
            }

            return null;
        }

        public bool KillGame(Process process)
        {
            if (process != null)
            {
                process.Kill();
            }
            return true;
        }

        delegate void UpdateLogCallback(String log);
        private void UpdateLog(String log)
        {
            if (this.InvokeRequired)
            {
                while (!this.IsHandleCreated)
                {
                    if (this.Disposing || this.IsDisposed)
                        return;
                }
                UpdateLogCallback d = new UpdateLogCallback(UpdateLog);
                this.Invoke(d, new object[] {log});
            }
            else
            {
                this.textBox1.AppendText(log + "\r\n");
            }

        }

        public void LaunchUWPInjector(Process gameProcess)
        {
            //创建一个进程
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + Application.StartupPath + "/Bin/UWPInjector.exe -p " + gameProcess.Id;

            // 必须禁用操作系统外壳程序
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
                
 
            //启动进程
            process.Start();

            //准备读出输出流和错误流
            string outputData = string.Empty;
            string errorData = string.Empty;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
 
            process.OutputDataReceived += (sender, e) =>
            {
                Console.WriteLine(e.Data);
            };

            while (!process.HasExited)
            {
                process.StandardInput.WriteLine();
                Thread.Sleep(100);
            }

            process.WaitForExit(); 
            //关闭进程
            process.Close();
 
            //返回流结果
        }

        public static void CopyDir(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            if (!Directory.Exists(toDir))
            {
                Directory.CreateDirectory(toDir);
            }

            string[] files = Directory.GetFiles(fromDir);
            foreach (string formFileName in files)
            {
                string fileName = Path.GetFileName(formFileName);
                string toFileName = Path.Combine(toDir, fileName);
                File.Copy(formFileName, toFileName, true);
            }
            string[] fromDirs = Directory.GetDirectories(fromDir);
            foreach (string fromDirName in fromDirs)
            {
                string dirName = Path.GetFileName(fromDirName);
                string toDirName = Path.Combine(toDir, dirName);
                CopyDir(fromDirName, toDirName);
            }
        }

        public static void MoveDir(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            CopyDir(fromDir, toDir);

            try
            {
                Directory.Delete(fromDir, true);
            }
            catch (Exception ex)
            {
            }
        }

        public void BackupSaves()
        {
            Directory.CreateDirectory("D:\\DungeonsSaves");
            Directory.CreateDirectory("D:\\DungeonsSaves\\1.0");
            Directory.CreateDirectory("D:\\DungeonsSaves\\1.2");

            // 获取“保存的游戏”目录
            IntPtr outPath;
            if (SHGetKnownFolderPath(new Guid("{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}"),
                0x00004000, new IntPtr(0), out outPath) >= 0)
            {
                CopyDir(Marshal.PtrToStringUni(outPath) + "\\Mojang Studios\\Dungeons\\", "D:\\DungeonsSaves\\1.2");
            }

            CopyDir("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Dungeons\\", "D:\\DungeonsSaves\\1.0");
            

            UpdateLog("存档备份至 D:/DungeonsSaves。");
            UpdateLog("注：六月更新后存档已升级，一般不会丢失，如有特殊情况请手动还原。");
        }

        public void RegApp()
        {
            // 卸载原APP
            ProcessStartInfo startInfo = new ProcessStartInfo("powershell", "Get-AppxPackage *Microsoft.Lovika* | Remove-AppxPackage");

            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            UpdateLog("原APP已卸载。");

            Thread.Sleep(1000);

            // 安装新目录
            startInfo = new ProcessStartInfo("powershell", "Add-AppxPackage -path " + gamePath + "\\appxmanifest.xml -register");

            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process = Process.Start(startInfo);
            process.WaitForExit();

            UpdateLog("新APP路径已注册。");
        }

        public void DoLocalization()
        {
            Process process = null;

            // 检测游戏在不在
            process = FindGameHide();

            if (process == null)
            {
                UpdateLog("正在启动游戏..");
                LaunchGame();

                while (true)
                {
                    process = FindGameHide();
                    if (process != null)
                    {
                        UpdateLog("游戏启动成功，等待加载完成..");
                        break;
                    }

                    Thread.Sleep(50);
                }

                // 等待20秒吧
                Thread.Sleep(1000 * 20);
            }
            else
            {
                UpdateLog("游戏正在运行，请稍候..");
            }

            UpdateLog("等待导出包..");

            // 启动注入工具导出包
            LaunchUWPInjector(process);

            // 关闭弹出的文件夹
            UpdateLog("包已导出。");

            UpdateLog("正在移动文件..");
            MoveDir("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\TempState\\DUMP", gamePath);

            UpdateLog("文件移动完毕，写入Paks..");
            File.Copy(Application.StartupPath + "/Paks/" + pakFile, gamePath + "\\Dungeons\\Content\\Paks\\" + pakFile, true);

            UpdateLog("汉化完成，等待重新注册应用..");

            KillGame(process);

            Thread.Sleep(1000);

            BackupSaves();

            RegApp();

            // 还原旧存档
            CopyDir("D:\\DungeonsSaves\\1.0", "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Dungeons\\");
            UpdateLog("存档还原完毕。");

            UpdateLog("全部完成，可以开始游戏了：）");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();

            if (textBox2.Text.Equals(""))
            {
                textBox2.Text = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\App";
            }

            gamePath = textBox2.Text;

            Directory.CreateDirectory(gamePath);

            ThreadStart threadStart = new ThreadStart(DoLocalization);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DirectoryInfo dir = new DirectoryInfo(Application.StartupPath + "/Paks/");
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name.ToLower().EndsWith(".pak"))
                {
                    label4.Text += file.Name + " ";
                    pakFile = file.Name;
                    break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BackupSaves();
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath + "\\Dungeons";
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.Links[0].LinkData = "https://tieba.baidu.com/f?fr=wwwt&kw=%E6%88%91%E7%9A%84%E4%B8%96%E7%95%8C%E5%9C%B0%E4%B8%8B%E5%9F%8E";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());  
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            RegApp();
        }
    }
}
