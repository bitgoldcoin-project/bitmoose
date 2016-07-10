using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMoose.Core.Settings
{
    public class MinerLogSetting
    {
        private bool m_Enabled = true;
        private int m_MaxFileSize = 512000;

        #region " Properties "

        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        public int MaxFileSize
        {
            get
            {
                return m_MaxFileSize;
            }
            set
            {
                m_MaxFileSize = value;
            }
        }

        #endregion

    }
}