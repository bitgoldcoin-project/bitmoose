using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMoose.Core.Settings
{
    public class MinerCrashRecoverySetting
    {
        private int m_Retries = 3;
        private int m_Interval = 60;

        #region " Properties "

        public int Retries
        {
            get
            {
                return m_Retries;
            }
            set
            {
                m_Retries = value;
            }
        }

        public int Interval
        {
            get
            {
                return m_Interval;
            }
            set
            {
                m_Interval = value;
            }
        }

        public string AlternateMiner
        {
            get;
            set;
        }

        #endregion

    }
}