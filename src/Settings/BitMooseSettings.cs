using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BitMoose.Core.Settings
{
    public class BitMooseSettings
    {
        public const string SettingsFileName = "Settings.xml";
        public const string LogFileName = "BitMoose_Trace.log";

        private const string DefaultUserNameValue = "";
        private const string DefaultPasswordValue = "";

        #region " Properties "

        public string BitMoosePath
        {
            get;
            protected set;
        }

        public bool DebugLogging
        {
            get;
            set;
        }

        public string DefaultUserName
        {
            get;
            set;
        }

        public string DefaultPassword
        {
            get;
            set;
        }

        public string DefaultMinerOption
        {
            get;
            set;
        }

        public string DefaultPool
        {
            get;
            set;
        }

        public Dictionary<string, MinerOption> MinerOptions
        {
            get;
            protected set;
        }

        public Dictionary<string, MinerSetting> Miners
        {
            get;
            protected set;
        }

        public MiningPool[] Pools
        {
            get;
            set;
        }

        #endregion

        #region " Constructor(s) "

        public BitMooseSettings()
        {
            this.MinerOptions = new Dictionary<string, MinerOption>();
            this.Miners = new Dictionary<string, MinerSetting>();
        }

        #endregion

        #region " Methods "

        #region " Loading "

        /// <summary>
        /// Loads the settings from the XML file.
        /// </summary>
        public void Load(string p_config_file_name)
        {
            string _localDirectory = Path.GetDirectoryName(p_config_file_name);
            if (Directory.Exists(_localDirectory))
            {
                if (File.Exists(p_config_file_name))
                {
                    Dictionary<string, MinerOption> _options = new Dictionary<string, MinerOption>();
                    Dictionary<string, MinerSetting> _miners = new Dictionary<string, MinerSetting>();

                    XDocument _xdoc = null;
                    try
                    {
                        _xdoc = XDocument.Load(p_config_file_name);
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Failed to load settings from XML file. " + ex.Message, ex);
                    }

                    //load the defaults
                    bool debugLog = false;
                    if (_xdoc.Root.Element("DebugLogging") != null)
                    {
                        debugLog = Convert.ToBoolean(_xdoc.Root.Element("DebugLogging").Value);
                    }

                    string defUserName = (from n in _xdoc.Root.Descendants("DefaultUserName")
                                          select n.Value).FirstOrDefault();
                    string defPassword = (from n in _xdoc.Root.Descendants("DefaultPassword")
                                          select n.Value).FirstOrDefault();
                    string defMinerOpt = (from n in _xdoc.Root.Descendants("DefaultMinerOption")
                                          select n.Value).FirstOrDefault();
                    string defPool = (from n in _xdoc.Root.Descendants("DefaultPool")
                                      select n.Value).FirstOrDefault();

                    //load the moose options
                    foreach (var element in _xdoc.Root.Element("MinerOptions").Descendants("MinerOption"))
                    {
                        string name = null;
                        MinerOption opt = LoadMinerOption(element, out name);
                        if (_options.ContainsKey(name))
                        {
                            throw new ApplicationException(String.Format("Duplicate moose option name '{0}' detected. moose names must be unique.", name));
                        }
                        _options.Add(name, opt);
                    }

                    //load the miners
                    int minerIndex = 1;
                    foreach (var element in _xdoc.Root.Element("Miners").Descendants("Miner"))
                    {
                        string name = null;
                        MinerSetting m = LoadMiner(element, out name);
                        name = name ?? String.Format("Miner #{0}", minerIndex);
                        m.UserName = m.UserName ?? defUserName;
                        m.Password = m.Password ?? defPassword;
                        if (_miners.ContainsKey(name))
                        {
                            throw new ApplicationException(String.Format("Duplicate moose name '{0}' detected. moose names must be unique.", name));
                        }
                        _miners.Add(name, m);
                        minerIndex++;
                    }

                    //load the pools
                    List<MiningPool> pools = new List<MiningPool>();
                    foreach (var element in _xdoc.Root.Element("Pools").Descendants("Pool"))
                    {
                        MiningPool pool = new MiningPool()
                        {
                            Name = element.Attribute("name").Value,
                            Host = element.Attribute("host").Value,
                            Port = Convert.ToInt16(element.Attribute("port").Value)
                        };
                        if (element.Attribute("website") != null)
                        {
                            pool.Website = element.Attribute("website").Value;
                        }
                        pools.Add(pool);
                    }

                    //validate
                    if (String.IsNullOrEmpty(defMinerOpt))
                    {
                        throw new ApplicationException("The default moose option is not specified in the settings file. It must be named as the value for the 'DefaultMinerOption' element.");
                    }

                    foreach (var option in _options)
                    {
                        if (File.Exists(option.Value.Path) == false)
                        {
                            if (File.Exists(Path.Combine(_localDirectory, option.Value.Path)) == false)
                            {
                                throw new ApplicationException(String.Format("Invalid path for moose option '{0}'. The file was not found or is inaccessible.", option.Key));
                            }
                            else
                            {
                                option.Value.Path = Path.Combine(_localDirectory, option.Value.Path); //update path to use combined.
                            }
                        }
                        if (String.IsNullOrEmpty(option.Key))
                        {
                            throw new ApplicationException("Invalid name on moose option. All moose options must have a name attribute specified with a unique value.");
                        }
                    }

                    foreach (var _miner in _miners)
                    {
                        if (_options.ContainsKey(_miner.Value.OptionKey) == false)
                        {
                            throw new ApplicationException(String.Format("Invalid option value '{1}' on moose '{0}'. The option value must be a name from the available moose options.", _miner.Key, _miner.Value.OptionKey));
                        }
                        if (String.IsNullOrEmpty(_miner.Key))
                        {
                            throw new ApplicationException("Invalid name on moose. All miners must have a name attribute specified with a unique value.");
                        }
                    }

                    if (String.IsNullOrEmpty(defPool) == false && pools.Any(a => a.Name == defPool) == false)
                    {
                        throw new ApplicationException("The specified default pool does not exist.");
                    }

                    //set values
                    this.BitMoosePath = _localDirectory;
                    this.DebugLogging = debugLog;
                    this.DefaultUserName = (defUserName ?? DefaultUserNameValue).Trim();
                    this.DefaultPassword = (defPassword ?? DefaultPasswordValue).Trim();
                    this.DefaultMinerOption = defMinerOpt;
                    this.DefaultPool = defPool;
                    this.MinerOptions = _options;
                    this.Miners = _miners;
                    this.Pools = pools.ToArray();
                }
                else
                {
                    throw new FileNotFoundException(String.Format("The settings file '{0}' was not found or is inaccessible.", p_config_file_name), p_config_file_name);
                }
            }
            else
            {
                throw new DirectoryNotFoundException(String.Format("The directory path '{0}' was not found or is inaccessible.", _localDirectory));
            }
        }

        /// <summary>
        /// Loads the moose option from the settings XML into an represenative object
        /// </summary>
        private MinerOption LoadMinerOption(XElement optionElement, out string name)
        {
            MinerOption _option = new MinerOption()
            {
                Path = optionElement.Attribute("path").Value,
                ArgumentFormat = optionElement.Attribute("argumentformat").Value
            };

            if (optionElement.Attribute("arguments") != null)
            {
                _option.Arguments = optionElement.Attribute("arguments").Value;
            }

            name = optionElement.Attribute("name").Value;
            return _option;
        }

        /// <summary>
        /// Loads the moose from the settings XML into an represenative object
        /// </summary>
        private MinerSetting LoadMiner(XElement p_xminer, out string p_oname)
        {
            MinerSetting _ms = new MinerSetting();
            p_oname = p_xminer.Attribute("name").Value;

            _ms.OptionKey = p_xminer.Attribute("option").Value;
            if (p_xminer.Attribute("arguments") != null)
            {
                _ms.Arguments = p_xminer.Attribute("arguments").Value;
            }
            if (p_xminer.Attribute("pool") != null)
            {
                _ms.Pool = p_xminer.Attribute("pool").Value;
            }
            if (p_xminer.Attribute("host") != null)
            {
                _ms.Host = p_xminer.Attribute("host").Value;
            }
            if (p_xminer.Attribute("port") != null)
            {
                short port = 8332;
                if (short.TryParse(p_xminer.Attribute("port").Value, out port))
                {
                    _ms.Port = port;
                }
            }
            if (p_xminer.Attribute("username") != null)
            {
                _ms.UserName = p_xminer.Attribute("username").Value;
            }
            if (p_xminer.Attribute("password") != null)
            {
                _ms.Password = p_xminer.Attribute("password").Value;
            }
            if (p_xminer.Attribute("enabled") != null)
            {
                _ms.Enabled = Convert.ToBoolean(p_xminer.Attribute("enabled").Value);
            }
            if (p_xminer.Attribute("netupdate") != null)
            {
                _ms.StatusUpdates = Convert.ToBoolean(p_xminer.Attribute("netupdate").Value);
            }

            //load logging
            _ms.Logging = new MinerLogSetting();
            XElement logElement = p_xminer.Element("Log");
            if (logElement != null)
            {
                if (logElement.Attribute("enabled") != null)
                {
                    _ms.Logging.Enabled = Convert.ToBoolean(logElement.Attribute("enabled").Value);
                }
                if (logElement.Attribute("maxfilesize") != null)
                {
                    _ms.Logging.MaxFileSize = Convert.ToInt32(logElement.Attribute("maxfilesize").Value);
                }
            }

            //load crash recovery
            _ms.CrashRecovery = new MinerCrashRecoverySetting();
            XElement recoveryElement = p_xminer.Element("CrashRecovery");
            if (recoveryElement != null)
            {
                if (recoveryElement.Attribute("retries") != null)
                {
                    _ms.CrashRecovery.Retries = Convert.ToInt32(recoveryElement.Attribute("retries").Value);
                }
                if (recoveryElement.Attribute("interval") != null)
                {
                    _ms.CrashRecovery.Interval = Convert.ToInt32(recoveryElement.Attribute("interval").Value);
                }
                if (recoveryElement.Attribute("altminer") != null)
                {
                    _ms.CrashRecovery.AlternateMiner = recoveryElement.Attribute("altminer").Value;
                }
            }

            //load process
            _ms.Process = new MinerProcessSetting();
            XElement procElement = p_xminer.Element("Process");
            if (procElement != null)
            {
                if (procElement.Attribute("priority") != null)
                {
                    _ms.Process.Priority = Convert.ToInt16(procElement.Attribute("priority").Value);
                }
                if (procElement.Attribute("loadprofile") != null)
                {
                    _ms.Process.LoadUserProfile = Convert.ToBoolean(procElement.Attribute("loadprofile").Value);
                }
                if (procElement.Attribute("windomain") != null)
                {
                    _ms.Process.WinDomain = procElement.Attribute("windomain").Value;
                }
                if (procElement.Attribute("winusername") != null)
                {
                    _ms.Process.WinUserName = procElement.Attribute("winusername").Value;
                }
                if (procElement.Attribute("winpassword") != null)
                {
                    _ms.Process.WinPassword = procElement.Attribute("winpassword").Value;
                }
                if (procElement.Attribute("cpuaffinity") != null)
                {
                    _ms.Process.CPUAffinity = (procElement.Attribute("cpuaffinity").Value ?? String.Empty).Trim();
                }
            }
            return _ms;
        }

        /// <summary>
        /// Loads the settings about GeForce Vga_Card.
        /// </summary>
        public void LoadGeForce(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            //load the moose options
            Dictionary<string, MinerOption> _options = new Dictionary<string, MinerOption>();
            {
                MinerOption _option = new MinerOption()
                {
                    ArgumentFormat = " --url={host}:{port} --userpass={username}:{password} {args}",
                    Arguments = p_defArgs,
                    Path = @"packages\cuda\cuda.exe"
                };
                _options.Add("cuda", _option);
            }

            //load the miners
            Dictionary<string, MinerSetting> _miners = new Dictionary<string, MinerSetting>();
            {
                MinerSetting _miner = new MinerSetting()
                {
                    Pool = p_defPool,
                    OptionKey = p_defMinerOpt,
                    Arguments = p_defArgs, 
                    Host = p_defHost,
                    Port = (short)p_defPort,
                    UserName = p_defUserName,
                    Password = p_defPassword,
                    Enabled = true,

                    CrashRecovery = new MinerCrashRecoverySetting()
                    {
                        AlternateMiner = "",
                        Interval = 60,
                        Retries = 3
                    },

                    Logging = new MinerLogSetting()
                    {
                        Enabled = false,
                        MaxFileSize = 1048576
                    },

                    Process = new MinerProcessSetting()
                    {
                        Priority = (short)p_priority,
                        CPUAffinity = "",
                        LoadUserProfile = false,
                        WinDomain = "",
                        WinUserName = "",
                        WinPassword = ""
                    },

                    StatusUpdates = false
                };

                _miners.Add("Default", _miner);
            }

            //load the pools
            List<MiningPool> _pools = new List<MiningPool>();
            {
                MiningPool _pool = new MiningPool()
                    {
                        Name = "Give-Me-Coins",
                        Host = "ftc.give-me-coins.com",
                        Port = 3336,
                        Website = "http://www.give-me-coins.com"
                    };
                _pools.Add(_pool);
            }

            //set values
            this.BitMoosePath = p_localDirectory;
            this.DebugLogging = p_debugLog;
            this.DefaultUserName = (p_defUserName ?? DefaultUserNameValue).Trim();
            this.DefaultPassword = (p_defPassword ?? DefaultPasswordValue).Trim();
            this.DefaultMinerOption = p_defMinerOpt;
            this.DefaultPool = p_defPool;
            this.MinerOptions = _options;
            this.Miners = _miners;
            this.Pools = _pools.ToArray();
        }

        /// <summary>
        /// Loads the settings about cpu-miner quark.
        /// </summary>
        public void LoadQuark(string p_localDirectory, bool p_debugLog, int p_priority, string p_defUserName, string p_defPassword, string p_defMinerOpt, string p_defPool, string p_defHost, string p_defArgs, int p_defPort)
        {
            //load the moose options
            Dictionary<string, MinerOption> _options = new Dictionary<string, MinerOption>();
            {
                MinerOption _option = new MinerOption()
                {
                    ArgumentFormat = " -a quark -q --url {host}:{port} -u {username} -p {password} {args}",
                    Arguments = p_defArgs,
                    Path = @"packages\qrk\quark.exe"
                };
                _options.Add("quark", _option);
            }

            //load the miners
            Dictionary<string, MinerSetting> _miners = new Dictionary<string, MinerSetting>();
            {
                MinerSetting _miner = new MinerSetting()
                {
                    Pool = p_defPool,
                    OptionKey = p_defMinerOpt,
                    Arguments = p_defArgs,
                    Host = p_defHost,
                    Port = (short)p_defPort,
                    UserName = p_defUserName,
                    Password = p_defPassword,
                    Enabled = true,

                    CrashRecovery = new MinerCrashRecoverySetting()
                    {
                        AlternateMiner = "",
                        Interval = 60,
                        Retries = 3
                    },

                    Logging = new MinerLogSetting()
                    {
                        Enabled = false,
                        MaxFileSize = 1048576
                    },

                    Process = new MinerProcessSetting()
                    {
                        Priority = (short)p_priority,
                        CPUAffinity = "",
                        LoadUserProfile = false,
                        WinDomain = "",
                        WinUserName = "",
                        WinPassword = ""
                    },

                    StatusUpdates = false
                };

                _miners.Add("Default", _miner);
            }

            //load the pools
            List<MiningPool> _pools = new List<MiningPool>();
            {
                MiningPool _pool = new MiningPool()
                {
                    Name = "coinmine",
                    Host = "qrk.coinmine.pl",
                    Port = 6010,
                    Website = "http://www2.coinmine.pl/qrk/"
                };
                _pools.Add(_pool);
            }

            //set values
            this.BitMoosePath = p_localDirectory;
            this.DebugLogging = p_debugLog;
            this.DefaultUserName = (p_defUserName ?? DefaultUserNameValue).Trim();
            this.DefaultPassword = (p_defPassword ?? DefaultPasswordValue).Trim();
            this.DefaultMinerOption = p_defMinerOpt;
            this.DefaultPool = p_defPool;
            this.MinerOptions = _options;
            this.Miners = _miners;
            this.Pools = _pools.ToArray();
        }

        #endregion

        #region " Saving "

        /// <summary>
        /// Saves all current settings to the XML file.
        /// </summary>
        public void Save(string p_fileName)
        {
            FileInfo _fi = new FileInfo(p_fileName);
            if (_fi.Directory.Exists == false)
                Directory.CreateDirectory(_fi.DirectoryName);

            XDocument _xdoc = new XDocument();
            {
                XElement _root = new XElement("BitMoose");
                {
                    _root.Add(new XElement("DebugLogging", this.DebugLogging));
                    _root.Add(new XElement("DefaultUserName", this.DefaultUserName));
                    _root.Add(new XElement("DefaultPassword", this.DefaultPassword));
                    _root.Add(new XElement("DefaultMinerOption", this.DefaultMinerOption));
                    _root.Add(new XElement("DefaultPool", this.DefaultPool));

                    SaveMiners(_root);
                    SaveMinerOptions(_root);
                    SaveMinerPools(_root);
                }

                _xdoc.Add(_root);
                _xdoc.Save(p_fileName);
            }
        }

        private void SaveMiners(XElement p_root)
        {
            XElement _miners = new XElement("Miners");
            foreach (var _miner in this.Miners)
            {
                XElement _xminer = new XElement("Miner");
                {
                    AddAttribute(_xminer, "name", _miner.Key);
                    AddAttribute(_xminer, "enabled", _miner.Value.Enabled);
                    AddAttribute(_xminer, "pool", _miner.Value.Pool, true);
                    AddAttribute(_xminer, "option", _miner.Value.OptionKey);
                    AddAttribute(_xminer, "arguments", _miner.Value.Arguments, true);
                    AddAttribute(_xminer, "host", _miner.Value.Host, true);
                    AddAttribute(_xminer, "port", _miner.Value.Port, true);
                    AddAttribute(_xminer, "username", _miner.Value.UserName, true);
                    AddAttribute(_xminer, "password", _miner.Value.Password, true);
                    AddAttribute(_xminer, "netupdate", _miner.Value.Enabled);
                }

                XElement _xlog = new XElement("Log");
                {
                    AddAttribute(_xlog, "enabled", _miner.Value.Logging.Enabled);
                    AddAttribute(_xlog, "maxfilesize", _miner.Value.Logging.MaxFileSize);
                    _xminer.Add(_xlog);
                }

                XElement _xcrash = new XElement("CrashRecovery");
                {
                    AddAttribute(_xcrash, "retries", _miner.Value.CrashRecovery.Retries);
                    AddAttribute(_xcrash, "interval", _miner.Value.CrashRecovery.Interval);
                    AddAttribute(_xcrash, "altminer", _miner.Value.CrashRecovery.AlternateMiner, true);
                    _xminer.Add(_xcrash);
                }

                XElement _xprocess = new XElement("Process");
                {
                    AddAttribute(_xprocess, "priority", _miner.Value.Process.Priority);
                    AddAttribute(_xprocess, "cpuaffinity", _miner.Value.Process.CPUAffinity, true);
                    AddAttribute(_xprocess, "loadprofile", _miner.Value.Process.LoadUserProfile);
                    AddAttribute(_xprocess, "windomain", _miner.Value.Process.WinDomain, true);
                    AddAttribute(_xprocess, "winusername", _miner.Value.Process.WinUserName, true);
                    AddAttribute(_xprocess, "winpassword", _miner.Value.Process.WinPassword, true);
                    _xminer.Add(_xprocess);
                }

                _miners.Add(_xminer);
            }

            p_root.Add(_miners);
        }

        private void SaveMinerOptions(XElement root)
        {
            XElement options = new XElement("MinerOptions");
            foreach (var mo in this.MinerOptions)
            {
                XElement option = new XElement("MinerOption");
                AddAttribute(option, "name", mo.Key);
                AddAttribute(option, "path", mo.Value.Path);
                AddAttribute(option, "argumentformat", mo.Value.ArgumentFormat);
                AddAttribute(option, "arguments", mo.Value.Arguments);
                options.Add(option);
            }
            root.Add(options);
        }

        private void SaveMinerPools(XElement root)
        {
            XElement pools = new XElement("Pools");
            foreach (var p in this.Pools)
            {
                XElement pool = new XElement("Pool");
                AddAttribute(pool, "name", p.Name);
                AddAttribute(pool, "host", p.Host);
                AddAttribute(pool, "port", p.Port);
                AddAttribute(pool, "website", p.Website, true);
                pools.Add(pool);
            }
            root.Add(pools);
        }

        private void AddAttribute(XElement element, string name, object value)
        {
            AddAttribute(element, name, value, false);
        }

        private void AddAttribute(XElement element, string name, object value, bool skipIfNull)
        {
            if (skipIfNull)
            {
                if (value != null)
                {
                    element.Add(new XAttribute(name, value));
                }
            }
            else
            {
                element.Add(new XAttribute(name, value));
            }
        }

        #endregion

        #endregion
    }
}