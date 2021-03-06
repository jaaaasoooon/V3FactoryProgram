using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BoqiangH5Entity
{
    public class H5BmsInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public int Index { get; set; }

        public int Address { get; set; }

        public string AddressStr { get; set; }//增加一个地址字符串，用于信息界面显示

        public string Description { get; set; }

        string strValue;
        public string StrValue
        {
            get { return strValue; }
            set
            {
                strValue = value;
                if (this.PropertyChanged != null)//激发事件，参数为 StrValue 属性  
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("StrValue"));
                }
            }
        }

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public string Unit { get; set; }

        public string Scale { get; set; }

        public byte RegisterNum { get; set; }

        public byte ByteCount { get; set; }

        public string Conversion { get; set; }

        public bool IsShow { get; set; }

        private BalanceStatusEnum balStat;
        /// <summary>
        /// 状态
        /// </summary>
        public virtual BalanceStatusEnum BalanceStat
        {
            get { return balStat; }
            set
            {
                balStat = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("BalanceStat"));
            }
        }
    }

    public class BitStatInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string BitInfo { get; set; }

        public int ByteIndex { get; set; }

        public int BitIndex { get; set; }
        
        bool bBitStatus = false;
        public bool IsSwitchOn
        {
            get { return bBitStatus; }
            set
            {
                bBitStatus = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsSwitchOn"));
                }

            }
        }

        SolidColorBrush foreColor = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45));
        public SolidColorBrush ForeColor
        {
            get { return foreColor; }
            set
            {
                foreColor = value;
                if (this.PropertyChanged != null)    
                {
                    this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("ForeColor"), null, null);
                }
            }
        }

        SolidColorBrush backColor = new SolidColorBrush(Colors.DarkGray); 
        public SolidColorBrush BackColor
        {
            get { return backColor; }
            set
            {
                backColor = value;
                if (this.PropertyChanged != null)  
                {
                    
                    this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("BackColor"), null, null);
                }
            }
        }

        public bool IsShow { get; set; }
        public bool IsWarning { get; set; }
        public bool IsProtect { get; set; }
    }

    public class ManufactureCode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Code { get; set; }

        public string Description { get; set; }
    }
    /// <summary>
    /// 从csv 生成 xml 配置文件
    /// </summary>
    public class XmlNodeInfo 
    {
        public string Index { get; set; }

        public string DidNum { get; set; }

        public string DidDescription { get; set; }

        public string StrValue { get; set; }


        public string MinValue { get; set; }

        public string MaxValue { get; set; }

        public string Unit { get; set; }

        public string Scale { get; set; }

        public string ByteNum { get; set; }

        public string Conversion { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DTCInfo
    {
        public int Index { get; set; }

        public string DTCDisplay { get; set; }

        public string DTCBytes { get; set; }

        public string DTCMeaning { get; set; }

        public string FaultsAttribute { get; set; }

        public string MatureCondition { get; set; }

        public string SystemAction { get; set; }

        public string DematureCondition { get; set; }

        public string PossibleFaultCauses { get; set; }
    }

    public class H5RecordInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Index { get; set; }

        public byte RecordInfo { get; set; }

        public string RecordTime { get; set; }

        public string RecordType { get; set; }

        public string PackStatus { get; set; }

        public string BatteryStatus { get; set; }

        public ulong FCC { get; set; }//满充容量

        public ulong RC { get; set; }//剩余容量

        public uint SOC { get; set; }

        public uint Cell1Voltage { get; set; }

        public uint Cell2Voltage { get; set; }

        public uint Cell3Voltage { get; set; }

        public uint Cell4Voltage { get; set; }

        public uint Cell5Voltage { get; set; }

        public uint Cell6Voltage { get; set; }

        public uint Cell7Voltage { get; set; }

        public uint Cell8Voltage { get; set; }

        public uint Cell9Voltage { get; set; }

        public uint Cell10Voltage { get; set; }

        public uint Cell11Voltage { get; set; }

        public uint Cell12Voltage { get; set; }

        public uint Cell13Voltage { get; set; }

        public uint Cell14Voltage { get; set; }

        public uint Cell15Voltage { get; set; }

        public uint Cell16Voltage { get; set; }

        public double TotalVoltage { get; set; }

        public string Current { get; set; }

        public double AmbientTemp { get; set; }

        public double Cell1Temp { get; set; }

        public double Cell2Temp { get; set; }

        public int Cell3Temp { get; set; }

        public int Cell4Temp { get; set; }
        public int Cell5Temp { get; set; }

        public int Cell6Temp { get; set; }
        public int Cell7Temp { get; set; }

        public int PowerTemp { get; set; }

    }
    public class H5ProtectParamInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Index { get; set; }

        public string Description { get; set; }

        string strValue;
        public string StrValue
        {
            get { return strValue; }
            set
            {
                strValue = value;
                if (this.PropertyChanged != null)//激发事件，参数为 StrValue 属性  
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("StrValue"));
                }
            }
        }

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public string Unit { get; set; }


        public byte ByteCount { get; set; }

        public bool isUnsigned { get; set; }
    }

    public class H5DidiRecordInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Index { get; set; }

        public string RecordTime { get; set; }

        public double TotalVoltage { get; set; }

        public string Current { get; set; }

        public double Cell1Temp { get; set; }

        public double Cell2Temp { get; set; }

        public double Cell3Temp { get; set; }

        public double Cell4Temp { get; set; }

        public double Cell5Temp { get; set; }

        public int Humidity { get; set; }

        public int Cell1Voltage { get; set; }

        public int Cell2Voltage { get; set; }

        public int Cell3Voltage { get; set; }

        public int Cell4Voltage { get; set; }

        public int Cell5Voltage { get; set; }

        public int Cell6Voltage { get; set; }

        public int Cell7Voltage { get; set; }

        public int Cell8Voltage { get; set; }

        public int Cell9Voltage { get; set; }

        public int Cell10Voltage { get; set; }

        public int Cell11Voltage { get; set; }

        public int Cell12Voltage { get; set; }

        public int Cell13Voltage { get; set; }

        public int Cell14Voltage { get; set; }

        public int Cell15Voltage { get; set; }

        public int Cell16Voltage { get; set; }

        public int FCC { get; set; }//满充容量

        //public ulong RC { get; set; }//剩余容量
        public string ChargeMOSStatus { get; set; }

        public string DischargeMOSStatus { get; set; }

        public string DetStatus { get; set; }
        public string DischargeEnableStatus { get; set; }
        public int LoopNumber { get; set; }

        public uint SOC { get; set; }

        public string BatteryStatus { get; set; }

        public string Balance { get; set; }
        public string RecordType { get; set; }
        //public string PackStatus { get; set; }
        //public string BatStatus { get; set; }
        public string EventType { get; set; }
        public string PackStatus { get; set; }
        public string MosStatus { get; set; }
        public string VoltageStatus { get; set; }
        public string CurrentStatus { get; set; }
        public string TemperatureStatus { get; set; }
        public string HumidityStatus { get; set; }
        public string ConfigStatus { get; set; }
        public string CommunicationStatus { get; set; }
        public string ModeStatus { get; set; }
        public string LogicStatus { get; set; }
    }

    public class OperationRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Index { set; get; }
        public string UID { set; get; }
        public string BID { set; get; }
        public string OperationType { set; get; }
        public DateTime ModifiedTime { set; get; }
        public string Result { set; get; }
        public string TotalVoltage { set; get; }
        public string Current { set; get; }
        public string CellVoltage { set; get; }
        public string Ambient { set; get; }
        public string CellTemp { set; get; }
        public string Humidity { set; get; }
        public int? LoopNumber { set; get; }
        public DateTime? RTC { set; get; }
        public DateTime? MCUCheckTime { set; get; }
        public string Data { set; get; }
        public string Comments { set; get; }
        public string UserName { set; get; }
    }

    public class RepairRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Index { set; get; }
        public string UID { set; get; }
        public string BMSID { set; get; }
        public DateTime ModifiedTime { set; get; }
        public string Result { set; get; }
        public string Description { set; get; }
        public string Comments { set; get; }
        public string UserName { set; get; }

    }

    public class UserRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Index { set; get; }
        public string UserNo { set; get; }
        public string UserName { set; get; }
        public string Password { set; get; }
        public string Duty { set; get; }

    }
}
