#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Hardware;
using MediaPortal.Hooks;
using MediaPortal.Util;
using MediaPortal.Player;
using Microsoft.Win32;
using System.Xml;
using System.Collections;
using TvControl;


namespace MPTray
{
  public class ShellApplication
  {
    private NotifyIcon _systemNotificationAreaIcon;
    private bool _isRecording = false;
    private ContextMenu _contextMenuTray;
    private bool ev = true;
    private TvServer _tvserver = new TvServer();

    #region Methods

    private void InstallKeyboardHook()
    {
      Log.Write("MPTray: InstallKeyboardHook");
      _keyboardHook = new KeyboardHook();
      _keyboardHook.KeyDown += OnKeyDown;
      _keyboardHook.KeyUp += OnKeyUp;
      _keyboardHook.IsEnabled = true;
    }

    private static void SwitchFocus()
    {
      Log.Write("MPTray: SwitchFocus");

      int attempts = 0;

      while (attempts < 120) 
      {
        Process[] processes = Process.GetProcessesByName("mediaportal");
          
        if (processes.Length > 0)
        {
            IntPtr handle = processes[0].MainWindowHandle;
            Log.Write("MPTray: handle=" + handle.ToString());

            // Make MediaPortal window normal ( if minimized )
            Win32API.ShowWindow(handle, Win32API.ShowWindowFlags.ShowNormal);

            // Make Mediaportal window focused
            if (Win32API.SetForegroundWindow(handle, true))
            {
                Log.Write("MPTray: Successfully switched focus.");
                break;
            }
            else {
                Log.Write("MPTray: error calling SetForegroundWindow");
            }
        }
        attempts++;
        Log.Write("MPTray: waiting for process, retrying ({0})", attempts);
        Thread.Sleep(1000);
      }

    }

    private static void OnClick(object sender, RemoteEventArgs e)
    {
      Log.Write("MPTray: OnClick");

        switch (e.Button)
        {
            case RemoteButton.Start:
                #region start

                Process[] processes = Process.GetProcessesByName("mediaportal");

                    if (processes.Length != 0)
                    {
                        if (processes.Length > 1)
                        {
                            Log.Write("MPTray: More than one process named \"MediaPortal\" has been found!");
                            foreach (Process procName in processes)
                            {
                                Log.Write("MPTray: {0} (Started: {1}, ID: {2})", procName.ProcessName, procName.StartTime.ToShortTimeString(), procName.Id);
                            }
                        }
                        Log.Write("MPTray: MediaPortal is already running - switching focus.");
                        SwitchFocus();
                    }
                    else
                    {
                        try
                        {
                            Uri uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);

                            Process process = new Process
                                                {
                                                    StartInfo =
                                                      {
                                                          FileName = "mediaportal.exe",
                                                          WorkingDirectory = Path.GetDirectoryName(uri.LocalPath),
                                                          UseShellExecute = true
                                                      }
                                                };

                            Log.Write("MPTray: starting MediaPortal");
                            process.Start();

                            using (EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset, "MediaPortalHandleCreated"))
                            {
                                if (handle.SafeWaitHandle.IsInvalid)
                                {
                                    return;
                                }

                                SwitchFocus();

                                handle.Set();
                                handle.Close();
                            }

                        }
                        catch (Exception ex)
                        {
                            Log.Write("MPTray: Error starting MediaPortal {0}", ex.Message);
                        }
                    }
                    break; // case RemoteButton.Start
            #endregion
            case RemoteButton.Power1:
            case RemoteButton.Power2:
            case RemoteButton.PowerTV:
            #region Power
                    
