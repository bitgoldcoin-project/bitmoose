using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BitMoose.Core.Communication;
using BitMoose.Core.Settings;

namespace BitMoose.Core
{
    public enum MiningProcessorState
    {
        Stopped = 0,
        Started = 1
    }

    public class MiningProcessor : IDisposable
    {
        private BitMooseSettings __MooseSettings = null;
        private Dictionary<string, Process> __MinerProcesses = new Dictionary<string, Process>();
        private Dictionary<string, FileStream> __MinerLogStreams = new Dictionary<string, FileStream>();
        private Server __CommServer = null;
        private MiningProcessorState __ProcState = MiningProcessorState.Stopped;

        public event DataReceivedEventHandler OutputDataReceivedEvent = null;
        public event DataReceivedEventHandler ErrorDataReceivedEvent = null;

        #region " Properties "

        public MiningProcessorState State
        {
            get
            {
                return __ProcState;
            }
            protected set
            {
                __ProcState = value;
            }
        }

        #endregion

        #region " Methods "

        /// <summary>
        /// Starts mining activity
        /// </summary>
        public void Start(string p_localDirectory)
        {
            try
            {
                LoadConfiguration(p_localDirectory);

                StartLogging();
                Trace.TraceInformation("Logging started.");

                StartCommunications(__MooseSettings.DebugLogging);
                Trace.TraceInformation("Communications started.");

                StartMining();
                Trace.TraceInformation("Mining started.");

                __ProcState = MiningProcessorState.Started;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                //throw;
            }
        }

        /// <summary>
        /// Starts GeForce mining activity
        /// </summary>
        public void GeForceStart(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            try
            {
                GeForceConfiguration(p_localDirectory, p_debugLog, p_priority, p_defUserName, p_defPassword, p_defMinerOpt, p_defPool, p_defHost, p_defArgs, p_defPort);

                StartLogging();
                Trace.TraceInformation("Logging started.");

                StartCommunications(__MooseSettings.DebugLogging);
                Trace.TraceInformation("Communications started.");

                StartMining();
                Trace.TraceInformation("Mining started.");

                __ProcState = MiningProcessorState.Started;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                //throw;
            }
        }

        /// <summary>
        /// Starts CPU-Quark mining activity
        /// </summary>
        public void QuarkStart(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            try
            {
                QuarkConfiguration(p_localDirectory, p_debugLog, p_priority, p_defUserName, p_defPassword, p_defMinerOpt, p_defPool, p_defHost, p_defArgs, p_defPort);

                StartLogging();
                Trace.TraceInformation("Logging started.");

                StartCommunications(__MooseSettings.DebugLogging);
                Trace.TraceInformation("Communications started.");

                StartMining();
                Trace.TraceInformation("Mining started.");

                __ProcState = MiningProcessorState.Started;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                //throw;
            }
        }

        /// <summary>
        /// Stops all mining and logging activity
        /// </summary>
        public void Stop()
        {
            StopMining();
            Trace.TraceInformation("Mining stopped.");
            
            StopCommunications();
            Trace.TraceInformation("Communications stopped.");
            
            StopLogging();
            __ProcState = MiningProcessorState.Stopped;
        }

