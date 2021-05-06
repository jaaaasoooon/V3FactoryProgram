using BoqiangH5.BQProtocol;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO.Ports;
using DBService;
using BoqiangH5.DDProtocol;
using System.Windows.Documents;
using System.Text;
using System.Net.NetworkInformation;

namespace BoqiangH5
{
    /// <summary>
    /// UserCtrlBqBmsInfo.xaml 的交互逻辑
    /// </summary>
    public partial class UserCtrlBqBmsInfo : UserControl
    {
        public static List<H5BmsInfo> m_ListBmsInfo = new List<H5BmsInfo>();
        public static List<H5BmsInfo> m_ListCellVoltage = new List<H5BmsInfo>();

        public static List<H5BmsInfo> m_ListBqDeviceInfo = new List<H5BmsInfo>();

        static List<BitStatInfo> m_ListSysStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListProtectStatus = new List<BitStatInfo>();

        static List<BitStatInfo> m_ListPackStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListMosStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListVoltageProtectStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListCurrentProtectStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListTemperatureProtectStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListHumidityProtectStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListConfigStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListCommunicationStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListModeStatus = new List<BitStatInfo>();
        static List<BitStatInfo> m_ListLogicStatus = new List<BitStatInfo>();
        static Dictionary<string, string> m_DicManufactureCode = new Dictionary<string, string>();

        //MCU消息
        List<H5BmsInfo> ListSysInfo1 = new List<H5BmsInfo>();
        List<H5BmsInfo> ListSysInfo2 = new List<H5BmsInfo>();
        List<H5BmsInfo> ListChargeInfo = new List<H5BmsInfo>();

        string U_ID = "默认值";
        OperationTypeEnum operateType = OperationTypeEnum.默认值;
        public event EventHandler<EventArgs<string>> RequireReadUIDEvent;
        public void SetUID(string uid)
        {
            U_ID = uid;
            if(IsUIDExist(uid))
            {
                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                Thread.Sleep(200);
                switch (operateType)
                {
                    case OperationTypeEnum.RTC校准:
                        break;
                    case OperationTypeEnum.SOC校准:
                        isAlterSOC = false;
                        ushort soc = ushort.Parse(XmlHelper.m_strSOCValue);
                        BoqiangH5.BQProtocol.BqProtocol.BqInstance.BQ_AlterSOC(BitConverter.GetBytes(soc));
                        isAlterSOC = true;
                        break;
                    case OperationTypeEnum.零点校准:
                        isAdjustZeroCurrent = false;
                        BqProtocol.BqInstance.AdjustZeroCurrent(0);
                        isAdjustZeroCurrent = true;
                        break;
                    case OperationTypeEnum.负10A校准:
                        if (SelectCANWnd.m_IsUsingKP182)//如果启用KP182
                        {
                            if ((MainWindow.serialPort != null) && (MainWindow.serialPort.IsOpen == true))
                            {
                                SetKP182CurrentMode(MainWindow.serialPort,10000);//设置KP182拉载模式为CC模式
                            }
                        }
                        else
                        {
                            if (MessageBoxResult.OK == MessageBox.Show("请开启负载设备进行放电，配置完成后再点击确定！", "-10A校准提示", MessageBoxButton.OKCancel, MessageBoxImage.Information))
                            {
                                isAdjust10ACurrent = false;
                                preAdjustCurrentVal = tbCurrent.Text.Trim();
                                BqProtocol.BqInstance.AdjustRtCurrent((int)((-10) * Math.Pow(10, 3)));
                                isAdjust10ACurrent = true;
                            }
                        };
                        break;
                    case OperationTypeEnum.上电:
                        RequirePowerOnEvent?.Invoke(this, EventArgs.Empty);
                        break;
                    case OperationTypeEnum.下电:
                        RequirePowerOffEvent?.Invoke(this, EventArgs.Empty);
                        break;
                    case OperationTypeEnum.关机:
                        var socItem = m_ListBmsInfo.FirstOrDefault(p => p.Description == "SOC");
                        if (socItem != null)
                        {
                            int socVal = Int32.Parse(socItem.StrValue);
                            if (socVal != OneClickFactorySetting.m_SOCValue)
                            {
                                RefreshResult("失败", false);
                                ShowMessage(string.Format("SOC值检测失败，SOC值不为设置值 {0}% ！",OneClickFactorySetting.m_SOCValue), false);
                                return;
                            }
                            else
                            {
                                RefreshResult("成功", true);
                                ShowMessage("SOC值检测成功！", true);
                            }
                        }
                        else
                        {
                            RefreshResult("失败", false);
                            ShowMessage("找不到SOC检测项，请确认检测项 ！", false);
                            return;
                        }
                        isDeepSleep = false;
                        BqProtocol.bReadBqBmsResp = true;
                        BqProtocol.BqInstance.BQ_Shutdown();
                        isDeepSleep = true;
                        break;
                    case OperationTypeEnum.休眠:
                        isShallowSleep = false;
                        BqProtocol.bReadBqBmsResp = true;
                        BqProtocol.BqInstance.BQ_Sleep();
                        isShallowSleep = true;
                        break;
                    case OperationTypeEnum.Eeprom写入:
                        WriteEepromEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_EepromFilePath.Trim()));
                        break;
                    case OperationTypeEnum.MCU写入:
                        WriteMcuEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                        break;
                    case OperationTypeEnum.一键出厂配置:
                        ShowMessage("一键出厂配置开始！", true);
                        RefreshResult("配置中", false);
                        BqProtocol.BqInstance.m_bIsOneClick = true;
                        isOneClickFactoryConfig = true;
                        Thread.Sleep(300);
                        //OneClickEvent?.Invoke(this, new EventArgs<bool>(true));
                        RequireReadRecordEvent?.Invoke(this, EventArgs.Empty);
                        break;
                    case OperationTypeEnum.一键出厂检验:
                        RefreshResult("检验中", false);
                        ShowMessage("一键出厂检验开始！", true);
                        BqProtocol.BqInstance.m_bIsOneClick = true;
                        Rtc = null;
                        CheckMCUTime = null;
                        Thread.Sleep(300);
                        isOneClickCheck = true;
                        RequireReadRecordEvent?.Invoke(this, EventArgs.Empty);
                        break;
                    case OperationTypeEnum.充放电测试:
                		isChargeOrDischargeTest = true;
                		flag = false;//使用两个电池负载的时候，用于判断
                		isCharge = false;//开始先放电10A，其次充电16A，所以第二次才是充电，标志位置1
                		ShowMessage("充放电测试开始！", false);
                		RefreshResult("测试开始", false);
                		deviceAddress = 0x01;
                		currentValue = 0;
                		waitTime = 0;
                		currentError = 0;
                		//CSVFileHelper.WriteLogs("log", "充放电", "触发上电请求！");
                		btnPowerON_Click(null, null);
                        break;
                    case OperationTypeEnum.BMS绑定:
                        using (V3Entities eb12 = new V3Entities())
                        {
                            var item = eb12.uidrecord.FirstOrDefault(p => p.UID == U_ID);
                            if (!string.IsNullOrEmpty(item.BMSID))
                            {
                                if (MessageBoxResult.Yes == MessageBox.Show(string.Format("该BMS已绑定ID {0}，是否更改绑定？", item.BMSID), "提示", MessageBoxButton.YesNo, MessageBoxImage.Information))
                                {
                                    //做权限管理
                                    VerifyWnd wnd = new VerifyWnd();
                                    wnd.ShowDialog();
                                    if (wnd.isOK == false)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if(Binding(U_ID, tbSn.Text.Trim()))
                                {
                                    tbSn.Text = string.Empty;
                                }
                            }
                        }

                        break;
                    default:
                        BqProtocol.BqInstance.m_bIsStopCommunication = false;
                        break;
                }
            }
            else
            {
                if(operateType == OperationTypeEnum.BMS注册)
                {
                    BMSRegister(uid);
                }
                else
                {
                    ShowMessage(string.Format("该BMS的UID {0} 未进行注册，请注册后再进行操作！", uid), false);
                }
            }
        }
        public UserCtrlBqBmsInfo()
        {
            InitializeComponent();
      
            InitBqBmsInfoWnd();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(OnTimer);
        }

        private void InitBqBmsInfoWnd()
        {
            m_ListCellVoltage.Clear();
            m_ListBmsInfo.Clear();

            string strConfigFile = XmlHelper.m_strBqProtocolFile; 

            XmlHelper.LoadXmlConfig(strConfigFile, "bms_info/cell_votage_info", m_ListCellVoltage);
            XmlHelper.LoadXmlConfig(strConfigFile, "bms_info/bms_info_node", m_ListBmsInfo);

            XmlHelper.LoadXmlConfig(strConfigFile, "bq_device_info/sys_config_info", m_ListBqDeviceInfo);

            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "pack_status_info/byte_status_info/bit_status_info", m_ListPackStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "mos_status_info/byte_status_info/bit_status_info", m_ListMosStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "voltage_protect_status_info/byte_status_info/bit_status_info", m_ListVoltageProtectStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "current_protect_status_info/byte_status_info/bit_status_info", m_ListCurrentProtectStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "temperature_protect_status_info/byte_status_info/bit_status_info", m_ListTemperatureProtectStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "humidity_protect_status_info/byte_status_info/bit_status_info", m_ListHumidityProtectStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "config_status_info/byte_status_info/bit_status_info", m_ListConfigStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "communication_status_info/byte_status_info/bit_status_info", m_ListCommunicationStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "mode_status_info/byte_status_info/bit_status_info", m_ListModeStatus);
            XmlHelper.LoadBqBmsStatusConfig(strConfigFile, "logic_status_info/byte_status_info/bit_status_info", m_ListLogicStatus);
            XmlHelper.LoadManufactureCode(strConfigFile, "manufacture_code/manufacture_code_node", m_DicManufactureCode);

            //读MCU参数
            XmlHelper.LoadXmlConfig(strConfigFile, "mcu_info/system1/mcu_node_info", ListSysInfo1);
            XmlHelper.LoadXmlConfig(strConfigFile, "mcu_info/system2/mcu_node_info", ListSysInfo2);
            XmlHelper.LoadXmlConfig(strConfigFile, "mcu_info/charge_discharge/mcu_node_info", ListChargeInfo);

            m_ListSysStatus.AddRange(m_ListPackStatus);
            //m_ListSysStatus.AddRange(m_ListMosStatus);
            foreach (var it in m_ListMosStatus)
            {
                if (it.BitInfo.Contains("损坏"))
                {
                    m_ListProtectStatus.Add(it);
                }
                else
                {
                    m_ListSysStatus.Add(it);
                }
            }
            m_ListSysStatus.AddRange(m_ListConfigStatus);
            m_ListSysStatus.AddRange(m_ListCommunicationStatus);
            m_ListSysStatus.AddRange(m_ListModeStatus);
            m_ListSysStatus.AddRange(m_ListLogicStatus);

