using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMoose.Core.Settings
{
    public class MinerSetting
    {
        private bool __Enabled = true;
        private bool __StatusUpdates = true;

        #region " Properties "

        public string OptionKey
        {
            get;
            set;
        }

        public string Pool
        {
            get;
            set;
        }

        public string Arguments
        {
            get;
            set;
        }

        public string Host
        {
            get;
            set;
        }

        public short? Port
        {
            get;
            set;
        }

        public string UserName
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public bool Enabled
        {
            get
            {
                return __Enabled;
            }
            set
            {
                __Enabled = value;
            }
        }

        public bool StatusUpdates
        {
            get
            {
                return __StatusUpdates;
            }
            set
            {
                __StatusUpdates = value;
            }
        }

        public MinerLogSetting Logging
        {
            get;
            set;
        }

        public MinerCrashRecoverySetting CrashRecovery
        {
            get;
            set;
        }

        public MinerProcessSetting Process
        {
            get;
            set;
        }

        #endregion

        #region " Constructor(s) "

        public MinerSetting()
        {
            this.Logging = new MinerLogSetting();
            this.CrashRecovery = new MinerCrashRecoverySetting();
            this.Process = new MinerProcessSetting();
        }

        #endregion
    }
}