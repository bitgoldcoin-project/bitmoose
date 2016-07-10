using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security;
using System.Text;

namespace BitMoose.Core
{
    public static class Utilities
    {
        /// <summary>
        /// Returns a secure string object from a non-secure string.
        /// </summary>
        public static SecureString ToSecureString(this string input)
        {
            if (input != null)
            {
                SecureString ss = new SecureString();
                for (int x = 0; x < input.Length; x++)
                {
                    ss.AppendChar(input[x]);
                }

                return ss;
            }

            return null;
        }

        public static bool IsNullOrWhiteSpace(this string input)
        {
            if (String.IsNullOrEmpty(input) == false && input.Trim().Length > 0)
            {
                return false;
            }
            
            return true;
        }

        public static string CleanString(string input)
        {
            if (input != null)
            {
                StringBuilder sb = new StringBuilder();
                for (int x = 0; x < input.Length; x++)
                {
                    if (Char.IsLetterOrDigit(input[x]) || Char.IsSeparator(input[x]) || Char.IsPunctuation(input[x]) || Char.IsWhiteSpace(input[x]))
                    {
                        sb.Append(input[x]);
                    }
                }
                return sb.ToString();
            }
            return input;
        }

        public static string GetFQDN()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();
            string fqdn = "";
            if (!hostName.Contains(domainName))
                fqdn = hostName + "." + domainName;
            else
                fqdn = hostName;

            return fqdn;
        }

        public static ProcessPriorityClass ToProcessPriority(int p_priority)
        {
            var _result = ProcessPriorityClass.Normal;

            if (p_priority < -1)
            {
                _result = ProcessPriorityClass.Idle;
            }
            else if (p_priority < 0)
            {
                _result = ProcessPriorityClass.BelowNormal;
            }
            else if (p_priority > 0)
            {
                _result = ProcessPriorityClass.AboveNormal;
            }
            else if (p_priority > 1)
            {
                _result = ProcessPriorityClass.High;
            }
            else if (p_priority > 2)
            {
                _result = ProcessPriorityClass.RealTime;
            }

            return _result;
        }

        /// <summary>
        /// Parses values by the given argument format
        /// </summary>
        public static string ParseArguments(string p_argFormat, string p_host, string p_userName, string p_password, short p_port, string p_additionalArgs)
        {
            string _args = p_argFormat;

            _args = _args.Replace("{host}", p_host);
            _args = _args.Replace("{username}", p_userName);
            _args = _args.Replace("{password}", p_password);
            _args = _args.Replace("{port}", p_port.ToString());

            if (_args.Contains("{combinedhost}"))
            {
                if (String.IsNullOrEmpty(p_host) == false && p_userName != null)
                {
                    string authBlock = p_userName;
                    if (p_password != null)
                    {
                        authBlock += ":" + p_password;
                    }
                    if (p_host.Contains("://"))
                    {
                        _args = _args.Replace("{combinedhost}", p_host.Insert(p_host.IndexOf("://"), authBlock + '@'));
                    }
                    else
                    {
                        _args = _args.Replace("{combinedhost}", "http://" + authBlock + '@' + p_host);
                    }
                }
                else
                {
                    _args = _args.Replace("{combinedhost}", p_host);
                }
            }
            
            return _args.Replace("{args}", p_additionalArgs);
        }

        /// <summary>
        /// Creates and starting a moose process
        /// </summary>
        public static Process CreateMiningProcess(string minerName, string path, string args, string winDomain, string winUserName, SecureString winPassword, bool loadUserProfile)
        {
            ProcessStartInfo _startInfo = new ProcessStartInfo(path, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(path)),
                Domain = winDomain,
                UserName = winUserName,
                Password = winPassword,
                LoadUserProfile = loadUserProfile
            };

            //psi.Verb = "runas";
            _startInfo.EnvironmentVariables.Add("BitMoose.MinerName", minerName);

            Process _process = new Process()
            {
                StartInfo = _startInfo
            };

            return _process;
        }

        public static void StartMiningProcess(Process p_processor, ProcessPriorityClass p_priority, long? p_cpuAffinity)
        {
            p_processor.Start();
            p_processor.PriorityClass = p_priority;
            if (p_cpuAffinity.HasValue)
                p_processor.ProcessorAffinity = new IntPtr(p_cpuAffinity.Value);
        }

        /// <summary>
        /// Converts a string representation of a range of cpus into a cpu affinity that can be used with an IntPtr
        /// </summary>
        public static long? ParseCPUAffinity(string cpus)
        {
            if (String.IsNullOrEmpty(cpus) == false)
            {
                if (cpus.Contains(','))
                { //parse a comma-seperated list of cpus, e.g. "1,2,4,6"
                    long affinity = 0;
                    string[] temp = cpus.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < temp.Length; x++)
                    {
                        int cpuVal = 0;
                        if (int.TryParse(temp[x], out cpuVal) && cpuVal > 0)
                        {
                            long tempaffinity = 1 << cpuVal - 1;
                            affinity |= tempaffinity;
                        }
                    }
                    if (affinity > 0)
                    {
                        return affinity;
                    }
                }
                else if (cpus.Contains('-'))
                { //parse a range, e.g. "3-4" or "1-8"
                    int cpuValLow = 0;
                    int cpuValHigh = 0;
                    long affinity = 0;
                    int dashIndex = cpus.IndexOf('-');
                    if (int.TryParse(cpus.Substring(0, dashIndex), out cpuValLow))
                    {
                        if (int.TryParse(cpus.Substring(dashIndex + 1, cpus.Length - dashIndex - 1), out cpuValHigh))
                        {
                            for (int x = cpuValLow; x <= cpuValHigh; x++)
                            {
                                affinity = affinity | (uint)(1 << x - 1);
                            }
                            return affinity;
                        }
                    }
                }
                else
                { //parse a single cpu value, e.g. "5"
                    int cpuVal = 0;
                    if (int.TryParse(cpus, out cpuVal) && cpuVal > 0)
                    {
                        long affinity = 1 << cpuVal - 1;
                        return affinity;
                    }
                }
            }

            return null;
        }
    }
}