            m_ListProtectStatus.AddRange(m_ListVoltageProtectStatus);
            m_ListProtectStatus.AddRange(m_ListCurrentProtectStatus);
            m_ListProtectStatus.AddRange(m_ListTemperatureProtectStatus);
            m_ListProtectStatus.AddRange(m_ListHumidityProtectStatus);
        }

        private void ucBqBmsInfo_Loaded(object sender, RoutedEventArgs e)
        {
            dgBqBmsInfo.ItemsSource = m_ListBmsInfo;
            dgBqBmsCellVoltage.ItemsSource = m_ListCellVoltage;

            dgBqDeviceInfo.ItemsSource = m_ListBqDeviceInfo;

            listBoxSysStatus.ItemsSource = m_ListSysStatus;
            listBoxBatStatus.ItemsSource = m_ListProtectStatus;
            if (m_ListCellVoltage.Count > 2)
            {
                DataGridRow row = DataGridExtension.GetRow(dgBqBmsCellVoltage, 0);
                row.Visibility = Visibility.Collapsed;
                DataGridRow _row = DataGridExtension.GetRow(dgBqBmsCellVoltage, 1);
                _row.Visibility = Visibility.Collapsed;
            }

        }

        public static Dictionary<string, Dictionary<int, string>> Dic_Mac_Operation = new Dictionary<string, Dictionary<int, string>>();
        public void GetMacOperation()
        {
            using (V3Entities v3 = new V3Entities())
            {
                var items = from oper in v3.operation
                            join mac_oper in v3.mac_operation on oper.OperationID equals mac_oper.OperationID
                            join mac in v3.computermac on mac_oper.MACID equals mac.ID
                            select new
                            {
                                macAddress = mac.MAC,
                                operID = oper.OperationID,
                                type = oper.Type
                            };

                foreach (var item in items)
                {
                    if (Dic_Mac_Operation.ContainsKey(item.macAddress))
                    {
                        var dic = Dic_Mac_Operation[item.macAddress];
                        if (dic.ContainsKey(item.operID))
                        {
                            continue;
                        }
                        else
                        {
                            dic.Add(item.operID, item.type);
                        }
                    }
                    else
                    {
                        Dictionary<int, string> dic = new Dictionary<int, string>();
                        dic.Add(item.operID, item.type);
                        Dic_Mac_Operation.Add(item.macAddress, dic);
                    }
                }

            }
        }
        public bool GetLocalMac(out string mac)
        {
            mac = string.Empty;
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection allAddress = adapterProperties.UnicastAddresses;
                if (allAddress.Count > 0)
                {
                    if (adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        mac = adapter.GetPhysicalAddress().ToString();
                        break;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(mac))
            {
                if (mac.Length == 12)
                {
                    mac = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", mac.Substring(0, 2), mac.Substring(2, 2), mac.Substring(4, 2),
                        mac.Substring(6, 2), mac.Substring(8, 2), mac.Substring(10, 2));
                    return true;
                }
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
        public void GetMacSetting()
        {
            string Mac;
            if (!GetLocalMac(out Mac))
            {
                MessageBox.Show("获取本机MAC地址失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                GetMacOperation();
                if (Dic_Mac_Operation.Count > 0 && MainWindow.RoleID == 4)
                {
                    Dictionary<int, string> dic_operations = Dic_Mac_Operation[Mac];
                    foreach (var item in dic_operations.Values)
                    {
                        switch (item)
                        {
                            case "上电":
                                btnPowerON.IsEnabled = true; 
                                break;
                            case "下电":
                                btnPowerOFF.IsEnabled = true;
                                break;
                            case "关机":
                                btnDeepSleep.IsEnabled = true;
                                break;
                            case "休眠":
                                btnShallowSleep.IsEnabled = true;
                                break;
                            case "零点校准":
                                btnAdjustZero.IsEnabled = true;
                                break;
                            case "负10A校准":
                                btnAdjust10A.IsEnabled = true;
                                break;
                            case "SOC校准":
                                btnAdjustSOC.IsEnabled = true;
                                break;
                            case "充放电测试":
                                btnChargeOrDischarge.IsEnabled = true;
                                break;
                            case "一键出场配置":
                                btnOneClickFactory.IsEnabled = true;
                                break;
                            case "一键出厂检验":
                                btnOneClickFactoryCheck.IsEnabled = true;
                                break;
                            case "BMS注册":
                                btnBMSRegister.IsEnabled = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        public void SetOffLineUIStatus()
        {
            SetOffLineStatus(m_ListSysStatus);
            SetOffLineStatus(m_ListProtectStatus);
            count = 0;
            RefreshResult("待测试", false);
        }
        private void SetOffLineStatus(List<BitStatInfo> listStatInfo)
        {
            for (int n = 0; n < listStatInfo.Count; n++)
            {
                listStatInfo[n].IsSwitchOn = false;
                listStatInfo[n].BackColor = new SolidColorBrush(Colors.DarkGray);
            }
        }

        public void HandleRecvBmsInfoDataEvent(object sender, CustomRecvDataEventArgs e)
        {
            DataGridRow row1 = DataGridExtension.GetRow(dgBqBmsInfo, 0);
            for (int i = 0; i < m_ListCellVoltage.Count; i++)
            {
                DataGridRow row = DataGridExtension.GetRow(dgBqBmsCellVoltage, i);
                row.Background = row1.Background;
            }
            BqUpdateBmsInfo(e.RecvMsg);
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var soc = m_ListBmsInfo.SingleOrDefault(p => p.Description == "SOC");
                if (soc != null)
                {
                    ucBattery.Battery_Glasses(100, 0, double.Parse(soc.StrValue));
                }
                var current = m_ListCellVoltage.SingleOrDefault(p => p.Description == "实时电流");
                if (current != null)
                {
                    tbCurrent.Text = current.StrValue;
                }
                var voltage = m_ListCellVoltage.SingleOrDefault(p => p.Description == "电池包电压");
                if (voltage != null)
                {
                    tbVoltage.Text = voltage.StrValue;
                }

                if (maxVoltageCellNum > 0)
                {
                    DataGridRow row = DataGridExtension.GetRow(dgBqBmsCellVoltage, maxVoltageCellNum - 1 + 2);//加上隐藏的两行
                    row.Background = new SolidColorBrush(Colors.SkyBlue);
                }

                if (minVoltageCellNum > 0)
                {
                    DataGridRow row = DataGridExtension.GetRow(dgBqBmsCellVoltage, minVoltageCellNum - 1 + 2);//加上隐藏的两行
                    row.Background = new SolidColorBrush(Colors.YellowGreen);
                }
            }));
        }

        string bmsfilePath = string.Empty;
        string cellfilePath = string.Empty;
        System.Windows.Threading.DispatcherTimer timer = null;
        FileStream _fs = null;
        StreamWriter sw = null;
        //lipeng   2020.3.26,增加BMS信息记录
        private void cbIsSaveBms_Click(object sender, RoutedEventArgs e)
        {
            FileStream fs = null;
            bmsfilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"Data\Bms_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            if ((bool)cbIsSaveBms.IsChecked)
            {
                //BqProtocol.BqInstance.m_bIsSaveBmsInfo = true;
                //if (!(bool)cbIsUpdate.IsChecked)
                //{
                //    cbIsUpdate.IsChecked = true;
                //    BqProtocol.BqInstance.m_bIsUpdateBmsInfo = true;
                //}
                FileInfo fi = new FileInfo(bmsfilePath);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                if (!File.Exists(bmsfilePath))
                {
                    fs = File.Create(bmsfilePath);//创建该文件
                    fs.Close();
                    CSVFileHelper.SaveBmsORCellCSVTitle(bmsfilePath,true,m_ListBmsInfo,m_ListCellVoltage,new List<H5BmsInfo>());//保存Bms数据文件头
                }

                int _interval = SelectCANWnd.m_RecordInterval;

                //msgQueue = new Queue<string>();
                timer.Interval = new TimeSpan(0, 0, _interval);
                timer.Start();

                _fs = new FileStream(bmsfilePath, System.IO.FileMode.Append, System.IO.FileAccess.Write);

                sw = new StreamWriter(_fs, System.Text.Encoding.Default);
            }
            else
            {
                BqProtocol.BqInstance.m_bIsSaveBmsInfo = false;
                if (fs != null)
                {
                    fs.Close();
                    bmsfilePath = string.Empty;
                }
                if (timer != null)
                    timer.Stop();
                sw.Close();
                _fs.Close();
            }

        }

        private void OnTimer(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(System.DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分ss秒"));
            sb.Append(",");
            foreach (var it in m_ListBmsInfo)
            {
                //if (!it.Description.Contains("保留"))
                {
                    sb.Append(it.StrValue);
                    sb.Append(",");
                    if (it.Description == "SOC")
                    {
                        sb.Append(m_ListCellVoltage.SingleOrDefault(p => p.Description == "电池包电压").StrValue);
                        sb.Append(",");
                        sb.Append(m_ListCellVoltage.SingleOrDefault(p => p.Description == "实时电流").StrValue);
                        sb.Append(",");
                        sb.Append(cellMinVoltage.ToString());
                        sb.Append(",");
                        sb.Append(cellMaxVoltage.ToString());
                        sb.Append(",");
                    }
                }
            }
            foreach (var it in m_ListCellVoltage)
            {
                if (it.Description == "电池包电压" || it.Description == "实时电流")
                {
                    continue;
                }
                else
                {
                    sb.Append(it.StrValue);
                    sb.Append(",");
                }
            }
            if (sb.Length != 0)
            {
                sw.WriteLine(sb.ToString()); ;
            }
        }

        bool isDeepSleep = false;//在下发消息命令的时候增加此bool变量，拒绝总线上的其他回复消息
        public event EventHandler DeepSleepEvent;
        public event EventHandler ShallowSleepEvent;
        private void btnDeepSleep_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.关机;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }
        bool isShallowSleep = false;//在下发消息命令的时候增加此bool变量，拒绝总线上的其他回复消息
        private void btnShallowSleep_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.休眠;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        public void HandleDebugEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (isDeepSleep || isShallowSleep || isAlterSOC)
            {
                BqProtocol.bReadBqBmsResp = true;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    switch (e.RecvMsg[1])
                    {
                        case 0xBA:
                            if (isDeepSleep)
                            {
                                var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                                if (res == 0)
                                {
                                    DeepSleepEvent?.Invoke(this, EventArgs.Empty);//设置深休眠成功，断开连接
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.关机, OperationResultEnum.成功, string.Empty);
                                    MessageBox.Show("关机模式设置成功！", "设置关机模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.关机, OperationResultEnum.失败, "关机模式设置失败");
                                    MessageBox.Show("关机模式设置失败！", "设置关机模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                isDeepSleep = false;
                            }
                            break;
                        case 0xBB:
                            if (isShallowSleep)
                            {
                                var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                                if (res == 0)
                                {
                                    ShallowSleepEvent?.Invoke(this, EventArgs.Empty);//设置浅休眠成功，断开连接
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.休眠, OperationResultEnum.成功, string.Empty);
                                    MessageBox.Show("休眠模式设置成功！", "设置休眠模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.休眠, OperationResultEnum.失败, "休眠模式设置失败");
                                    MessageBox.Show("休眠模式设置失败！", "设置休眠模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                isShallowSleep = false;
                            }
                            break;
                        case 0xB9:
                            if (isAlterSOC)
                            {
                                var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                                if (res == 0)
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.成功, string.Empty);
                                    MessageBox.Show("SOC设置成功！", "设置SOC提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.失败, "SOC设置失败");
                                    MessageBox.Show("SOC设置失败！", "设置SOC提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                isAlterSOC = false;
                            }
                            break;
                        default:
                            {
                                if (isDeepSleep)
                                {
                                    isDeepSleep = false;
                                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.关机, OperationResultEnum.失败, "关机模式设置失败！");
                                    MessageBox.Show("关机模式设置失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else if (isShallowSleep)
                                {
                                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.休眠, OperationResultEnum.失败, "休眠模式设置失败！");
                                    MessageBox.Show("休眠模式设置失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                    isShallowSleep = false;
                                }
                                else if (isAlterSOC)
                                {
                                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.失败, "SOC设置失败！");
                                    MessageBox.Show("SOC设置失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                    isAlterSOC = false;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void tbSn_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                InitInterfaceShow();
                if (MainWindow.m_statusBarInfo.IsOnline)
                {
                    string str = tbSn.Text.Trim();
                    if (str.Length <= 16)
                    {
                        operateType = OperationTypeEnum.BMS绑定;
                        //BqProtocol.BqInstance.m_bIsStopCommunication = true;
                        //Thread.Sleep(200);
                        RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
                    }
                    else
                    {
                        ShowMessage("输入条码长度大于16位，请检查！", false);
                    }
                }
                else
                {
                    tbSn.Text = string.Empty;
                    ShowMessage("系统未连接，请先连接设备再进行操作！", false);
                }
            }
        }

        bool isAlterSOC = false;
        private void btnAdjustSOC_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline == true)
            {
                string str = @"^[0-9]{1,3}$";
                if (!Regex.IsMatch(XmlHelper.m_strSOCValue, str))
                {
                    //MessageBox.Show("请输入正确的 SOC 值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowMessage("请输入正确的 SOC 值！", false);
                    return;
                }
                byte socVal = byte.Parse(XmlHelper.m_strSOCValue);

                if (socVal < 0 || socVal > 100)
                {
                    //MessageBox.Show("请输入正确的 SOC 值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowMessage("请输入正确的 SOC 值！", false);
                    return;
                }
                operateType = OperationTypeEnum.SOC校准;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));

            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                //MessageBox.Show("系统未连接，请先连接设备再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        bool isAdjustZeroCurrent = false;
        private void btnAdjustZero_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline == true)
            {
                preAdjustCurrentVal = tbCurrent.Text.Trim();
                operateType = OperationTypeEnum.零点校准;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                //MessageBox.Show("系统未连接，请先连接设备再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        public void HandleAdjustZeroCurrenEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (isAdjustZeroCurrent)
            {
                BqProtocol.bReadBqBmsResp = true;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xA8 && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.成功, string.Empty,preAdjustCurrentVal,"0");
                    ShowMessage("校准零点电流成功！", true);
                }
                else
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.失败, "校准零点电流失败！", preAdjustCurrentVal, "0");
                    ShowMessage("校准零点电流失败！", false);
                }
                isAdjustZeroCurrent = false;
            }
        }
        bool isAdjust10ACurrent = false;
        string preAdjustCurrentVal = string.Empty;
        private void btnAdjust10A_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline == true)
            {
                preAdjustCurrentVal = tbCurrent.Text.Trim();
                operateType = OperationTypeEnum.负10A校准;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        public void HandleAdjust10AEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (isAdjust10ACurrent)
            {
                BqProtocol.bReadBqBmsResp = true;
                if (SelectCANWnd.m_IsUsingKP182)//如果启用KP182
                {
                    if ((MainWindow.serialPort != null) && (MainWindow.serialPort.IsOpen == true))
                    {
                        SetKP182CurrentSwitchOFF(MainWindow.serialPort);
                    }       
                }

                if (e.RecvMsg[0] == 0xC2 || e.RecvMsg.Count == 0x03)
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.负10A校准, OperationResultEnum.成功, string.Empty, preAdjustCurrentVal, "-10000");
                    ShowMessage("10A电流校准成功！", true);
                }
                else
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.负10A校准, OperationResultEnum.失败, "10A电流校准失败！", preAdjustCurrentVal, "-10000");
                    ShowMessage("10A电流校准失败！", false);
                }
                isAdjust10ACurrent = false;
            }
        }
        public event EventHandler<EventArgs<string>> WriteMcuEvent;
        private void btnWriteMcu_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (!string.IsNullOrEmpty(OneClickFactorySetting.m_MCUFilePath))
            {
                if (MainWindow.m_statusBarInfo.IsOnline)
                {
                    operateType = OperationTypeEnum.MCU写入;
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
                    //WriteMcuEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                }
                else
                {
                    //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                    ShowMessage("系统未连接，请先连接设备再进行操作！", false);
                }
            }
            else
            {
                //MessageBox.Show("请先选择要写入的MCU文件!", "MCU写入提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowMessage("请先选择要写入的MCU文件！", false);
            }
        }

        public event EventHandler<EventArgs<string>> WriteEepromEvent;
        private void btnWriteEeprom_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (!string.IsNullOrEmpty(OneClickFactorySetting.m_EepromFilePath))
            {
                if (MainWindow.m_statusBarInfo.IsOnline)
                {
                    operateType = OperationTypeEnum.Eeprom写入;
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
                }
                else
                {
                    //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                    ShowMessage("系统未连接，请先连接设备再进行操作！", false);
                }
            }
            else
            {
                //MessageBox.Show("请先选择要写入的Eeprom文件！","Eeprom写入提示",MessageBoxButton.OK,MessageBoxImage.Information);
                ShowMessage("请先选择要写入的Eeprom文件！", false);
            }
        }

        public void AutoStartOneClickFactory()
        {
            Thread.Sleep(100);
            btnOneClickFactory_Click(null,null);
        }
        #region  一键出厂配置
        public event EventHandler RequireReadRTCEvent;
        public event EventHandler RequireAdjustRTCEvent;
        public event EventHandler RequireReadBootMsgEvent;
        public event EventHandler<EventArgs<string>> RequireAdjustSOCEvent;
        public event EventHandler RequireAdjustZeroCurrentEvent;
        public event EventHandler RequireAdjust10ACurrentEvent;
        public event EventHandler<EventArgs<string>> RequireWriteEepromEvent;
        public event EventHandler<EventArgs<string>> RequireWriteMcuEvent;
        //public EventWaitHandle waitHandle = new AutoResetEvent(false);
        public event EventHandler RequireReadRecordEvent;//读一条备份数据，检查Eeprom是否异常
        bool isOneClickFactoryConfig = false;
        public long count = 0;
        private void btnOneClickFactory_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.一键出厂配置;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));

            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        public void GetRecordsOver(bool isOK)
        {
            if(isOK)
            {
                ShowMessage("读取故障记录成功，检查Eeprom正常！", true);
                if (isOneClickCheck)
                {
                    #region 取消
                    ////if (OneClickFactorySetting.m_VoltageCheck == 1)
                    ////{
                    ////    ////电压核验
                    ////    if (isVoltageDiffHigh)
                    ////    {
                    ////        RefreshResult("失败", false);
                    ////        ShowMessage("电芯电压核验失败，压差过大！", false);
                    ////        SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "电芯电压核验失败，压差过大！");
                    ////        BqProtocol.BqInstance.m_bIsOneClick = false;
                    ////        isOneClickCheck = false;
                    ////        return;
                    ////    }
                    ////    else
                    ////     {
                    ////        ShowMessage("电芯电压检验成功！", true);
                    ////    }
                    ////}

                    ////Thread.Sleep(100);
                    ////double temperature = 0, humidity = 0;
                    ////if (OneClickFactorySetting.m_TemperatureCheck == 1)
                    ////{
                    ////    if (XmlHelper.m_strIsUsingTH10SB == "1")
                    ////    {
                    ////        if (MainWindow.th10sbSerialPort != null)
                    ////        {
                    ////            if (MainWindow.th10sbSerialPort.IsOpen)
                    ////            {
                    ////                ReadTH10SBVaule(out temperature, out humidity);
                    ////                if (temperature != 0)
                    ////                {
                    ////                    bool isTempDiffNG = false;
                    ////                    foreach (var _it in m_ListBmsInfo)
                    ////                    {
                    ////                        if (_it.Description == "环境温度" || _it.Description == "电芯温度4" || _it.Description == "电芯温度5" || _it.Description == "电芯温度6"
                    ////                            || _it.Description == "电芯温度7" || _it.Description == "单体最大温度" || _it.Description == "单体最小温度")
                    ////                        {
                    ////                            double val = double.Parse(_it.StrValue);
                    ////                            if (Math.Abs(val - temperature) > OneClickFactorySetting.m_TemperatureError)
                    ////                            {
                    ////                                isTempDiffNG = true;
                    ////                            }
                    ////                        }
                    ////                    }
                    ////                    ////温度核验
                    ////                    if (isTempDiffNG)
                    ////                    {
                    ////                        RefreshResult("失败", false);
                    ////                        //SaveTemperetureValue(OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////                        //MessageBox.Show("电芯温度核验失败，温差过大！", "温度核验提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ////                        ShowMessage("电芯温度核验失败，温差过大！", false);
                    ////                        //SaveOneClickCheckOperation(null, null, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////                        SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////                        isOneClickCheck = false;
                    ////                        BqProtocol.BqInstance.m_bIsOneClick = false;
                    ////                        return;
                    ////                    }
                    ////                    else
                    ////                    {
                    ////                        //AutoClosedMsgBox.Show("电芯温度检验成功！", "温度检验提示", 500, 64);
                    ////                        ShowMessage("电芯温度检验成功！", true);
                    ////                        //SaveTemperetureValue(OperationResultEnum.成功, string.Empty);
                    ////                    }
                    ////                }
                    ////            }
                    ////        }
                    ////    }
                    ////    else
                    ////    {
                    ////        ////温度核验
                    ////        if (isTempDiffHigh)
                    ////        {
                    ////            RefreshResult("失败", false);
                    ////            //SaveTemperetureValue(OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////            //MessageBox.Show("电芯温度核验失败，温差过大！", "温度核验提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ////            ShowMessage("电芯温度核验失败，温差过大！", false);
                    ////            //SaveOneClickCheckOperation(null, null, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////            SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                    ////            BqProtocol.BqInstance.m_bIsOneClick = false;
                    ////            isOneClickCheck = false;
                    ////            return;
                    ////        }
                    ////        else
                    ////        {
                    ////            //MessageBox.Show("电芯温度检验成功！", "温度检验提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ////            //AutoClosedMsgBox.Show("电芯温度检验成功！", "温度检验提示", 500, 64);
                    ////            ShowMessage("电芯温度检验成功！", true);
                    ////            //SaveTemperetureValue(OperationResultEnum.成功, string.Empty);
                    ////        }
                    ////    }
                    ////}
                    //////循环放电次数检查
                    ////if (isLoopNumberIsOK)
                    ////{
                    ////    ShowMessage("循环放电次数检验成功！", true);
                    ////}
                    ////else
                    ////{
                    ////    RefreshResult("失败", false);
                    ////    ShowMessage("循环放电次数检验失败，循环放电次数不为 0 ！", false);
                    ////    SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "循环放电次数检验失败，循环放电次数不为 0 ！");
                    ////    BqProtocol.BqInstance.m_bIsOneClick = false;
                    ////    isOneClickCheck = false;
                    ////    return;
                    ////}

                    //////湿度核验
                    ////if (OneClickFactorySetting.m_HumidityCheck == 1)
                    ////{
                    ////    if (XmlHelper.m_strIsUsingTH10SB == "1")
                    ////    {
                    ////        if (humidity != 0)
                    ////        {
                    ////            string humidityStr = m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue;
                    ////            double val = double.Parse(humidityStr);
                    ////            if (Math.Abs(val - humidity) > OneClickFactorySetting.m_HumidityError)
                    ////            {
                    ////                RefreshResult("失败", false);
                    ////                //MessageBox.Show("电芯湿度核验失败，湿差过大！", "温度核验提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ////                ShowMessage("电芯湿度核验失败，湿差过大！", false);
                    ////                //SaveOneClickCheckOperation(null, null, OperationResultEnum.失败, "电芯湿度核验失败，湿度差过大！");
                    ////                SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "电芯湿度核验失败，湿度差过大！");
                    ////                BqProtocol.BqInstance.m_bIsOneClick = false;
                    ////                isOneClickCheck = false;
                    ////                return;
                    ////            }
                    ////            else
                    ////            {
                    ////                //AutoClosedMsgBox.Show("电芯湿度检验成功！", "湿度检验提示", 500, 64);
                    ////                ShowMessage("电芯湿度检验成功！", true);

                    ////            }
                    ////        }
                    ////    }
                    ////}
                    ////Thread.Sleep(100);
                    //////RTC检验
                    ////RequireReadRTCEvent?.Invoke(this, EventArgs.Empty);
                    #endregion
                    RequireReadBqDeviceInfo();//请求读取博强设备信息
                }
                else
                {
                    ////写入Eeprom
                    if (OneClickFactorySetting.m_WriteEeprom == 1)
                    {
                        RequireWriteEepromEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_EepromFilePath.Trim()));
                    }
                    else
                    {
                        ////写入MCU
                        if (OneClickFactorySetting.m_WriteMCU == 1)
                        {
                            RequireWriteMcuEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                        }
                        else
                        {
                            RequireReadBqDeviceInfo();//请求读取博强设备信息
                        }
                    }
                }
            }
            else
            {
                RefreshResult("失败", false);
                if(operateType == OperationTypeEnum.一键出厂配置)
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), operateType,OperationResultEnum.失败, "读取故障记录失败，Eeprom可能损坏，请检查！");
                }
                else
                {
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "读取故障记录失败，Eeprom可能损坏，请检查！");
                }
                ShowMessage("读取故障记录失败，Eeprom可能损坏，请检查！", false);
                BqProtocol.BqInstance.m_bIsOneClick = false;
            }
        }
        public void WriteEepromOver(bool isOK)
        {
            if (isOK)
            {
                //AutoClosedMsgBox.Show("写入Eeprom参数成功！", "写入Eeprom提示", 500, 64);
                ShowMessage("写入Eeprom参数成功！", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.Eeprom写入, OperationResultEnum.成功, string.Empty);
                Thread.Sleep(100);
                ////写入MCU
                if (OneClickFactorySetting.m_WriteMCU == 1)
                {
                    RequireWriteMcuEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                }
                else
                {
                    ////部件型号校验
                    RequireReadBootMsgEvent?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                RefreshResult("失败", false);
                //MessageBox.Show("写入Eeprom参数失败！", "写入Eeprom提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowMessage("写入Eeprom参数失败！", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.Eeprom写入, OperationResultEnum.失败, "写入Eeprom参数失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
            }
        }

        public void WriteMcuOver(bool isOK)
        {
            if (isOK)
            {
                //AutoClosedMsgBox.Show("写入MCU参数成功！", "写入MCU提示", 500, 64);
                ShowMessage("写入MCU参数成功！", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.MCU写入, OperationResultEnum.成功, string.Empty);
                Thread.Sleep(200);
                ////部件型号校验
                RequireReadBootMsgEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RefreshResult("失败", false);
                //MessageBox.Show("写入MCU参数失败！", "写入MCU提示", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowMessage("写入MCU参数失败！", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.MCU写入, OperationResultEnum.失败, "写入MCU参数失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
            }
        }

        public void CheckBqDeviceOver()
        {
            Thread.Sleep(200);
            if (OneClickFactorySetting.m_VoltageCheck == 1)
            {
                ////电压核验
                bool isVoltageDiffNG = false;
                int index = 0;

                foreach (var item in m_ListCellVoltage)
                {
                    index++;
                    if (item.Description != "电池包电压" && item.Description != "实时电流" && index <= 16)
                    {
                        double val = double.Parse(item.StrValue);
                        if (Math.Abs(Math.Abs(val) - Math.Abs(OneClickFactorySetting.m_VoltageBase)) > Math.Abs(OneClickFactorySetting.m_VoltageError))
                        {
                            isVoltageDiffNG = true;
                        }
                    }
                }

                if (isVoltageDiffNG)
                {
                    RefreshResult("失败", false);
                    ShowMessage("电芯电压核验失败，压差过大！", false);
                    BqProtocol.BqInstance.m_bIsOneClick = false;
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.失败, "电芯电压核验失败，压差过大！");
                    return;
                }
                else
                {
                    ShowMessage("电芯电压核验成功！", true);
                }
            }
            Thread.Sleep(100);
            double temperature = 0, humidity = 0;
            if (OneClickFactorySetting.m_TemperatureCheck == 1)
            {
                if (XmlHelper.m_strIsUsingTH10SB == "1")
                {
                    if (MainWindow.th10sbSerialPort != null)
                    {
                        if (MainWindow.th10sbSerialPort.IsOpen)
                        {
                            ReadTH10SBVaule(out temperature, out humidity);
                            if (temperature != 0)
                            {
                                bool isTempDiffNG = false;
                                foreach (var item in m_ListBmsInfo)
                                {
                                    if (item.Description == "环境温度" || item.Description == "电芯温度1" || item.Description == "电芯温度2" || item.Description == "电芯温度3"
                                        || item.Description == "MOS温度1")
                                    {
                                        double val = double.Parse(item.StrValue);
                                        if (Math.Abs(Math.Abs(val - temperature)) > Math.Abs(OneClickFactorySetting.m_TemperatureError))
                                        {
                                            isTempDiffNG = true;
                                        }
                                    }
                                }
                                ////温度核验
                                if (isTempDiffNG)
                                {
                                    RefreshResult("失败", false);
                                    ShowMessage("电芯温度核验失败，温差过大！", false);
                                    BqProtocol.BqInstance.m_bIsOneClick = false;
                                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                                    return;
                                }
                                else
                                {
                                    ShowMessage("电芯温度检验成功！", true);
                                }
                            }
                        }
                    }
                }
                else
                {
                    bool isTempDiffNG = false;
                    var it = m_ListBmsInfo.FirstOrDefault(p => p.Description == "环境温度");
                    temperature = float.Parse(it.StrValue);
                    foreach (var item in m_ListBmsInfo)
                    {
                        if (item.Description == "环境温度" || item.Description == "电芯温度1" || item.Description == "电芯温度2" || item.Description == "电芯温度3"
                            || item.Description == "MOS温度1")
                        {
                            double val = double.Parse(item.StrValue);
                            if (Math.Abs(Math.Abs(val - temperature)) > Math.Abs(OneClickFactorySetting.m_TemperatureError))
                            {
                                isTempDiffNG = true;
                            }
                        }
                    }
                    ////温度核验
                    if (isTempDiffNG)
                    {
                        RefreshResult("失败", false);
                        ShowMessage("电芯温度核验失败，温差过大！", false);
                        BqProtocol.BqInstance.m_bIsOneClick = false;
                        SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.失败, "电芯温度核验失败，温差过大！");
                        return;
                    }
                    else
                    {
                        ShowMessage("电芯温度检验成功！", true);
                    }
                }
            }
            Thread.Sleep(100);
            var outResistance = m_ListBmsInfo.FirstOrDefault(p => p.Description == "外包进水阻抗");
            int outResistanceVal = Int32.Parse(outResistance.StrValue);
            if (Math.Abs(outResistanceVal) < Math.Abs(OneClickFactorySetting.m_minOutWaterResistance)
                || Math.Abs(outResistanceVal ) > Math.Abs(OneClickFactorySetting.m_maxOutWaterResistance))
            {
                RefreshResult("失败", false);
                ShowMessage("外包进水阻抗检验失败，外包进水阻抗不在范围内 ！", false);
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
                return;
            }
            else
            {
                ShowMessage("外包进水阻抗检验成功！", true);
            }
            var innerResistance = m_ListBmsInfo.FirstOrDefault(p => p.Description == "内包进水阻抗");
            int innerResistanceVal = Int32.Parse(innerResistance.StrValue);
            if (Math.Abs(innerResistanceVal) < Math.Abs(OneClickFactorySetting.m_minInnerWaterResistance)
                || Math.Abs(innerResistanceVal) > Math.Abs(OneClickFactorySetting.m_maxInnerWaterResistance))
            {
                RefreshResult("失败", false);
                ShowMessage("内包进水阻抗检验失败，内包进水阻抗不在范围内 ！", false);
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
                return;
            }
            else
            {
                ShowMessage("内包进水阻抗检验成功！", true);
            }

            //湿度核验
            if (OneClickFactorySetting.m_HumidityCheck == 1)
            {
                if (XmlHelper.m_strIsUsingTH10SB == "1")
                {
                    if (humidity != 0)
                    {
                        string humidityStr = m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue;
                        double val = double.Parse(humidityStr);
                        if (Math.Abs(val - humidity) > OneClickFactorySetting.m_HumidityError)
                        {
                            RefreshResult("失败", false);
                            ShowMessage("电芯湿度核验失败，湿度差过大！", false);
                            BqProtocol.BqInstance.m_bIsOneClick = false;
                            SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.失败, "电芯湿度核验失败，湿度差过大！");
                            return;
                        }
                        else
                        {
                            ShowMessage("电芯湿度检验成功！", true);
                        }
                    }
                }
            }
            Thread.Sleep(100);
            if(isOneClickFactoryConfig)
                RequireAdjustRTCEvent?.Invoke(this, EventArgs.Empty);
            else
            {
                if(isOneClickCheck)
                {
                    //放电循环次数检查
                    if (isLoopNumberIsOK)
                    {
                        ShowMessage("放电循环次数检验成功！", true);
                    }
                    else
                    {
                        RefreshResult("失败", false);
                        ShowMessage("放电循环次数检验失败，放电循环次数不为 0 ！", false);
                        SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "放电循环次数检验失败，放电循环次数不为 0 ！");
                        BqProtocol.BqInstance.m_bIsOneClick = false;
                        isOneClickCheck = false;
                        return;
                    }
                    Thread.Sleep(100);
                    //RTC检验
                    RequireReadRTCEvent?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void AdjustRTCOver(bool isOK)
        {
            if (isOK)
            {
                ShowMessage("校准RTC成功！", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.RTC校准, OperationResultEnum.成功, string.Empty);
                Thread.Sleep(100);
                ////零点校准
                if (OneClickFactorySetting.m_ZeroAdjust == 1)
                {
                    preAdjustCurrentVal = tbCurrent.Text.Trim();
                    RequireAdjustZeroCurrentEvent?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ////-10A校准
                    if (OneClickFactorySetting.m_Current10AAdjust == 1)
                    {
                        if (SelectCANWnd.m_IsUsingKP182)//如果启用KP182
                        {
                            if ((MainWindow.serialPort != null) && (MainWindow.serialPort.IsOpen == true))
                            {
                                SetKP182CurrentMode(MainWindow.serialPort,10000);//设置KP182拉载模式为CC模式
                            }
                        }
                        else
                        {
                            if (MessageBoxResult.OK == MessageBox.Show("请开启负载设备进行放电，配置完成后再点击确定！", "-10A校准提示", MessageBoxButton.OKCancel, MessageBoxImage.Information))
                            {
                                preAdjustCurrentVal = tbCurrent.Text.Trim();
                                RequireAdjust10ACurrentEvent?.Invoke(this, EventArgs.Empty);
                            }
                            else
                            {
                                Thread.Sleep(200);
                                RefreshResult("完成", true);
                                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.成功, string.Empty);
                                //////浅休眠
                                if (OneClickFactorySetting.m_ShallowSleep == 1)
                                {
                                    btnShallowSleep_Click(null, null);
                                }
                                else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                                {
                                    btnDeepSleep_Click(null, null);
                                }
                                BqProtocol.BqInstance.m_bIsOneClick = false;
                                CSVFileHelper.WriteLogs("log", "一键出厂配置", "一键出厂配置完成");
                                ShowMessage("一键出厂配置完成！", true);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(200);
                        RefreshResult("完成", true);
                        SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.成功, string.Empty);
                        //////浅休眠
                        if (OneClickFactorySetting.m_ShallowSleep == 1)
                        {
                            btnShallowSleep_Click(null, null);
                        }
                        else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                        {
                            btnDeepSleep_Click(null, null);
                        }
                        BqProtocol.BqInstance.m_bIsOneClick = false;
                        CSVFileHelper.WriteLogs("log", "一键出厂配置", "一键出厂配置完成");
                        ShowMessage("一键出厂配置完成！", true);
						Thread.Sleep(1000);
                        AutoChargeOrDischarge();
                    }
                }
            }
            else
            {
                RefreshResult("失败", false);
                ShowMessage("校准RTC失败！", false);
                BqProtocol.BqInstance.m_bIsOneClick = false;
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.RTC校准, OperationResultEnum.失败, "校准RTC失败！");
            }
        }

        public void AdjustZeroCurrentOver(bool isOK)
        {
            if (isOK)
            {
                ShowMessage("校准零点电流成功！", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.成功, string.Empty);
                Thread.Sleep(200);
                ////-10A校准
                if (OneClickFactorySetting.m_Current10AAdjust == 1)
                {
                    if(SelectCANWnd.m_IsUsingKP182)//如果启用KP182
                    {
                        if((MainWindow.serialPort != null) && (MainWindow.serialPort.IsOpen == true))
                        {
                            //CSVFileHelper.WriteLogs("log", "一键出厂配置", "开启电池负载！");
                            SetKP182CurrentMode(MainWindow.serialPort,10000);//设置KP182拉载模式为CC模式
                        }
                    }
                    else
                    {
                        if (MessageBoxResult.OK == MessageBox.Show("请开启负载设备进行放电，配置完成后再点击确定！", "-10A校准提示", MessageBoxButton.OKCancel, MessageBoxImage.Information))
                        {
                            preAdjustCurrentVal = tbCurrent.Text.Trim();
                            RequireAdjust10ACurrentEvent?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            Thread.Sleep(200);
                            RefreshResult("完成", true);
                            SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.成功, string.Empty);
                            //////浅休眠
                            if (OneClickFactorySetting.m_ShallowSleep == 1)
                            {
                                btnShallowSleep_Click(null, null);
                            }
                            else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                            {
                                btnDeepSleep_Click(null, null);
                            }
                            BqProtocol.BqInstance.m_bIsOneClick = false;
                            CSVFileHelper.WriteLogs("log", "一键出厂配置", "一键出厂配置完成");
                            ShowMessage("一键出厂配置完成！", true);
                            Thread.Sleep(1000);
                            AutoChargeOrDischarge();
                        }
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    RefreshResult("完成", true);
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.成功, string.Empty);
                    //////浅休眠
                    if (OneClickFactorySetting.m_ShallowSleep == 1)
                    {
                        btnShallowSleep_Click(null, null);
                    }
                    else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                    {
                        btnDeepSleep_Click(null, null);
                    }
                    BqProtocol.BqInstance.m_bIsOneClick = false;
                    CSVFileHelper.WriteLogs("log", "一键出厂配置", "一键出厂配置完成");
                    ShowMessage("一键出厂配置完成！", true);
					
					 Thread.Sleep(1000);
                     AutoChargeOrDischarge();
                }
            }
            else
            {
                RefreshResult("失败", false);
                ShowMessage("校准零点电流失败！", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.失败, "校准零点电流失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
            }
        }

        public void Adjust10ACurrentOver(bool isOK)
        {
            if (isOK)
            {
                ShowMessage("校准-10A电流成功！", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.负10A校准, OperationResultEnum.成功, string.Empty);
                if (SelectCANWnd.m_IsUsingKP182 == true)
                {
                    if ((MainWindow.serialPort != null) && (MainWindow.serialPort.IsOpen == true))
                    {
                        SetKP182CurrentSwitchOFF(MainWindow.serialPort);//关闭KP182拉载开关
                    }
                }
                Thread.Sleep(200);
                RefreshResult("完成", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂配置, OperationResultEnum.成功, string.Empty);
                //////浅休眠
                if (OneClickFactorySetting.m_ShallowSleep == 1)
                {
                    btnShallowSleep_Click(null, null);
                }
                else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                {
                    btnDeepSleep_Click(null, null);
                }
                BqProtocol.BqInstance.m_bIsOneClick = false;
                CSVFileHelper.WriteLogs("log", "一键出厂配置", "一键出厂配置完成");
                ShowMessage("一键出厂配置完成！", true);
				
				Thread.Sleep(1000);
                AutoChargeOrDischarge();
            }
            else
            {
                RefreshResult("失败", false);
                ShowMessage("校准-10A电流失败！", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.负10A校准, OperationResultEnum.失败, "校准零点电流失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
            }
        }
        #endregion


        public event EventHandler RequirePowerOnEvent;
        private void btnPowerON_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.上电;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }
        public event EventHandler RequirePowerOffEvent;
        private void btnPowerOFF_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.下电;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        public void AutoStartOneClickCheck()
        {
            Thread.Sleep(100);
            btnOneClickFactoryCheck_Click(null, null);
        }
        #region 一键出厂校验
        public event EventHandler<EventArgs<string>> RequireCheckEepromEvent;
        //public event EventHandler<EventArgs<bool>> OneClickEvent;
        bool isOneClickCheck = false;

        private void btnOneClickFactoryCheck_Click(object sender, RoutedEventArgs e)
        {
            InitInterfaceShow();
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.一键出厂检验;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                //MessageBoxForm.Show("系统未连接，请先连接设备再进行操作！", "提示", 1000);
                ShowMessage("系统未连接，请先连接设备再进行操作！", false);
            }
        }

        DateTime? Rtc;
        public void ReadRTCOver(string dtStr)
        {
            try
            {
                DateTime dt = DateTime.Parse(dtStr);
                long nowTicks = DateTime.Now.Ticks;
                long rtcTicks = dt.Ticks;
                long tsTicks = Math.Abs(nowTicks - rtcTicks);
                TimeSpan ts = new TimeSpan(tsTicks);
                TimeSpan synTS = new TimeSpan(0, 0, OneClickFactorySetting.m_SynTimeSpan);
                Rtc = dt;
                long synTicks = synTS.Ticks;
                if (tsTicks > synTicks)
                {
                    RefreshResult("失败", false);
                    ShowMessage("RTC检验失败，RTC偏差过大！", false);
                    SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "RTC检验失败，RTC偏差过大！");
                    BqProtocol.BqInstance.m_bIsOneClick = false;
                    isOneClickCheck = false;
                    return;
                }
                else
                {
                    ShowMessage("RTC检验成功！", true);
                    Thread.Sleep(200);
                    if (OneClickFactorySetting.m_EepromCheck == 1)
                    {
                        CSVFileHelper.WriteLogs("log", "Eeprom", "请求检查Eeprom");
                        RequireCheckEepromEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_EepromFilePath.Trim()));
                    }
                    else
                    {
                        if (OneClickFactorySetting.m_McuCheck == 1)
                        {
                            CSVFileHelper.WriteLogs("log", "MCU", "请求检查MCU");
                            RequireCheckMCUEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                        }
                        else
                        {
                            Thread.Sleep(200);
                            //SOC校准
                            if (OneClickFactorySetting.m_SOCAdjust == 1)
                            {
                                RequireAdjustSOCEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_SOCValue.ToString()));
                            }
                            else
                            {
                                Thread.Sleep(200);
                                RefreshResult("完成", true);

                                //////浅休眠
                                if (OneClickFactorySetting.m_ShallowSleep == 1)
                                {
                                    btnShallowSleep_Click(null, null);
                                }
                                else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                                {
                                    btnDeepSleep_Click(null, null);
                                }
                                BqProtocol.BqInstance.m_bIsOneClick = false;
                                isOneClickCheck = false;
                                ShowMessage("一键出厂检验完成！", true);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                SaveOperationRecord(null, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, string.Format("RTC解析异常，RTC值为 {0}！", dtStr));
                MessageBox.Show(ex.Message);
            }

        }
        public event EventHandler<EventArgs<string>> RequireCheckMCUEvent;
        public void CheckEepromParamOK(bool isOK)
        {
            if(isOK)
            {
                ShowMessage("Eeprom参数检验成功！", true);
                CSVFileHelper.WriteLogs("log", "Eeprom", "Eeprom检查完成");
                Thread.Sleep(200);
                if (OneClickFactorySetting.m_McuCheck == 1)
                {
                    CSVFileHelper.WriteLogs("log", "MCU", "请求检查MCU");
                    RequireCheckMCUEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_MCUFilePath.Trim()));
                }
                else
                {
                    Thread.Sleep(200);
                    if (OneClickFactorySetting.m_SOCAdjust == 1)
                    {
                        RequireAdjustSOCEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_SOCValue.ToString()));
                        //isCheckOrSetting = true;
                    }
                    else
                    {
                        Thread.Sleep(200);
                        RefreshResult("完成", true);
                        //////浅休眠
                        if (OneClickFactorySetting.m_ShallowSleep == 1)
                        {
                            btnShallowSleep_Click(null, null);
                        }
                        else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                        {
                            btnDeepSleep_Click(null, null);
                        }
                        BqProtocol.BqInstance.m_bIsOneClick = false;
                        isOneClickCheck = false;
                        SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.成功, string.Empty);
                        CSVFileHelper.WriteLogs("log", "一键出厂检验", "一键出厂检验完成");
                        ShowMessage("一键出厂检验完成！", true);
                        Rtc = null;
                        CheckMCUTime = null;
                    }
                }
            }
            else
            {
                RefreshResult("失败", false);
                ShowMessage("Eeprom参数检验失败，请检查！", false);
                SaveOperationRecord(null, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "Eeprom参数检验失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
            }
        }

        DateTime? CheckMCUTime;
        public void CheckMCUParamOK(bool isOK)
        {
            if (isOK)
            {
                CSVFileHelper.WriteLogs("log", "MCU", "MCU检查完成");
                CheckMCUTime = DateTime.Now;
                ShowMessage("MCU参数检验成功！", true);
                //SOC校准
                if (OneClickFactorySetting.m_SOCAdjust == 1)
                {
                    RequireAdjustSOCEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_SOCValue.ToString()));
                    //isCheckOrSetting = true;
                }
                else
                {
                    Thread.Sleep(200);
                    RefreshResult("完成", true);

                    //////浅休眠
                    if (OneClickFactorySetting.m_ShallowSleep == 1)
                    {
                        btnShallowSleep_Click(null, null);
                    }
                    else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                    {
                        btnDeepSleep_Click(null, null);
                    }
                    BqProtocol.BqInstance.m_bIsOneClick = false;
                    isOneClickCheck = false;
                    SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.成功, string.Empty);
                    CSVFileHelper.WriteLogs("log", "一键出厂检验", "一键出厂检验完成");
                    ShowMessage("一键出厂检验完成！", true);
                    Rtc = null;
                    CheckMCUTime = null;
                }
            }
            else
            {
                RefreshResult("失败", false);
                ShowMessage("MCU参数检验失败，请检查！", false);
                SaveOperationRecord(null, OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "MCU参数检验失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
            }
        }

        public void AlterSOCOver(bool isOK)
        {
            if (isOK)
            {
                ShowMessage("校准SOC成功！", true);
                Thread.Sleep(200);
                RefreshResult("完成", true);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.成功, string.Empty);
                //////浅休眠
                if (OneClickFactorySetting.m_ShallowSleep == 1)
                {
                    btnShallowSleep_Click(null, null);
                }
                else if (OneClickFactorySetting.m_DeepSleep == 1)//深休眠
                {
                    btnDeepSleep_Click(null, null);
                }
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
                SaveOperationRecord(string.Empty, OperationTypeEnum.一键出厂检验, OperationResultEnum.成功, string.Empty);
                CSVFileHelper.WriteLogs("log", "一键出厂检验", "一键出厂检验完成");
                ShowMessage("一键出厂检验完成！", true);
                Rtc = null;
                CheckMCUTime = null;
            }
            else
            {
                RefreshResult("失败", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.一键出厂检验, OperationResultEnum.失败, "校准SOC失败！");
                ShowMessage("校准SOC失败！", false);
                SaveOperationRecord(CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.失败, "校准SOC失败！");
                BqProtocol.BqInstance.m_bIsOneClick = false;
                isOneClickCheck = false;
            }
        }

        #endregion

        void RefreshResult(string content, bool flag)
        {
            Dispatcher.Invoke(new Action(()=>
            {
                labResult.Content = content;
                if (flag)
                {
                    labResult.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    labResult.Background = new SolidColorBrush(Colors.LightGray);
                }
            }));
        }
        #region  使用电池负载KP182
        private bool ReadAndWriteData(SerialPort serial, byte[] cmdBytes ,byte[] recvBytes)
        {
            if (serial != null )
            {
                if(!serial.IsOpen)
                {
                    ShowMessage(string.Format("串口{0}打开失败，请检查！", serial.PortName), false);
                    return false;
                }
            }
            else
            {
                ShowMessage(string.Format("串口未打开，请检查！", serial.PortName), false);
                return false;
            }
            try
            {
                serial.DiscardInBuffer();

                serial.Write(cmdBytes, 0, cmdBytes.Length);

                Thread.Sleep(300);
                for (int nRead = 0; nRead < 3; nRead++)
                {
                    serial.DiscardOutBuffer();
                    int nRdCount = serial.Read(recvBytes, 0, recvBytes.Length);

                    if (nRdCount == recvBytes.Length)
                    {
                        return true;
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex) 
            {
                serial.Close();
                if (isOneClickFactoryConfig) 
                    isOneClickFactoryConfig = false;
                RefreshResult("失败", false);
                ShowMessage("电池负载操作超时！", false);
                BqProtocol.BqInstance.m_bIsOneClick = false;
                MessageBox.Show(ex.Message);
            }
            return false;
        }


        byte deviceAddress = 0x01;
        int currentValue = 0;
        int waitTime = 0;
        int currentError = 0;

        private void SetKP182CurrentMode(SerialPort serial, int currentVal)
        {
            try
            {
                byte[] cmdBytes = new byte[] { deviceAddress, 0x06, 0x01, 0x10, 0x00, 0x01, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 };
                byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
                cmdBytes[cmdBytes.Length - 1] = crc16[1];
                cmdBytes[cmdBytes.Length - 2] = crc16[0];
                byte[] recvBytes = new byte[cmdBytes.Length - 4];
                if (ReadAndWriteData(serial, cmdBytes, recvBytes))
                {
                    if (recvBytes[1] == cmdBytes[1] && recvBytes[2] == cmdBytes[2] && recvBytes[3] == cmdBytes[3])
                    {
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 设置CC模式！", serial.PortName));
                        //MessageBox.Show("设置拉载模式为CC模式成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        SetKP182CurrentValue(serial, currentVal);
                    }
                    else
                    {
                        //MessageBox.Show("设置拉载模式为CC模式失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        ShowMessage("设置拉载模式为CC模式失败！", false);
                    }
                }
            }
            catch(Exception ex)
            {
                serial.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void SetKP182CurrentValue(SerialPort serial, int currentVal)
        {
            try
            {
                byte[] valBytes = BitConverter.GetBytes(currentVal);
                byte[] cmdBytes = new byte[] { deviceAddress, 0x06, 0x01, 0x16, 0x00, 0x01, 0x04 };
                cmdBytes = cmdBytes.Concat(valBytes.Reverse()).ToArray();
                cmdBytes = cmdBytes.Concat(new byte[] { 0x00, 0x00 }).ToArray();
                byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
                cmdBytes[cmdBytes.Length - 1] = crc16[1];
                cmdBytes[cmdBytes.Length - 2] = crc16[0];
                byte[] recvBytes = new byte[cmdBytes.Length - 4];
                //CSVFileHelper.WriteLogs("log", "充放电", "向184发送设置充放电电压值指令");
                if (ReadAndWriteData(serial, cmdBytes, recvBytes))
                {
                    if (recvBytes[1] == cmdBytes[1] && recvBytes[2] == cmdBytes[2] && recvBytes[3] == cmdBytes[3])
                    {
                        //MessageBox.Show("设置拉载电流10000mA成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 设置值 {1}！", serial.PortName,currentVal));
                        SetKP182CurrentSwitchON(serial,waitTime);
                    }
                    else
                    {
                        //MessageBox.Show(string.Format("设置拉载电流 {0} mA失败！", currentVal), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        ShowMessage(string.Format("设置拉载电流 {0} mA失败！", currentVal), false);
                    }
                }
            }
            catch (Exception ex)
            {
                serial.Close();
                MessageBox.Show(ex.Message);
            }
        }
        bool flag = false;
        bool isCharge = false;
        private void SetKP182CurrentSwitchON(SerialPort serial,int waittime)
        {
            try
            {
                byte[] cmdBytes = new byte[] { deviceAddress, 0x06, 0x01, 0x0E, 0x00, 0x01, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 };
                byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
                cmdBytes[cmdBytes.Length - 1] = crc16[1];
                cmdBytes[cmdBytes.Length - 2] = crc16[0];
                byte[] recvBytes = new byte[cmdBytes.Length - 4];
                if (ReadAndWriteData(serial, cmdBytes, recvBytes))
                {
                    if (recvBytes[1] == cmdBytes[1] && recvBytes[2] == cmdBytes[2] && recvBytes[3] == cmdBytes[3])
                    {
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 开关开启！", serial.PortName));
                        if (isChargeOrDischargeTest)
                        {
                            //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 进入充放电等待，时间 {1}！", serial.PortName,waittime));
                            isChargeOrDischargeTest = false;
                            LoadingWnd wnd = new LoadingWnd(waittime);
                            wnd.ShowDialog();

                            var item = m_ListCellVoltage.FirstOrDefault(p => p.Description == "实时电流");
                            if (item != null)
                            {
                                int value = 0;
                                if (int.TryParse(item.StrValue, out value))
                                {
                                    if (Math.Abs(Math.Abs(value) - Math.Abs(currentValue)) < Math.Abs(currentError))
                                    {
                                        if(isCharge)
                                        {
                                            ShowMessage("充放电测试完成！", true);
                                            RefreshResult("合格", true);
                                            SetKP182CurrentSwitchOFF(serial);
                                            //BqProtocol.BqInstance.m_bIsStopCommunication = true;
                                            //Thread.Sleep(600);
                                            //RequireAdjustSOCEvent?.Invoke(this, new EventArgs<string>(OneClickFactorySetting.m_SOCValue.ToString()));
                                        }
                                        else
                                        {
                                            ShowMessage("充放电测试完成！", true);
                                            RefreshResult("合格", true);
                                            SetKP182CurrentSwitchOFF(serial);
                                        }
                                        RefreshResult("合格", true);
                                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.充放电测试, OperationResultEnum.成功, string.Empty);
                                    }
                                    else
                                    {
                                        RefreshResult("不合格", false);
                                        ShowMessage("充放电测试完成！", true);
                                        SetKP182CurrentSwitchOFF(serial);
                                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.充放电测试, OperationResultEnum.失败, "充放电测试失败！");
                                        return;
                                    }
                                    //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 进入充放电判断完成！", serial.PortName));

                                    if (flag == false)
                                    {
                                        flag = true;
                                        isCharge = true;
                                        Thread.Sleep(300);
                                        if (SelectCANWnd.m_IsUsingKP182_2)
                                        {
                                            if (MainWindow.kp184SerialPort_2 != null && MainWindow.kp184SerialPort_2.IsOpen)
                                            {
                                                //CSVFileHelper.WriteLogs("log", "充放电", string.Format("打开电池负载 {0}！", MainWindow.kp184SerialPort_2.PortName));
                                                isChargeOrDischargeTest = true;
                                                ShowMessage("充放电测试开始！", false);
                                                RefreshResult("测试开始", false);
                                                deviceAddress = Convert.ToByte(XmlHelper.m_strKP182DeviceAddress_2, 16);
                                                currentValue = SelectCANWnd.m_CurrentValue_2;
                                                waitTime = SelectCANWnd.m_WaitTime_2;
                                                currentError = SelectCANWnd.m_CurrentError_2;
                                                //CSVFileHelper.WriteLogs("log", "充放电", string.Format("放电电压 {0}，等待时间 {1}，误差{2}！", currentValue.ToString(), waitTime.ToString(), currentError.ToString()));
                                                SetKP182CurrentMode(MainWindow.kp184SerialPort_2, currentValue);
                                            }
                                        }
                                        else
                                        {
                                            ShowMessage("电池负载KP184-02未打开，请启用该设备！", false);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(3000);
                            ReadCurrentMeasureValue(serial);
                        }
                    }
                    else
                    {
                        ShowMessage("设置拉载开关开启失败！", false);
                    }
                }
            }
            catch (Exception ex)
            {
                serial.Close();
                MessageBox.Show(ex.Message);
            }
        }


        private void SetKP182CurrentSwitchOFF(SerialPort serial)
        {
            try
            {
                byte[] cmdBytes = new byte[] { deviceAddress, 0x06, 0x01, 0x0E, 0x00, 0x01, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
                cmdBytes[cmdBytes.Length - 1] = crc16[1];
                cmdBytes[cmdBytes.Length - 2] = crc16[0];
                byte[] recvBytes = new byte[cmdBytes.Length - 4];
                if (ReadAndWriteData(serial, cmdBytes, recvBytes))
                {
                    if (recvBytes[1] == cmdBytes[1] && recvBytes[2] == cmdBytes[2] && recvBytes[3] == cmdBytes[3])
                    {
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 开关关闭！", serial.PortName));
                    }
                    else
                    {
                        ShowMessage("设置拉载开关关闭失败！", false);
                    }
                }
            }
            catch (Exception ex)
            {
                serial.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void ReadCurrentMeasureValue(SerialPort serial)
        {
            try
            {
                byte[] cmdBytes = new byte[] { deviceAddress, 0x03, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00 };
                byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
                cmdBytes[cmdBytes.Length - 1] = crc16[1];
                cmdBytes[cmdBytes.Length - 2] = crc16[0];
                byte[] recvBytes = new byte[23];

                if (ReadAndWriteData(serial, cmdBytes, recvBytes))
                {
                    //CSVFileHelper.WriteLogs("log", "充放电", string.Format("电池负载 {0} 读取数值！", serial.PortName));
                    int currentVal = ((recvBytes[8] << 16) | (recvBytes[9] << 8) | (recvBytes[10]));
                    if (Math.Abs(currentVal - 10000) <= 20)//KP182电压在范围内，请求-10A校准
                    {
                        if (isOneClickFactoryConfig)
                        {
                            preAdjustCurrentVal = tbCurrent.Text.Trim();
                            RequireAdjust10ACurrentEvent?.Invoke(this, EventArgs.Empty);
                            isOneClickFactoryConfig = false;
                        }
                        else
                        {
                            isAdjust10ACurrent = false;
                            preAdjustCurrentVal = tbCurrent.Text.Trim();
                            BqProtocol.BqInstance.AdjustRtCurrent((int)((-10) * Math.Pow(10, 3)));
                            isAdjust10ACurrent = true;
                        }
                        //SetKP182CurrentSwitchOFF();
                    }
                    else
                    {
                        ShowMessage(string.Format("测量电流值 {0} A！ 不能开启放电10A校准！", (currentVal / 1000)), false);
                    }
                }
            }
            catch (Exception ex)
            {
                serial.Close();
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 使用ReHe电压表、电流表
        byte voltmeterAddress = 0x01;
        private void ReadVoltmeterValue(double voltageBase,int error)
        {
            byte[] cmdBytes = new byte[] {voltmeterAddress,0x03, 0x00, 0x23, 0x00, 0x03, 0x00, 0x00};
            byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
            cmdBytes[cmdBytes.Length - 1] = crc16[0];
            cmdBytes[cmdBytes.Length - 2] = crc16[1];//校验位低位在前，高位在后
            byte[] recvBytes = new byte[11];
            if(ReadAndWriteData(MainWindow.voltmeterSerialPort, cmdBytes, recvBytes))
            {
                if (recvBytes[0] == cmdBytes[0] && recvBytes[1] == cmdBytes[1] && recvBytes[2] == 0x06)
                {
                    int point = recvBytes[3];//获取电压值小数点位置
                    int val = (recvBytes[7] << 8) | recvBytes[8];
                    double voltage = (val * Math.Pow(10, 3)) / Math.Pow(10, (4 - point));
                    //if (Math.Abs(voltage - voltageBase * Math.Pow(10, 3)) > error)
                    //{
                    //    resistanceResult.Fill = new SolidColorBrush(Colors.Red);
                    //}
                    //else
                    //{
                    //    resistanceResult.Fill = new SolidColorBrush(Colors.LightGreen);
                    //}
                    AutoClosedMsgBox.Show("内阻测试完成！", "提示", 1000, 64);
                }
                else
                {
                    MessageBox.Show("读取电压表电压数据失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ReadAmperemeterValue(double currentBase, int error)
        {
            byte[] cmdBytes = new byte[] { voltmeterAddress, 0x03, 0x00, 0x23, 0x00, 0x09, 0x00, 0x00 };
            byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
            cmdBytes[cmdBytes.Length - 1] = crc16[0];
            cmdBytes[cmdBytes.Length - 2] = crc16[1];//校验位低位在前，高位在后
            byte[] recvBytes = new byte[23];
            if(ReadAndWriteData(MainWindow.amperemeterSerialPort, cmdBytes, recvBytes))
            {
                if (recvBytes[0] == cmdBytes[0] && recvBytes[1] == cmdBytes[1] && recvBytes[2] == 0x12)
                {
                    int point = recvBytes[4];//获取电流值小数点位置
                    int val = (recvBytes[19] << 8) | recvBytes[20];
                    double current = (val * Math.Pow(10, 3)) / Math.Pow(10, (4 - point));
                    //if (Math.Abs(current - currentBase * Math.Pow(10, 3)) > error)
                    //{
                    //    powerResult.Fill = new SolidColorBrush(Colors.Red);
                    //}
                    //else
                    //{
                    //    powerResult.Fill = new SolidColorBrush(Colors.LightGreen);
                    //}
                    AutoClosedMsgBox.Show("功耗测试完成！", "提示", 1000, 64);
                }
                else
                {
                    MessageBox.Show("读取电流表电流数据失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }



        //private void btnPowerTest_Click(object sender, RoutedEventArgs e)
        //{
        //    double currentBase;
        //    if (double.TryParse(XmlHelper.m_strPowerCurrentBase, out currentBase))
        //    {
        //        int error;
        //        if (int.TryParse(XmlHelper.m_strPowerCurrentError, out error))
        //        {
        //            powerResult.Fill = new SolidColorBrush(Colors.LightGray);
        //            ReadAmperemeterValue(currentBase, error);
        //        }
        //        else
        //        {
        //            MessageBox.Show("功耗测试电流误差解析错误！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("功耗测试电流基数解析错误！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        //private void btnResistanceTest_Click(object sender, RoutedEventArgs e)
        //{
        //    double voltageBase;
        //    if(double.TryParse(XmlHelper.m_strResistanceVoltageBase,out voltageBase))
        //    {
        //        int error;
        //        if(int.TryParse(XmlHelper.m_strResistanceVoltageError,out error))
        //        {
        //            resistanceResult.Fill = new SolidColorBrush(Colors.LightGray);
        //            ReadVoltmeterValue(voltageBase, error);
        //        }
        //        else
        //        {
        //            MessageBox.Show("内阻测试电压误差解析错误！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("内阻测试电压基数解析错误！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        #endregion

        #region  使用TH10S-B温湿度仪
        private void ReadTH10SBVaule(out double temperature,out double humidity)
        {
            byte[] cmdBytes = new byte[] { deviceAddress, 0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00 };
            byte[] crc16 = CRC_Check.CRC16(cmdBytes, 0, cmdBytes.Length - 2);
            cmdBytes[cmdBytes.Length - 1] = crc16[0];
            cmdBytes[cmdBytes.Length - 2] = crc16[1];
            byte[] recvBytes = new byte[9];
            if (ReadAndWriteData(MainWindow.th10sbSerialPort, cmdBytes, recvBytes))
            {
                int tempVal = ((recvBytes[3] << 8) |(recvBytes[4]));
                temperature = tempVal / Math.Pow(10,1);
                
                int humVal = ((recvBytes[5] << 8) | (recvBytes[6]));
                humidity = humVal / Math.Pow(10, 1);
            }
            else
            {
                temperature = 0;
                humidity = 0;
            }
        }
        #endregion

        public void PowerOnOrPowerOffOver(bool isOff)
        {
            if(isOff)
            {
                SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.下电, OperationResultEnum.成功, string.Empty);
                ShowMessage("下电成功！", true);
                Thread.Sleep(300);
            }
            else
            {
                SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.上电, OperationResultEnum.成功, string.Empty);
                ShowMessage("上电成功！", true);
                Thread.Sleep(300);

                if(SelectCANWnd.m_IsUsingKP182)
                {
                    if(MainWindow.serialPort != null && MainWindow.serialPort.IsOpen)
                    {
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("打开电池负载 {0}！", MainWindow.serialPort.PortName));
                        deviceAddress = Convert.ToByte(XmlHelper.m_strKP182DeviceAddress, 16);
                        currentValue = SelectCANWnd.m_CurrentValue;
                        waitTime = SelectCANWnd.m_WaitTime;
                        currentError = SelectCANWnd.m_CurrentError;
                        //CSVFileHelper.WriteLogs("log", "充放电", string.Format("放电电压 {0}，等待时间 {1}，误差{2}！", currentValue.ToString(),waitTime.ToString(),currentError.ToString()));
                        SetKP182CurrentMode(MainWindow.serialPort, currentValue);
                    }
                }
                else
                {
                    //MessageBox.Show("电池负载KP184未打开，请启用该设备！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowMessage("电池负载KP184-01未打开，请启用该设备！", false);
                }
            }
        }
        bool isChargeOrDischargeTest = false;
        private void btnChargeOrDischarge_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.充放电测试;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                ShowMessage("系统未连接，请连接后再进行操作！", false);
            }
        }

        private void AutoChargeOrDischarge()
        {
            Thread.Sleep(100);
            btnChargeOrDischarge_Click(null, null);
        }

        void InitInterfaceShow()
        {
            rtbMsg.Document.Blocks.Clear();
            RefreshResult("待测试", false);
        }

        void ShowMessage(string content, bool flag)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                string msgStr = string.Format("{0}    {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), content);
                Paragraph paragraph = new Paragraph(new Run(msgStr));

                if (flag)
                {
                    paragraph.FontSize = 12;
                    paragraph.Foreground = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    paragraph.FontSize = 14;
                    paragraph.Foreground = new SolidColorBrush(Colors.Red);
                }

                rtbMsg.Document.Blocks.Add(paragraph);
                rtbMsg.ScrollToEnd();
            }));
        }

        public void ShowCheckMCuMsg(bool isOK,string content)
        {
            ShowMessage(content, isOK);
        }

        private void btnBMSRegister_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.m_statusBarInfo.IsOnline)
            {
                operateType = OperationTypeEnum.BMS注册;
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("BmsInfo"));
            }
            else
            {
                ShowMessage("系统未连接，请连接后再进行操作！", false);
            }
        }

        bool isReadBQDevice = false;
        private void btnReadBqDevice_Clicked(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                if (isBqProtocol)
                    BqProtocol.BqInstance.m_bIsStopCommunication = true;
                else
                    DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = true;
                Thread.Sleep(200);
                isReadBQDevice = true;
                isCompanyMatch = false;
                BqProtocol.BqInstance.BQ_ReadDeviceInfo();
            }
            else
            {
                MessageBox.Show("系统未连接，请连接后再进行操作！", "写入 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void HandleRecvBqDeviceInfoEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                if (isCompanyMatch)
                {
                    isCompanyMatch = false;
                    if (isBqProtocol)
                        BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    else
                        DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = false;
                    byte[] array = new byte[16];
                    Buffer.BlockCopy(e.RecvMsg.ToArray(), 36, array, 0, array.Length);
                    string company = System.Text.Encoding.ASCII.GetString(array);
                    int len = company.IndexOf('\0');
                    if (len >= 0)
                        company = company.Substring(0, len);
                    if (company == "BOQIANG" || company == "PBOQIANG")
                        IsCompanyMatchEvent?.Invoke(this, new EventArgs<bool>(true));
                    else
                        IsCompanyMatchEvent?.Invoke(this, new EventArgs<bool>(false));
                }
                else
                {
                    if (isReadBQDevice)
                    {
                        isReadBQDevice = false;
                        if (isBqProtocol)
                            BqProtocol.BqInstance.m_bIsStopCommunication = false;
                        else
                            DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = false;
                        List<string> list = new List<string>();
                        bool flag = false;
                        if (isRequireReadBqDeviceInfo)
                        {
                            flag = true;
                        }
                        else
                        {

                            flag = false;
                        }
                        isRequireReadBqDeviceInfo = false;
                        if (e.RecvMsg[0] != 0xCC || e.RecvMsg[1] != 0xA0 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                        {
                            return;
                        }
                        int offset = 4;
                        foreach (var item in m_ListBqDeviceInfo)
                        {
                            byte[] array = new byte[item.ByteCount];
                            Buffer.BlockCopy(e.RecvMsg.ToArray(), offset, array, 0, array.Length);
                            if (item.Description == "程序所处阶段")
                            {
                                int programStatus = ((array[1] << 8) | (((array[0]))));
                                if (programStatus == 1)
                                    item.StrValue = "APP初始化完成";
                                else if (programStatus == 0)
                                    item.StrValue = "BOOT";
                                else if (programStatus == 2)
                                    item.StrValue = "APP初始化";
                                list.Add(item.StrValue);
                            }
                            else
                            {
                                string val = System.Text.Encoding.ASCII.GetString(array);
                                int len = val.IndexOf('\0');
                                if (len >= 0)
                                    item.StrValue = val.Substring(0, len);
                                else
                                    item.StrValue = val;
                                list.Add(item.StrValue);

                            }
                            offset += item.ByteCount;
                        }
                        if (flag)
                        {
                            if(isOneClickFactoryConfig == true || isOneClickCheck == true)
                            {
                                string project = m_ListBqDeviceInfo.SingleOrDefault(p => p.Description == "项目名称").StrValue;
                                if (OneClickFactorySetting.m_ComponentModel != project)
                                {
                                    RefreshResult("失败", false);
                                    ShowMessage(string.Format("读取的项目名称 {0} 与设置的项目名称 {1} 不一致，请检查！",project,OneClickFactorySetting.m_ComponentModel), false);
                                    isOneClickFactoryConfig = false;
                                    isOneClickCheck = false;
                                    return;
                                }
                                else
                                {
                                    ShowMessage("项目名称检查完成！", true);
                                }
                                string hardwareVersion = m_ListBqDeviceInfo.SingleOrDefault(p => p.Description == "硬件版本").StrValue;
                                if (XmlHelper.m_strHardwareVersion != hardwareVersion)
                                {
                                    RefreshResult("失败", false);
                                    ShowMessage(string.Format("读取的硬件版本号 {0} 与设置的硬件版本号 {1} 不一致，请检查！", hardwareVersion, XmlHelper.m_strHardwareVersion), false);
                                    isOneClickFactoryConfig = false;
                                    isOneClickCheck = false;
                                    return;
                                }
                                else
                                {
                                    ShowMessage("硬件版本号检查完成！", true);
                                }
                                string firmwareVersion = m_ListBqDeviceInfo.SingleOrDefault(p => p.Description == "固件版本").StrValue;
                                if (XmlHelper.m_strSoftwareVersion != firmwareVersion)
                                {
                                    RefreshResult("失败", false);
                                    ShowMessage(string.Format("读取的固件版本号 {0} 与设置的固件版本号 {1} 不一致，请检查！", firmwareVersion, XmlHelper.m_strSoftwareVersion), false);
                                    isOneClickFactoryConfig = false;
                                    isOneClickCheck = false;
                                    return;
                                }
                                else
                                {
                                    ShowMessage("固件版本号检查完成！", true);
                                }
                                if(isOneClickCheck)
                                {
                                    var bmsProducedDate = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "保护板生产日期");
                                    DateTime dt;
                                    if (bmsProducedDate != null)
                                    {
                                        if (bmsProducedDate.StrValue == "2021-01-01")
                                        {
                                            RefreshResult("失败", false);
                                            ShowMessage("保护板生产日期检查为默认初始值2021-01-01，请检查！", false);
                                            isOneClickCheck = false;
                                            return;
                                        }
                                        if (DateTime.TryParse(bmsProducedDate.StrValue, out dt))
                                        {
                                            int days = (DateTime.Now - dt).Days;
                                            if(days > OneClickFactorySetting.m_ProducedDateError)
                                            {
                                                RefreshResult("失败", false);
                                                ShowMessage(string.Format("保护板生产日期超过设置的生产日期误差 {0} 天，请检查！", OneClickFactorySetting.m_ProducedDateError), false);
                                                isOneClickCheck = false;
                                                return;
                                            }
                                            else
                                            {
                                                ShowMessage("保护板生产日期检查完成！", true);
                                            }
                                        }
                                        else
                                        {
                                            RefreshResult("失败", false);
                                            ShowMessage("保护板生产日期解析异常，请检查！", false);
                                            isOneClickCheck = false;
                                            return;
                                        }
                                    }
                                    var bmsID = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "保护板序列号").StrValue;
                                    if(string.IsNullOrEmpty(bmsID))
                                    {
                                        RefreshResult("失败", false);
                                        ShowMessage("保护板序列号为空，请检查！", false);
                                        isOneClickCheck = false;
                                        return;
                                    }
                                    else
                                    {
                                        if(bmsID == "0123456789abcdef")
                                        {
                                            RefreshResult("失败", false);
                                            ShowMessage("读取到的保护板序列号为默认值，请检查！", false);
                                            isOneClickCheck = false;
                                            return;
                                        }
                                        else
                                        {
                                            ShowMessage("保护板序列号检查完成！", true);
                                        }
                                    }
                                }

                                CheckBqDeviceOver();
                            }
                            else
                                GetBqDeviceInfoEvent?.Invoke(this, new EventArgs<List<string>>(list));
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("读取设备信息成功！", "读取设备信息提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        bool isRequireReadBqDeviceInfo = false;
        public void RequireReadBqDeviceInfo()
        {
            isRequireReadBqDeviceInfo = true;
            btnReadBqDevice_Clicked(null, null);
        }

        bool isCompanyMatch = false;
        bool isBqProtocol = true;
        public void IsCompanyMatch(bool flag)
        {
            isBqProtocol = flag;
            if (isBqProtocol)
                BqProtocol.BqInstance.m_bIsStopCommunication = true;
            else
                DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            isCompanyMatch = true;
            BqProtocol.BqInstance.BQ_ReadDeviceInfo();
        }
        public event EventHandler<EventArgs<List<string>>> GetBqDeviceInfoEvent;
        public event EventHandler<EventArgs<bool>> IsCompanyMatchEvent;
        public void HandleRecvWriteBqBMSInfoEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
            if (e.RecvMsg[0] != 0xDD || e.RecvMsg[1] != 0xB0 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
            {
                return;
            }
            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
            if (res == 0)
                MessageBox.Show("写BMS信息成功！", "写 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("写BMS信息失败！", "写 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public void HandleRecvWriteBqPackInfoEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
            if (e.RecvMsg[0] != 0xDD || e.RecvMsg[1] != 0xB1 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
            {
                return;
            }
            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
            if (res == 0)
            {
                MessageBox.Show("写电池包信息成功！", "写 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Information);
                btnReadBqDevice_Clicked(null, null);
            }
            else
                MessageBox.Show("写电池包信息失败！", "写 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void btnWriteBMSDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.m_statusBarInfo.IsOnline == true)
                {
                    List<byte> listbytes = new List<byte>();
                    var bmsDate = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "保护板生产日期");
                    if (bmsDate != null)
                    {
                        DateTime date;
                        if (DateTime.TryParse(bmsDate.StrValue, out date))
                        {
                            byte[] array = System.Text.ASCIIEncoding.ASCII.GetBytes(bmsDate.StrValue);
                            listbytes.AddRange(array);
                            if (array.Length < 16)
                            {
                                for (int i = 0; i < 16 - array.Length; i++)
                                {
                                    listbytes.Add(0x00);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("保护板生产日期格式不正确，请检查！", "写入 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    var bmsID = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "保护板序列号");
                    if (bmsID != null)
                    {
                        if (bmsID.StrValue.Length <= 32)
                        {
                            byte[] array = System.Text.Encoding.ASCII.GetBytes(bmsID.StrValue);
                            listbytes.AddRange(array);
                            if (array.Length < 32)
                            {
                                for (int i = 0; i < 32 - array.Length; i++)
                                {
                                    listbytes.Add(0x00);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("保护板序列号长度超过32位，请检查！", "写入 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    BqProtocol.BqInstance.m_bIsStopCommunication = true;
                    Thread.Sleep(200);
                    BqProtocol.BqInstance.BQ_WriteBMSInfo(listbytes.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void btnWritePackDevice_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.m_statusBarInfo.IsOnline == true)
                {
                    List<byte> listbytes = new List<byte>();
                    var packDate = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "电池包生产日期");
                    if (packDate != null)
                    {
                        DateTime date;
                        if (DateTime.TryParse(packDate.StrValue, out date))
                        {
                            byte[] array = System.Text.ASCIIEncoding.ASCII.GetBytes(packDate.StrValue);
                            listbytes.AddRange(array);
                            if (array.Length < 16)
                            {
                                for (int i = 0; i < 16 - array.Length; i++)
                                {
                                    listbytes.Add(0x00);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("电池包生产日期格式不正确，请检查！", "写入 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    var packID = m_ListBqDeviceInfo.FirstOrDefault(p => p.Description == "电池包序列号");
                    if (packID != null)
                    {
                        if (packID.StrValue.Length <= 32)
                        {
                            byte[] array = System.Text.Encoding.ASCII.GetBytes(packID.StrValue);
                            listbytes.AddRange(array);
                            if (array.Length < 32)
                            {
                                for (int i = 0; i < 32 - array.Length; i++)
                                {
                                    listbytes.Add(0x00);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("电池包序列号长度超过32位，请检查！", "写入 设备信息 提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    BqProtocol.BqInstance.m_bIsStopCommunication = true;
                    Thread.Sleep(200);
                    BqProtocol.BqInstance.BQ_WritePackInfo(listbytes.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "提示", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void dgBqDeviceInfo_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int index = dgBqDeviceInfo.SelectedIndex;
            if (index < 9)
            {
                DataGridCell cell = DataGridExtension.GetCell(dgBqDeviceInfo, index, 1);
                if (cell != null)
                {
                    cell.IsEnabled = false;
                }
            }
        }

        bool isMsgVisible = true;
        bool isReadUID = false;
        private void btnReadUID_Click(object sender, RoutedEventArgs e)
        {
            isReadUID = false;
            if (isRequireReadUID)
                DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = true;
            else
                BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(100);
            BqProtocol.BqInstance.BQ_ReadUID();
            isReadUID = true;
        }
        public void HandleReadUIDEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                if (isReadUID)
                {
                    if (isRequireReadUID)
                        DDProtocol.DdProtocol.DdInstance.m_bIsStopCommunication = false;
                    else
                        BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    isRequireReadUID = false;
                    if (e.RecvMsg[0] != 0xCC || e.RecvMsg[1] != 0xA1 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                    {
                        return;
                    }

                    byte[] array = new byte[16];
                    Buffer.BlockCopy(e.RecvMsg.ToArray(), 4, array, 0, array.Length);
                    tbUID.Text = CSVFileHelper.ToHexStrFromByte(array, true);
                    GetUIDEvent?.Invoke(this, new EventArgs<string>(tbUID.Text));
                    if (isMsgVisible)
                    {
                        MessageBox.Show("读取UID成功！", "读取UID提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    isMsgVisible = true;
                    isReadUID = false;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public event EventHandler<EventArgs<string>> GetUIDEvent;
        bool isRequireReadUID = false;
        public void RequireReadUID()
        {
            isMsgVisible = false;
            isRequireReadUID = true;
            btnReadUID_Click(null, null);
        }

        bool isOverDischarge = false;
        public event EventHandler OverDischargeEvent;
        private void btnOverDischarge_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                isOverDischarge = false;
                BqProtocol.bReadBqBmsResp = true;
                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                Thread.Sleep(200);
                BqProtocol.BqInstance.BQ_OverDischarge();
                isOverDischarge = true;
            }
            else
            {
                MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region 数据库操作
        void SaveOperationRecord(string data, OperationTypeEnum type, OperationResultEnum result, string comments)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    StringBuilder sb = new StringBuilder();
                    string totalVoltage = string.Empty;
                    string current = string.Empty;
                    foreach (var it in m_ListCellVoltage)
                    {
                        if (it.Description == "电池包电压")
                        {
                            totalVoltage = it.StrValue;
                        }
                        else if (it.Description == "实时电流")
                        {
                            current = it.StrValue;
                        }
                        else
                        {
                            sb.Append(it.StrValue);
                            sb.Append("$");
                        }
                    }
                    string cellTemp = string.Format("{0}${1}${2}${3}", m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度1").StrValue,
                        m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度2").StrValue, m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度3").StrValue,
                        m_ListBmsInfo.FirstOrDefault(p => p.Description == "MOS温度1").StrValue);

                    operationrecord record = new operationrecord()
                    {
                        UID = U_ID,
                        ModifiedTime = DateTime.Now,
                        Data = data,
                        OperationID = (sbyte)type,
                        ResultID = (int)result,
                        Comments = comments,
                        TotalVoltage = totalVoltage,
                        Current = current,
                        CellVoltage = sb.ToString().TrimEnd('$'),
                        AmbientTemp = m_ListBmsInfo.FirstOrDefault(p => p.Description == "环境温度").StrValue,
                        CellTemp = cellTemp,
                        Humidity = m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue,
                        RTC = Rtc,
                        MCUCheckTime = CheckMCUTime,
                        LoopNumber = Int32.Parse(m_ListBmsInfo.FirstOrDefault(p => p.Description == "放电循环次数").StrValue),
                        UserID = MainWindow.UserID
                    };
                    eb12.operationrecord.Add(record);
                    eb12.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void SaveOperationRecord(string data, OperationTypeEnum type, OperationResultEnum result, string comments, string pre_currentVal, string adjustVal)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    StringBuilder sb = new StringBuilder();
                    string totalVoltage = string.Empty;
                    string current = string.Empty;
                    foreach (var it in m_ListCellVoltage)
                    {
                        if (it.Description == "电池包电压")
                        {
                            totalVoltage = it.StrValue;
                        }
                        else if (it.Description == "实时电流")
                        {
                            current = it.StrValue;
                        }
                        else
                        {
                            sb.Append(it.StrValue);
                            sb.Append("$");
                        }
                    }
                    string cellTemp = string.Format("{0}${1}${2}${3}", m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度1").StrValue,
                        m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度2").StrValue, m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度3").StrValue,
                        m_ListBmsInfo.FirstOrDefault(p => p.Description == "MOS温度1").StrValue);

                    operationrecord record = new operationrecord()
                    {
                        UID = U_ID,
                        ModifiedTime = DateTime.Now,
                        Data = data,
                        OperationID = (sbyte)type,
                        ResultID = (int)result,
                        Comments = comments,
                        TotalVoltage = totalVoltage,
                        Current = current,
                        CellVoltage = sb.ToString().TrimEnd('$'),
                        AmbientTemp = m_ListBmsInfo.FirstOrDefault(p => p.Description == "环境温度").StrValue,
                        CellTemp = cellTemp,
                        Humidity = m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue,
                        RTC = Rtc,
                        MCUCheckTime = CheckMCUTime,
                        LoopNumber = Int32.Parse(m_ListBmsInfo.FirstOrDefault(p => p.Description == "放电循环次数").StrValue),
                        UserID = MainWindow.UserID,
                        Pre_AdjustCurrentVal = pre_currentVal,
                        AdjustCurrentVal = adjustVal
                    };
                    eb12.operationrecord.Add(record);
                    eb12.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void BMSRegister(string uid)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    var items = eb12.uidrecord.Where(p => p.UID == uid);
                    if (items.Count() != 0)
                    {
                        //MessageBox.Show(string.Format("该BMS的UID {0} 已存在！",uid),"提示",MessageBoxButton.OK,MessageBoxImage.Information);
                        ShowMessage(string.Format("该BMS的UID {0} 已存在！", uid), false);
                        return;
                    }
                    uidrecord record = new uidrecord()
                    {
                        UID = uid,
                        ModifiedTime = DateTime.Now,
                        UserID = MainWindow.UserID
                    };
                    eb12.uidrecord.Add(record);
                    eb12.SaveChanges();
                    ShowMessage("BMS登记注册成功！", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        bool IsUIDExist(string uid)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    var items = eb12.uidrecord.Where(p => p.UID == uid);
                    if (items.Count() == 0)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        bool Binding(string uid, string bmsID)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    if (IsUIDExist(uid))
                    {
                        var item = eb12.uidrecord.FirstOrDefault(p => p.UID == uid);
                        if (string.IsNullOrEmpty(item.BMSID))
                        {
                            item.BMSID = bmsID;
                            item.ModifiedTime = DateTime.Now;
                            eb12.SaveChanges();
                            SaveOperationRecord(string.Empty, OperationTypeEnum.BMS绑定, OperationResultEnum.成功, string.Format("绑定BMS ID为 {0}", item.BMSID));
                            //MessageBox.Show("BMS ID绑定成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            ShowMessage("BMS ID绑定成功！", true);
                        }
                        else
                        {
                            //记录绑定事件
                            SaveOperationRecord(string.Empty, OperationTypeEnum.BMS绑定, OperationResultEnum.成功, string.Format("将绑定的BMS ID由 {0} 更改为 {1}", item.BMSID, bmsID));
                            item.BMSID = bmsID;
                            item.ModifiedTime = DateTime.Now;
                            eb12.SaveChanges();
                            //MessageBox.Show("BMS ID绑定成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            ShowMessage("BMS ID绑定成功！", true);
                        }
                        return true;
                    }
                    else
                    {
                        //MessageBox.Show(string.Format("该BMS的UID {0} 未进行注册，请注册后再进行操作！", uid), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        ShowMessage(string.Format("该BMS的UID {0} 未进行注册，请注册后再进行操作！", uid), false);
                        return false;
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }
        #endregion
    }



    public static class DataGridExtension
    {
        /// <summary>
        /// 获取DataGrid控件单元格
        /// </summary>
        /// <param name="dataGrid">DataGrid控件</param>
        /// <param name="rowIndex">单元格所在的行号</param>
        /// <param name="columnIndex">单元格所在的列号</param>
        /// <returns>指定的单元格</returns>
        public static DataGridCell GetCell(this DataGrid dataGrid, int rowIndex, int columnIndex)
        {
            DataGridRow rowContainer = GetRow(dataGrid, rowIndex);
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
                if (cell == null)
                {
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[columnIndex]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
                }
                return cell;
            }
            return null;
        }

        /// <summary>
        /// 获取DataGrid的行
        /// </summary>
        /// <param name="dataGrid">DataGrid控件</param>
        /// <param name="rowIndex">DataGrid行号</param>
        /// <returns>指定的行号</returns>
        public static DataGridRow GetRow(this DataGrid dataGrid, int rowIndex)
        {
            DataGridRow rowContainer = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            if (rowContainer == null)
            {
                dataGrid.UpdateLayout();
                dataGrid.ScrollIntoView(dataGrid.Items[rowIndex]);
                rowContainer = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            }
            return rowContainer;
        }

        /// <summary>
        /// 获取父可视对象中第一个指定类型的子可视对象
        /// </summary>
        /// <typeparam name="T">可视对象类型</typeparam>
        /// <param name="parent">父可视对象</param>
        /// <returns>第一个指定类型的子可视对象</returns>
        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
    }
}
