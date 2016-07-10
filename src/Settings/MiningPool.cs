using System;
using System.Collections.Generic;
using System.Linq;

namespace BitMoose.Core.Settings
{
    public class MiningPool
    {
        #region " Properties "

        public string Name
        {
            get;
            set;
        }

        public string Host
        {
            get;
            set;
        }

        public short Port
        {
            get;
            set;
        }

        public string Website
        {
            get;
            set;
        }

        #endregion

    }
}