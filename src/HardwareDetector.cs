using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace BitMoose.Core
{
    public class HardwareDetector
    {
        #region " Properties "

        public int CPUPhysical
        {
            get;
            protected set;
        }

        public int CPUPhysicalCores
        {
            get;
            protected set;
        }

        public int CPULogicalCores
        {
            get;
            protected set;
        }

        public int GPUPhysical
        {
            get;
            protected set;
        }

        #endregion

        #region " Methods "

        public static string[] GetVgaCardDescriptions()
        {
            var _result = new List<string>();

            ManagementObjectSearcher _searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver");
            foreach (ManagementObject _item in _searcher.Get())
            {
                var _device_class = _item["DeviceClass"];
                if (_device_class != null && _device_class.ToString() == "DISPLAY")
                    _result.Add(_item.Properties["Description"].Value.ToString());
            }

            return _result.ToArray();
        }

        public static string GetGeForceDescription()
        {
            var _result = String.Empty;

            string[] _descriptions = GetVgaCardDescriptions();
            foreach (var _d in _descriptions)
            {
                if (_d != null && _d.Contains("GeForce") == true)
                {
                    _result = _d;
                    break;
                }
            }

            return _result;
        }

        public static int GetGeForceLevel()
        {
            var _result = 0;

            string[] _descriptions = GetVgaCardDescriptions();
            foreach (var _d in _descriptions)
            {
                if (_d != null && _d.Contains("GeForce") == true)
                {
                    _result++;

                    if (_d.Contains("GTX") == true)
                        _result++;

                    break;
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Detect()
        {
            int _count = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                _count += Convert.ToInt32(item["NumberOfProcessors"]);
            }
            this.CPUPhysical = _count;

            _count = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                _count += Convert.ToInt32(item["NumberOfCores"]);
            }
            this.CPUPhysicalCores = _count;

            _count = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                _count += Convert.ToInt32(item["NumberOfLogicalProcessors"]);
            }
            this.CPULogicalCores = _count;

            _count = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver").Get())
            {
                var _device_class = item["DeviceClass"];
                if (_device_class != null && _device_class.ToString() == "DISPLAY")
                    _count++;
            }
            this.GPUPhysical = _count;
        }

        #endregion

    }
}