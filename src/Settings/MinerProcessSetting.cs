using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitMoose.Core.Settings
{
    public class MinerProcessSetting
    {
        #region " Properties "

        public short Priority
        {
            get;
            set;
        }

        public string CPUAffinity
        {
            get;
            set;
        }

        public bool LoadUserProfile
        {
            get;
            set;
        }

        public string WinDomain
        {
            get;
            set;
        }

        public string WinUserName
        {
            get;
            set;
        }

        public string WinPassword
        {
            get;
            set;
        }

        #endregion
    }
}