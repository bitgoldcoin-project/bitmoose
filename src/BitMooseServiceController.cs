using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace BitMoose.Core
{
    public static class BitMooseServiceController
    {
        public static bool TryGetStatus(out ServiceControllerStatus status)
        {
            status = ServiceControllerStatus.Stopped;
            try
            {
                ServiceController bmservice = new ServiceController("bitmoose");
                status = bmservice.Status;
                return true;
            }
            catch (InvalidOperationException)
            {
            }
            return false;
        }

        public static void Start()
        {
            ServiceController bmservice = new ServiceController("bitmoose");
            if (bmservice.Status == ServiceControllerStatus.Stopped)
            {
                bmservice.Start();
            }
            else if (bmservice.Status == ServiceControllerStatus.Paused)
            {
                bmservice.Continue();
            }
        }

        public static void Stop()
        {
            ServiceController bmservice = new ServiceController("bitmoose");
            if (bmservice.Status == ServiceControllerStatus.Running || bmservice.Status == ServiceControllerStatus.Paused)
            {
                bmservice.Stop();
            }
        }

        public static void Restart()
        {
            Stop();
            Start();
        }

        public static void Install(string appPath)
        {
            string _args = String.Format("create \"bitmoose\" start= auto binPath= \"{0}\" DisplayName= \"Bit Moose Mining\"", Path.Combine(appPath, "bitmoose-svc.exe"));

            ProcessStartInfo psi = new ProcessStartInfo("sc", _args)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            Process proc = Process.Start(psi);
        }

        public static void Uninstall()
        {
            ProcessStartInfo psi = new ProcessStartInfo("sc", "delete \"bitmoose\"")
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            Process proc = Process.Start(psi);
        }
    }
}