        /// <summary>
        /// Loads the configuration from the xml file into the processor
        /// </summary>
        private void LoadConfiguration(string p_localDirectory)
        {
            var _filename = Path.Combine(p_localDirectory, BitMooseSettings.SettingsFileName);
            this.__MooseSettings = new BitMooseSettings();
            this.__MooseSettings.Load(_filename);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_localDirectory"></param>
        /// <param name="p_debugLog"></param>
        /// <param name="p_priority"></param>
        /// <param name="p_defUserName"></param>
        /// <param name="p_defPassword"></param>
        /// <param name="p_defMinerOpt"></param>
        /// <param name="p_defPool"></param>
        /// <param name="p_defHost"></param>
        /// <param name="p_defArgs"></param>
        /// <param name="p_defPort"></param>
        private void GeForceConfiguration(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            __MooseSettings = new BitMooseSettings();
            __MooseSettings.LoadGeForce(p_localDirectory, p_debugLog, p_priority, p_defUserName, p_defPassword, p_defMinerOpt, p_defPool, p_defHost, p_defArgs, p_defPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_localDirectory"></param>
        /// <param name="p_debugLog"></param>
        /// <param name="p_priority"></param>
        /// <param name="p_defUserName"></param>
        /// <param name="p_defPassword"></param>
        /// <param name="p_defMinerOpt"></param>
        /// <param name="p_defPool"></param>
        /// <param name="p_defHost"></param>
        /// <param name="p_defArgs"></param>
        /// <param name="p_defPort"></param>
        private void QuarkConfiguration(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            __MooseSettings = new BitMooseSettings();
            __MooseSettings.LoadQuark(p_localDirectory, p_debugLog, p_priority, p_defUserName, p_defPassword, p_defMinerOpt, p_defPool, p_defHost, p_defArgs, p_defPort);
        }
        
        #region " Communications "

        private void StartCommunications(bool p_debugLog)
        {
            if (p_debugLog == true)
            {
                __CommServer = new Server();
                __CommServer.Start();
            }
        }

        private void StopCommunications()
        {
            if (__CommServer != null)
            {
                __CommServer.Stop();
                __CommServer = null;
            }
        }

        private void WriteToCommunications(Process sender, string data)
        {
            if (sender != null && data != null && sender.StartInfo.EnvironmentVariables.ContainsKey("BitMoose.MinerName"))
            {
                if (__CommServer != null)
                {
                    string name = sender.StartInfo.EnvironmentVariables["BitMoose.MinerName"];
                    if (name != null && __MooseSettings.Miners.ContainsKey(name) && __MooseSettings.Miners[name].StatusUpdates)
                    {
                        __CommServer.Write(String.Format("{0}: {1}\0\0", name, data.Trim()));
                    }
                }
            }
        }

        #endregion

        #region " Mining "

        /// <summary>
        /// Starts all mining processes based on the loaded settings.
        /// </summary>
        private void StartMining()
        {
            foreach (var _miner in __MooseSettings.Miners)
            {
                MinerOption _option = __MooseSettings.MinerOptions[_miner.Value.OptionKey];
                MiningPool _pool = __MooseSettings.Pools.Where(a => a.Name == _miner.Value.Pool).FirstOrDefault();

                string _host = _miner.Value.Host;
                if (_pool != null && String.IsNullOrEmpty(_host))
                    _host = _pool.Host;

                short _port = 8332;
                if (_miner.Value.Port.HasValue)
                    _port = _miner.Value.Port.Value;
                else if (_pool != null)
                    _port = _pool.Port;

                string _args = Utilities.ParseArguments
                                        (
                                            _option.ArgumentFormat, _host, _miner.Value.UserName, 
                                            _miner.Value.Password, _port, _miner.Value.Arguments
                                        );

                Process _proc = Utilities.CreateMiningProcess
                                        (
                                            _miner.Key, _option.Path, _args, _miner.Value.Process.WinDomain,
                                            _miner.Value.Process.WinUserName, _miner.Value.Process.WinPassword.ToSecureString(),
                                            _miner.Value.Process.LoadUserProfile
                                        );

                _proc.StartInfo.RedirectStandardOutput = true;
                _proc.StartInfo.RedirectStandardError = true;
                _proc.StartInfo.RedirectStandardInput = true;

                _proc.EnableRaisingEvents = true;

                _proc.OutputDataReceived += new DataReceivedEventHandler(MinerProcess_OutputDataReceived);
                _proc.ErrorDataReceived += new DataReceivedEventHandler(MinerProcess_ErrorDataReceived);

                Utilities.StartMiningProcess(_proc, Utilities.ToProcessPriority(_miner.Value.Process.Priority), Utilities.ParseCPUAffinity(_miner.Value.Process.CPUAffinity));

                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();

                __MinerProcesses.Add(_miner.Key, _proc);
            }
        }

        private void MinerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (__MooseSettings.DebugLogging)
            {
                if (e.Data.IsNullOrWhiteSpace() == false)
                {
                    string _message = Utilities.CleanString(e.Data);
                    Trace.TraceInformation(_message);

                    WriteToLog(sender as Process, _message);
                    WriteToCommunications(sender as Process, _message);
                }
            }

            if (OutputDataReceivedEvent != null)
                OutputDataReceivedEvent(sender, e);
        }

        private void MinerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (__MooseSettings.DebugLogging)
            {
                if (e.Data.IsNullOrWhiteSpace() == false)
                {
                    string _message = Utilities.CleanString(e.Data);
                    Trace.TraceError(_message);

                    WriteToLog(sender as Process, _message);
                    WriteToCommunications(sender as Process, _message);
                }
            }

            if (ErrorDataReceivedEvent != null)
                ErrorDataReceivedEvent(sender, e);
        }

        /// <summary>
        /// Stops all mining processes by sending a close signal, then after 2.5s forcibly killing the process if it hasn't yet closed.
        /// </summary>
        private void StopMining()
        {
            foreach (var _p in __MinerProcesses)
            {
                if (_p.Value != null && _p.Value.HasExited == false)
                {
                    _p.Value.CancelErrorRead();
                    _p.Value.CancelOutputRead();
                    _p.Value.StandardInput.Write("\x3");
                    _p.Value.StandardInput.Flush();
                    _p.Value.StandardInput.Close();
                    _p.Value.CloseMainWindow();
 
                    Trace.TraceInformation(String.Format("Moose '{0}' process sent close command.", _p.Key));
                }
            }
            
            //begin 2.5s worth of spin check to ensure the processes have exited.
            for (int x = 0; x < 8; x++)
            {
                bool _alive = false;
                foreach (var _p in __MinerProcesses)
                {
                    try
                    {
                        if (_p.Value != null && _p.Value.Responding && _p.Value.HasExited == false)
                        {
                            _alive = true;
                            break;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                if (_alive)
                {
                    Thread.Sleep(500);
                }
            }
            
            //kill any process that may still be alive
            foreach (var _p in __MinerProcesses)
            {
                try
                {
                    if (_p.Value != null && _p.Value.HasExited == false)
                    {
                        Trace.TraceWarning(String.Format("Moose '{0}' process was killed forcibly (didn't terminate after 2.5 seconds).", _p.Key));
                        _p.Value.Kill();
                        _p.Value.Close();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
            
            __MinerProcesses.Clear();
        }

        #endregion

        #region " Logging "

        /// <summary>
        /// Starts application logging
        /// </summary>
        private void StartLogging()
        {
            if (__MooseSettings.DebugLogging)
            {
                TextWriterTraceListener _textLog = new TextWriterTraceListener(BitMooseSettings.LogFileName, "BitMoose")
                {
                    TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId,
                    Filter = new EventTypeFilter(SourceLevels.Verbose)
                };
                Trace.Listeners.Add(_textLog);
                Trace.AutoFlush = true;
            }
            
            foreach (var _miner in __MooseSettings.Miners)
            {
                if (_miner.Value.Logging.Enabled)
                {
                    string _logpath = Path.Combine(__MooseSettings.BitMoosePath, "BitMoose_" + _miner.Key + ".log");
                    
                    FileStream _fs = new FileStream(_logpath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1024);
                    _fs.Seek(0, SeekOrigin.End);

                    __MinerLogStreams.Add(_miner.Key, _fs);
                }
            }
        }

        /// <summary>
        /// Stops application logging
        /// </summary>
        private void StopLogging()
        {
            Trace.Listeners.Clear();

            foreach (var log in __MinerLogStreams)
            {
                log.Value.Close();
            }

            __MinerLogStreams.Clear();
        }

        /// <summary>
        /// Writes the data for the given process to the log file.
        /// </summary>
        private void WriteToLog(Process sender, string data)
        {
            if (sender != null && data != null && sender.StartInfo.EnvironmentVariables.ContainsKey("BitMoose.MinerName"))
            {
                string _name = sender.StartInfo.EnvironmentVariables["BitMoose.MinerName"];
                if (_name != null && __MinerLogStreams.ContainsKey(_name) && __MooseSettings.Miners.ContainsKey(_name))
                {
                    FileStream _fs = __MinerLogStreams[_name];
                    if (__MooseSettings.Miners[_name].Logging.Enabled)
                    {
                        lock (_fs)
                        {
                            if (_fs.Length > __MooseSettings.Miners[_name].Logging.MaxFileSize)
                            {
                                _fs.SetLength(0);
                            }
             
                            byte[] _text = Encoding.UTF8.GetBytes(DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt: ") + data + Environment.NewLine);
                            _fs.Write(_text, 0, _text.Length);
                        }
                    }
                    else
                    {
                        _fs.Close();
                        __MinerLogStreams.Remove(_name);
                    }
                }
            }
        }

        #endregion

        #endregion
 
        public void Dispose()
        {
            if (__CommServer != null)
            {
                __CommServer.Dispose();
                __CommServer = null;
            }
        }
    }
}