                    break; // case Remotebutton.Powerxxx
            #endregion
        } // switch
    }

    private static void OnDeviceArrival(object sender, EventArgs e)
    {
      Log.Write("MPTray: Device installed");
    }

    private static void OnDeviceRemoval(object sender, EventArgs e)
    {
      Log.Write("MPTray: Device removed");
    }

    private void DoMappedHandling(object sender, KeyEventArgs e)
    {
        Log.Write("MPTray: DoMappedHandling");

    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      Log.Write("MPTray: OnKeyDown");
     if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
      {
        _windowsKeyPressed = true;
      }

      if (_windowsKeyPressed && e.KeyCode == Keys.E)
      {
        Process[] processes = Process.GetProcessesByName("explorer");

        if (processes.Length == 0)
          Process.Start("explorer.exe");
      }

      if (_windowsKeyPressed && e.KeyCode == Keys.S)
      {
        OnClick(null, new RemoteEventArgs(RemoteButton.Start));
        e.Handled = true;
      }

      if (_windowsKeyPressed && e.KeyCode == Keys.K)
      {
        TerminateProcess("mediaportal");
        e.Handled = true;
      }

      if (_windowsKeyPressed && (e.KeyCode == Keys.T || e.KeyCode == Keys.K || e.KeyCode != Keys.M))
      {
        IntPtr handle = Win32API.FindWindow("Shell_TrayWnd", null);

        if (handle != IntPtr.Zero)
        {
          if (Win32API.IsWindowVisible(handle) == false)
          {
            Win32API.ShowWindow(handle, Win32API.ShowWindowFlags.ShowNotActivated);
          }
          if (Win32API.IsWindowEnabled(handle) == false)
          {
            Win32API.EnableWindow(handle, 1);
          }
        }

        e.Handled = e.KeyCode != Keys.M;
      }

      if (_windowsKeyPressed && e.KeyCode == Keys.C)
      {
        Uri uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);

        Process.Start(Path.GetDirectoryName(uri.LocalPath) + @"\configuration.exe");

        e.Handled = true;
      }

      if (e.KeyCode == Keys.Escape && (e.Modifiers & Keys.Control | Keys.Shift) == (Keys.Control | Keys.Shift))
        Process.Start("taskmgr.exe");

      if (e.KeyCode == Keys.Delete && (e.Modifiers & Keys.Control | Keys.Alt) == (Keys.Control | Keys.Alt))
        Process.Start("taskmgr.exe");

      if (!e.Handled)
      {
          DoMappedHandling(sender, e);
      }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
      Log.Write("MPTray: OnKeyUp");
      if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
        _windowsKeyPressed = false;
    }

    private static void Register(bool register, RegistryKey hive)
    {
      Log.Write("MPTray: Register");
      try
      {
        if (register)
        {
          RegistryKey key = hive.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
          if (key != null)
          {
            key.SetValue("MediaPortal Shell", Application.ExecutablePath);
          }
        }
        else
        {
          RegistryKey key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
          if (key != null)
          {
            key.DeleteValue("MediaPortal Shell");
          }
        }
      }
      catch (Exception e)
      {
        Log.Write("MPTray: Failed to modify autostart entry {0}", e.ToString());
      }
    }

    private void Run()
    {
      try
      {
        try
        {
          Thread.CurrentThread.Name = "MPTray";
        }
        catch (InvalidOperationException) { }

        Log.Open("MPTray.log");
        Log.Write("MPTray: Starting...");

        Config.Open();

        if (TerminateProcess("ehtray"))
        {
          Log.Write("MPTray: Terminating running instance(s) of ehtray.exe");
        }

        Remote.Click += OnClick;
        Device.DeviceArrival += OnDeviceArrival;
        Device.DeviceRemoval += OnDeviceRemoval;
        SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
        // reduce the memory footprint of the app
        Process process = Process.GetCurrentProcess();

        process.MaxWorkingSet = (IntPtr)900000;
        process.MinWorkingSet = (IntPtr)300000;

        InitTrayIcon();
        InitRecCheckThread();
        Application.Run();
      }
      catch (Exception e)
      {
        Log.Write("MPTray: Error on startup {0}", e.ToString());
      }

      if (_keyboardHook != null)
      {
        Log.Write("MPTray: Disabling keyboard hook");
        _keyboardHook.IsEnabled = false;
      }

      ev = false;

      Log.Write("MPTray: Exiting...");
    }

    void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Log.Write("MpTray: resume event");
            SwitchFocus();
        }
    }

    private static bool TerminateProcess(string processName)
    {
      Log.Write("MPTray: TerminateProcess");

      bool terminatedProcess = false;

      try
      {
        foreach (Process process in Process.GetProcessesByName(processName))
        {
          if (process != Process.GetCurrentProcess())
          {
            process.Kill();
            process.Close();

            if (terminatedProcess == false)
              terminatedProcess = true;
          }
        }
      }
      catch (Exception ex)
      {
        Log.Write("MPTray: Error while terminating process(es): {0}, {1}", processName, ex.ToString());
      }

      return terminatedProcess;
    }

    #endregion Methods

    #region Entry Point

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
      ShellApplication handler = new ShellApplication();

      foreach (string arg in args)
      {
        switch (arg.ToLower())
        {
          case "/shell":
          case "-shell":
            handler.InstallKeyboardHook();
            break;
          case "/noicon":
          case "-noicon":
            break;
          case "/kill":
          case "-kill":
            TerminateProcess("mptray");
            return;
          case "/register":
          case "-register":
          case "/register:user":
          case "-register:user":
            Register(true, Registry.CurrentUser);
            return;
          case "/register:all":
          case "-register:all":
            Register(true, Registry.LocalMachine);
            return;
          case "/unregister":
          case "-unregister":
          case "/unregister:user":
          case "-unregister:user":
            Register(false, Registry.CurrentUser);
            return;
          case "/unregister:all":
          case "-unregister:all":
            Register(false, Registry.LocalMachine);
            return;
          case "/unregister:both":
          case "-unregister:both":
            Register(false, Registry.CurrentUser);
            Register(false, Registry.LocalMachine);
            return;
          default:
            Log.Write("MPTray: Ignoring unknown command line parameter: '{0}'", arg);
            break;
        }
      }

      Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

      if (processes.Length != 1)
      {
        Log.Write("MPTray: Another instance of MPTray is already running");
        return;
      }

      handler.Run();
    }

    private void InitTrayIcon()
    {
      Log.Write("MPTray: InitTrayIcon");
      if (_systemNotificationAreaIcon == null)
      {
        try
        {
          _contextMenuTray = new ContextMenu();
          MenuItem menuItem1 = new MenuItem();

          // Initialize contextMenuTray
          _contextMenuTray.MenuItems.AddRange(new[] { menuItem1 });

          // Initialize menuItem1
          menuItem1.Index = 0;
          menuItem1.Text = "Close";
          menuItem1.Click += MenuItem1Click;

          _systemNotificationAreaIcon = new NotifyIcon
                                          {
                                            ContextMenu = _contextMenuTray,
                                            Icon = Properties.Resources.MPTrayIcon,
                                            Text = "MediaPortal Tray Launcher",
                                            Visible = true
                                          };
        }
        catch (Exception ex)
        {
          Log.Write("MPTray: Could not init tray icon - {0}", ex.ToString());
        }
      }
    }

    private void InitRecCheckThread()
    {
      ThreadStart s = new ThreadStart(RecCheckWorker);
      Thread t = new Thread(s);
      t.Start();
    }

    private void RecCheckWorker()
    {
      bool recState = _isRecording;

      while (ev == true)
      {
//        if (objTvServer != null) //&& a_TvBusinessLayer != null && a_TvDatabase != null && a_TvControl != null)
        {
          try
          {
              recState = _tvserver.IsAnyCardRecording();    

          }
          catch (Exception e)
          {
          }
        }


        if (_isRecording != recState)
        {
            _systemNotificationAreaIcon.Visible = false;
            _systemNotificationAreaIcon = null;

          if (recState == true)
            _systemNotificationAreaIcon = new NotifyIcon
            {
              ContextMenu = _contextMenuTray,
              Icon = Properties.Resources.MPTrayIconRec,
              Text = "MediaPortal TV server is recording",
              Visible = true
            };
          else
            _systemNotificationAreaIcon = new NotifyIcon
            {
              ContextMenu = _contextMenuTray,
              Icon = Properties.Resources.MPTrayIcon,
              Text = "MediaPortal Tray Launcher",
              Visible = true
            };
          _isRecording = recState;
        }
        Thread.Sleep(5000);
      }
    }

    private void MenuItem1Click(object sender, EventArgs e)
    {
      Log.Write("MPTray: MenuItem1Click");
      _systemNotificationAreaIcon.Visible = false;
      _systemNotificationAreaIcon = null;
      Application.Exit();
    }

    #endregion Entry Point

    #region Fields

    private KeyboardHook _keyboardHook;
    private bool _windowsKeyPressed;

    #endregion Fields

    #region Log
    public class Log
    {
        private static StreamWriter _streamWriter;

        public static void Open(string fileName)
        {
            if (_streamWriter != null)
            {
                return;
            }

            string filePath = Path.Combine(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Log), fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    string backup = Path.ChangeExtension(filePath, ".bak");

                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }
                    File.Move(filePath, backup);
                }
                catch { }
            }

            try
            {
                _streamWriter = new StreamWriter(filePath, false)
                {
                    AutoFlush = true
                };
                string message = String.Format("{0:yyyy-MM-dd HH:mm:ss.ffffff} - {1}: Log Opened", DateTime.Now, Thread.CurrentThread.Name);
                _streamWriter.WriteLine(message);

                message = String.Format("{0:yyyy-MM-dd HH:mm:ss.ffffff} - {1}: {2}", DateTime.Now, Thread.CurrentThread.Name, FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion);
                _streamWriter.WriteLine(message);
            }
            catch { }
        }

      public static void ReOpen(string fileName)
        {
        if (_streamWriter != null)
            {
                return;
            }

        string filePath = Path.Combine(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Log), fileName);
        

            try
            {
          _streamWriter = new StreamWriter(filePath, true)
          {
            AutoFlush = true
          };
            }
        catch { }
      }

      public static void Close()
      {
        if (_streamWriter == null)
        {
          return;
        }
        _streamWriter.Dispose();
        _streamWriter = null;
        
      }

        public static void Write(string format, params object[] args)
        {
        Log.ReOpen("MPTray.log");
            if (_streamWriter == null)
            {
                return;
            }
            string message = String.Format("{0:yyyy-MM-dd HH:mm:ss.ffffff} - ", DateTime.Now) + String.Format(format, args);

            _streamWriter.WriteLine(message);
        Log.Close();
        }
    }
    #endregion

    #region Config
    public class Config
    {
        public static System.Xml.XmlDocument _doc;

        public static void Open()
        {
            if (_doc != null)
            {
                return;
            }

            _doc = new System.Xml.XmlDocument();
            string filePath = Path.Combine(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config), "mptraymap.xml");
            if (!File.Exists(filePath))
            {
                try
                {
                    DefaultConfig(filePath);
                }
                catch { }
            }

            try
            {
                _doc.Load(filePath);
            }
            catch { }
        }

        public static void Close()
        {
            if (_doc == null)
            {
                return;
            }
            try
            {
            }
            finally
            {
//                _doc.Dispose();
                _doc = null;
            }
        }

        public static void Write(string format, params object[] args)
        {
            if (_doc == null)
            {
                return;
            }
        }

        private static void DefaultConfig(string filePath)
        {
            if (_doc == null)
                return;

            _doc.LoadXml("<MPTrayMap>" +
                "   <Apps>" +
                "       <Default>" +
                "       </Default>" +
                "   </Apps>" +
                "   <Remotes>" +
                "   </Remotes>" +
                "</MPTrayMap>");


            _doc.Save(filePath);
        }
    }
    #endregion
  }
}