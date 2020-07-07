using System;
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
using Microsoft.Win32;
using System.Management;

namespace DungeonsHelper
{
    public partial class Form1 : Form
    {

        private String gamePath = "";

        public Form1()
        {
            InitializeComponent();
        }

        private bool debug = false;

        public String OsMode()
        {
            if (IntPtr.Size == 8)
            {
                return "x64";
            }
            return "x86";
        }

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

            [DllImport("Shell32.dll")]
            internal static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

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

        // 检查是否在开发者模式
        public bool CheckIsDevelopmentMode()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\");
                if (key != null)
                {
                    key = key.OpenSubKey("AppModelUnlock");
                    if (key != null)
                    {
                        object obj = key.GetValue("AllowDevelopmentWithoutDevLicense");
                        if (obj != null)
                        {
                            if (obj.ToString().Equals("1"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
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

        // 线程中操作UI避免死锁
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

        delegate void FinishedCallback();
        private void Finished()
        {
            if (this.InvokeRequired)
            {
                while (!this.IsHandleCreated)
                {
                    if (this.Disposing || this.IsDisposed)
                        return;
                }
                FinishedCallback d = new FinishedCallback(Finished);
                this.Invoke(d, new object[] { });
            }
            else
            {
                this.button1.Enabled = true;
            }

        }

        public void LaunchUWPInjector(Process gameProcess)
        {
            //创建一个进程
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + Application.StartupPath + "/Bin/" + OsMode() + "/UWPInjector.exe -p " + gameProcess.Id;

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

        public void CopyDir(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            if (!Directory.Exists(toDir))
            {
                Directory.CreateDirectory(toDir);
            }

            //throw new Exception("sdfsdsf");

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

        public void MoveDir(string fromDir, string toDir)
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

        public void Rar(String soruceDir, String rarFileName)
        {
            String commandOptions = string.Format("a {0} {1} -r -ep1", rarFileName, soruceDir);
 
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = Application.StartupPath + "/Bin/rar.exe";
            processStartInfo.Arguments = commandOptions;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        public void UnRar(String saveDir, String rarFileName)
        {
            String commandOptions = string.Format("x {0} {1} -y", rarFileName, saveDir);
 
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = Application.StartupPath + "/Bin/rar.exe";
            processStartInfo.Arguments = commandOptions;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
 
            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        public void MoveDirByRar(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir))
                return;

            Rar(fromDir + "/*", "temp");
            UnRar(toDir + "/", "temp");

            try
            {
                File.Delete(Application.StartupPath + "/temp.rar");
                Directory.Delete(fromDir, true);
            }
            catch (Exception ex)
            {
            }
        }

        public bool IsGameCopySuccess()
        {
            return File.Exists(gamePath + "/appxmanifest.xml");
        }

        public void BackupSaves()
        {

            String path = Path.GetPathRoot(Application.StartupPath) + "DungeonsSaves";

            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "\\1.0");
            Directory.CreateDirectory(path + "\\1.2");

            // 获取“保存的游戏”目录
            IntPtr outPath;
            if (NativeMethods.SHGetKnownFolderPath(new Guid("{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}"),
                0x00004000, new IntPtr(0), out outPath) >= 0)
            {
                CopyDir(Marshal.PtrToStringUni(outPath) + "\\Mojang Studios\\Dungeons\\", path + "\\1.2");
            }

            CopyDir("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Dungeons\\", path + "\\1.0");


            UpdateLog("存档备份至 " + path + "。");
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
            try
            {
                Process process = null;

                if (!debug)
                {
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

                    if (!Directory.Exists("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\TempState\\DUMP"))
                    {
                        KillGame(process);
                        throw new Exception("导出包失败，请尝试手动操作Dumper或联系开发组。");
                    }
                }

                // 关闭弹出的文件夹
                UpdateLog("包已导出。");

                UpdateLog("正在移动文件..");
                try
                {
                    MoveDir("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\TempState\\DUMP", gamePath);
                }
                catch (Exception ex)
                {
                    // 尝试rar复制法
                    try
                    {
                        UpdateLog("移动文件失败，尝试使用压缩文件方式转移，可能需要一点时间..");
                        MoveDirByRar("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\TempState\\DUMP", gamePath);
                        if (!IsGameCopySuccess())
                        {
                            throw new Exception("压缩文件方式转移失败。");
                        }
                    }
                    catch (Exception ex2)
                    {
                        if ((int)MessageBox.Show("检测到错误：" + ex2.Message + "\r\n\r\n请不要点击按钮，现在需要您手动进行一个操作。操作如下：在弹出的文件夹中有一个“DUMP”文件夹，请打开它，将里面全部文件拷贝到“" + gamePath + "”下，注意目录层级(如果拷贝文件出现错误“您的组织不允许……”，请用压缩工具打包为压缩包拷贝过去再解压，可绕开这个错误)，拷贝完成后请点击确定继续执行。\r\n\r\n作者目前不清楚这个错误为何发生，如果您知道原因请右下角联系作者帮助改进程序，谢谢。", "手动操作提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != 1)
                        {
                            UpdateLog("取消执行。");
                            return;
                        }
                        else
                        {
                            UpdateLog("用户确认手动操作。");
                        }

                        // 检查是否正确移动                   
                    }
                }

                UpdateLog("文件移动完毕，写入Paks..");
                CopyDir(Application.StartupPath + "/Paks/", gamePath + "\\Dungeons\\Content\\Paks\\");

                UpdateLog("汉化完成，等待重新注册应用..");

                if (!debug)
                {

                    KillGame(process);

                    Thread.Sleep(1000);

                    BackupSaves();

                    RegApp();

                    // 还原旧存档
                    CopyDir(Path.GetPathRoot(Application.StartupPath) + "DungeonsSaves\\1.0", "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Dungeons\\");
                    UpdateLog("存档还原完毕。");

                }

                UpdateLog("全部完成，可以开始游戏了：）");
            }
            catch (Exception ex)
            {
                UpdateLog("错误：" + ex.Message);
            }
            finally
            {
                Finished();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!CheckIsDevelopmentMode())
            {
                if ((int)MessageBox.Show("检测到您的系统未打开“开发人员模式”，可能会导致游戏汉化失败。请您前往windows开始-->设置-->开发者选项-->选择开发人员模式，汉化完成后即可改回去。\r\n\r\n点击确定仍继续执行，点击取消返回。", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != 1)
                {
                    return;
                }
            }

            button1.Enabled = false;

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
                    break;
                }
            }

            if (File.Exists(Application.StartupPath + "/.GamePath"))
            {
                StreamReader sr = new StreamReader(Application.StartupPath + "/.GamePath", Encoding.UTF8);
                textBox2.Text = sr.ReadToEnd().Trim();
                sr.Close();

                gamePath = textBox2.Text;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Console.WriteLine(IsGameCopySuccess());
            BackupSaves();
        }

        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox2.Text;

            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (folderBrowserDialog1.SelectedPath.EndsWith("Dungeons"))
                {
                    textBox2.Text = folderBrowserDialog1.SelectedPath;
                }
                else
                {
                    textBox2.Text = folderBrowserDialog1.SelectedPath + "\\Dungeons";
                }

                gamePath = textBox2.Text;

                StreamWriter sw = new StreamWriter(Application.StartupPath + "/.GamePath");
                sw.WriteLine(textBox2.Text);
                sw.Flush();
                sw.Close();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.Links[0].LinkData = "https://www.mcbbs.net/forum.php?mod=viewthread&tid=1018057";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());  
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel2.Links[0].LinkData = "https://tieba.baidu.com/p/6789499149";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());  
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel3.Links[0].LinkData = "https://www.mcbbs.net/portal.php";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());  
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel4.Links[0].LinkData = "https://tieba.baidu.com/f?fr=wwwt&kw=%E6%88%91%E7%9A%84%E4%B8%96%E7%95%8C%E5%9C%B0%E4%B8%8B%E5%9F%8E";
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString()); 
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            CopyDir(Application.StartupPath + "/Paks/", Application.StartupPath + "/aaa/");
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            MoveDirByRar("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Packages\\Microsoft.Lovika_8wekyb3d8bbwe\\TempState\\DUMP", gamePath);
        }

    }
}
