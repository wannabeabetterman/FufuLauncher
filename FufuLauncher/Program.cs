using System;
using System.Linq;
using System.Runtime.InteropServices;
using FufuLauncher.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace FufuLauncher
{
    public static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--elevated-inject", StringComparison.OrdinalIgnoreCase))
            {
                RunElevatedInjection(args);
                return;
            }

            var key = "FufuLauncher_Main_Instance_Key";
            var mainInstance = AppInstance.FindOrRegisterForKey(key);

            if (!mainInstance.IsCurrent)
            {
                var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                var task = mainInstance.RedirectActivationToAsync(activationArgs).AsTask();
                task.Wait();
                return;
            }

            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        private static void RunElevatedInjection(string[] args)
        {
            // args: --elevated-inject <gameExePath> <dllPath> <configMask> <commandLineArgs...>
            int exitCode = 1;
            try
            {
                if (args.Length < 4)
                {
                    return;
                }

                string gameExePath = args[1];
                string dllPath = args[2];
                
                if (!int.TryParse(args[3], out int configMask))
                {
                    MessageBox(IntPtr.Zero, "配置参数格式错误", "FufuLauncher 错误", 0x10);
                    return;
                }

                string commandLineArgs = args.Length > 4 ? string.Join(' ', args.Skip(4)) : string.Empty;

                var launcher = new LauncherService();

                launcher.UpdateConfig(gameExePath,
                    (configMask & (1 << 0)) != 0, // hideQuestBanner
                    (configMask & (1 << 1)) != 0, // disableDamageText
                    (configMask & (1 << 2)) != 0, // useTouchScreen
                    (configMask & (1 << 3)) != 0, // disableEventCameraMove
                    (configMask & (1 << 4)) != 0, // removeTeamProgress
                    (configMask & (1 << 5)) != 0, // redirectCombineEntry
                    (configMask & (1 << 6)) != 0, // resin106
                    (configMask & (1 << 7)) != 0, // resin201
                    (configMask & (1 << 8)) != 0, // resin107009
                    (configMask & (1 << 9)) != 0, // resin107012
                    (configMask & (1 << 10)) != 0 // resin220007
                );

                var result = launcher.LaunchGameAndInject(gameExePath, dllPath, commandLineArgs, out var errorMessage, out var pid);

                if (result != 0)
                {
                    MessageBox(IntPtr.Zero, $"注入启动失败: {errorMessage} (代码: {result})", "FufuLauncher 错误", 0x10); // MB_ICONERROR
                }

                exitCode = result == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                MessageBox(IntPtr.Zero, $"注入进程发生异常: {ex.Message}", "FufuLauncher 错误", 0x10);
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }
    }
}