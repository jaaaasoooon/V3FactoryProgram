using BoqiangH5.BQProtocol;
using DBService;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Threading;

namespace BoqiangH5
{
    /// <summary>
    /// UserCtrlAdjust.xaml 的交互逻辑
    /// </summary>
    public partial class UserCtrlAdjust : UserControl
    {        
        List<H5BmsInfo> ListAdjustVoltage2 = new List<H5BmsInfo>();

        System.Windows.Threading.DispatcherTimer  timerRTC;

        int nStartAddr = 0xA200;
        int nCellVoltageAddr = 0xA210;

        public UserCtrlAdjust()
        {            
            InitializeComponent();
            InitAdjustWnd();

            UpdateAdjustWndStatus();
        }

        private void InitAdjustWnd()
        { 
            string strConfigFile = XmlHelper.m_strBqProtocolFile;
            XmlHelper.LoadXmlConfig(strConfigFile, "bms_info/cell_votage_info", ListAdjustVoltage2);

            //cbIsRefresh.IsChecked = true;
            SetTimerHandshake();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dgAdjustVoltage2.ItemsSource = ListAdjustVoltage2;

            if (ListAdjustVoltage2.Count > 2)
            {
                DataGridRow row = DataGridExtension.GetRow(dgAdjustVoltage2, 0);
                row.Visibility = Visibility.Collapsed;
                DataGridRow _row = DataGridExtension.GetRow(dgAdjustVoltage2, 1);
                _row.Visibility = Visibility.Collapsed;
            }
        }
        private void SetTimerHandshake()
        {
            timerRTC = new System.Windows.Threading.DispatcherTimer();
            timerRTC.Tick += new EventHandler(OnTimedHandshakeEvent);
            timerRTC.Interval = new TimeSpan(0, 0, 1);
            timerRTC.Start();
        }

        //bool isFirst = true;
         private void OnTimedHandshakeEvent(Object sender, EventArgs e)
         {
            tbCurrentTime.Text = DateTime.Now.ToString();

            if (cbIsRefresh.IsChecked == true && MainWindow.m_statusBarInfo.IsOnline == true)
            {
                if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                    BqProtocol.BqInstance.BQ_ReadRTC();
            }
        }
        bool isReadRTC = false;
        public void RequireReadRTC()
        {
            isReadRTC = true;
            BqProtocol.BqInstance.BQ_ReadRTC();
        }

        private void UpdateAdjustWndStatus()
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                if(SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                {
                    btnAdjustCellVol.IsEnabled = true;
                    btnAdjustBatteryVol.IsEnabled = true;
                    btnAdjustLoadVol.IsEnabled = true;
                    btnAdRtCurrent.IsEnabled = true;
                    btnAdZeroCurrent.IsEnabled = true;
                    btnAdjustTemp.IsEnabled = true;
                    btnAdjustRTC.IsEnabled = true;
                    btnAdjustInnerResistance.IsEnabled = true;
                    btnAdjustOutResistance.IsEnabled = true;
                }    
            }
            else
            {
                btnAdjustCellVol.IsEnabled = false;
                btnAdjustBatteryVol.IsEnabled = false;
                btnAdjustLoadVol.IsEnabled = false;
                btnAdRtCurrent.IsEnabled = false;
                btnAdZeroCurrent.IsEnabled = false;
                btnAdjustTemp.IsEnabled = false;
                btnAdjustRTC.IsEnabled = false;
                btnAdjustInnerResistance.IsEnabled = false;
                btnAdjustOutResistance.IsEnabled = false;
            }

        }

        public void HandleAdjustWndUpdateEvent(object sender, EventArgs e)
        {
            //if (SelectCANWnd.m_H5Protocol == H5Protocol.DI_DI)
            //{
            //    if (MainWindow.m_statusBarInfo.IsOnline)
            //    {
            //        btnAdjustRTC.IsEnabled = true;
            //    }
            //    return;
            //}

            UpdateAdjustWndStatus();
        }

        public void HandleRecvBmsInfoDataEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
            {
                if (string.IsNullOrEmpty(tbRtc.Text.Trim()))
                {
                    BqProtocol.BqInstance.BQ_ReadRTC();
                }
                BqUpdateCellVoltage(e.RecvMsg);
            }
            else
            {
                ////if (string.IsNullOrEmpty(tbRtc.Text.Trim()))
                ////{
                ////    DDProtocol.DdProtocol.DdInstance.Didi_ReadRTC();
                ////}
                DdUpdateCellVoltage(e.RecvMsg);
            }
        }

        private void DdUpdateCellVoltage(List<byte> listRecv)
        {
            if(listRecv.Count != 0xCE)
            {
                return;
            }

            int nDdByteIndex = (nCellVoltageAddr - nStartAddr) * 2;

            for (int n = 0; n < 16; n++)  // for (int n = 16; n < 32; n++)
            {
                ListAdjustVoltage2[n].StrValue = ((listRecv[nDdByteIndex] << 8) | listRecv[nDdByteIndex + 1]).ToString();
                nDdByteIndex += 2;
            }
            
        }


        private void BqUpdateCellVoltage(List<byte> listRecv)
        {
            if (listRecv[0] != 0xCC || listRecv[1] != 0xA2)
            {
                return;
            }
            if (listRecv.Count < (listRecv[2] << 8 | listRecv[3]))
            {
                return;
            }

            int nBqByteIndex = 4;

            for (int n = 0; n < ListAdjustVoltage2.Count; n++)
            {
                int nCellVol = 0;
                for (int m = ListAdjustVoltage2[n].ByteCount - 1; m >= 0; m--)
                {
                    nCellVol = (nCellVol << 8 | listRecv[nBqByteIndex + m]);
                }
                ListAdjustVoltage2[n].StrValue = nCellVol.ToString();

                nBqByteIndex += ListAdjustVoltage2[n].ByteCount;
            }

            //tbTotalVoltage.Text = ListAdjustVoltage2[1].StrValue;
            tbCurrent.Text = ListAdjustVoltage2[0].StrValue;

            int temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp1.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp2.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp3.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp4.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp5.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbCellTemp6.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbAmbientTemp.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbMosTemp1.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            temp = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbMosTemp2.Text = ((temp - 2731) / 10).ToString();
            nBqByteIndex += 2;
            int resistance = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbInnerResistance.Text = resistance.ToString();
            nBqByteIndex += 2;
            resistance = (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbOutResistance.Text = resistance.ToString();
            nBqByteIndex += 4;
            int voltage = (listRecv[nBqByteIndex + 3] << 24) | (listRecv[nBqByteIndex + 2] << 16) | (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbBatteryVoltage.Text = voltage.ToString();
            nBqByteIndex += 4;
            voltage = (listRecv[nBqByteIndex + 3] << 24) | (listRecv[nBqByteIndex + 2] << 16) | (listRecv[nBqByteIndex + 1] << 8) | listRecv[nBqByteIndex];
            tbLoadVoltage.Text = voltage.ToString();
        }

        bool isAdjustCurrent = false;
        bool isAdjustZeroCurrent = false;
        bool isRequireAdjustZero = false;
        bool isRequireAdjust10A = false;
        public event EventHandler<EventArgs<bool>> AdjustZeroCurrentOverEvent;
        public event EventHandler<EventArgs<bool>> Adjust10ACurrentOverEvent;
        public void RequireAdjustZeroCurrent()
        {
            this.tbZeroCurrent.Text = "0";
            isRequireAdjustZero = true;
            isAdjustZeroCurrent = false;
            BqProtocol.BqInstance.AdjustZeroCurrent(int.Parse(tbZeroCurrent.Text));
            isAdjustZeroCurrent = true;
        }
        public void RequireAdjust10ACurrent()
        {
            this.tbRtCurrent.Text = "-10000";
            isRequireAdjust10A = true;
            isAdjustCurrent = false;
            BqProtocol.BqInstance.AdjustRtCurrent(int.Parse(tbRtCurrent.Text));
            isAdjustCurrent = true;
        }
        private void btnAdjustCurrent_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null)
                return;

            string strPatten = @"^[\-|0-9][0-9]*$";

            if (btn.Name == "btnAdRtCurrent")
            {
                if (!Regex.IsMatch(this.tbRtCurrent.Text, strPatten))
                {
                    MessageBox.Show("请输入正确的电流值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                isAdjustRTC = false;
                isAdjustCurrent = true;
                isAdjustZeroCurrent = false;
                isAdjustLoadVoltage = false;
                isAdjustBatteryVoltage = false;
                isAdjustOutResistance = false;
                isAdjustInnerResistance = false;
                U_ID = "默认值";
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
            }

            else if (btn.Name == "btnAdZeroCurrent")
            {
                if (!Regex.IsMatch(this.tbZeroCurrent.Text, strPatten))
                {
                    MessageBox.Show("请输入正确的电流值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                isAdjustRTC = false;
                isAdjustCurrent = false;
                isAdjustZeroCurrent = true;
                isAdjustLoadVoltage = false;
                isAdjustBatteryVoltage = false;
                isAdjustOutResistance = false;
                isAdjustInnerResistance = false;
                U_ID = "默认值";
                RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
            }

        }
        string preAdjustCurrentVal = string.Empty;
        public void HandleAdjustRTCurrenEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.bReadBqBmsResp = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
            if (isAdjustCurrent)
            {
                BqProtocol.bReadBqBmsResp = true;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xA9 && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        if (isRequireAdjust10A)
                        {
                            Adjust10ACurrentOverEvent?.Invoke(this, new EventArgs<bool>(true));
                            isRequireAdjust10A = false;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(800);
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.电流校准, OperationResultEnum.成功, string.Empty, preAdjustCurrentVal, tbCurrent.Text.Trim());
                            AutoClosedMsgBox.Show("校准实时电流成功！", "校准提示", 1000, 64);
                        }
                    }
                    else
                    {
                        if (isRequireAdjustZero)
                        {
                            Adjust10ACurrentOverEvent?.Invoke(this, new EventArgs<bool>(false));
                            isRequireAdjust10A = false;
                        }
                        else
                        {
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.电流校准, OperationResultEnum.失败, "校准实时电流失败！", preAdjustCurrentVal, tbCurrent.Text.Trim());
                            MessageBox.Show("校准实时电流失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                isAdjustCurrent = false;
            }
        }

        public void HandleAdjustZeroCurrenEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustZeroCurrent)
            {
                BqProtocol.bReadBqBmsResp = true;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xA8 && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        if (isRequireAdjustZero)
                        {
                            AdjustZeroCurrentOverEvent?.Invoke(this, new EventArgs<bool>(true));
                            isRequireAdjustZero = false;
                        }
                        else
                        {
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.成功, string.Empty, preAdjustCurrentVal, "0");
                            AutoClosedMsgBox.Show("校准零点电流成功！", "校准提示", 1000, 64);
                        }
                    }
                    else
                    {
                        if (isRequireAdjustZero)
                        {
                            AdjustZeroCurrentOverEvent?.Invoke(this, new EventArgs<bool>(false));
                            isRequireAdjustZero = false;
                        }
                        else
                        {
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.零点校准, OperationResultEnum.失败, "校准零点电流失败！", preAdjustCurrentVal, "0");
                            MessageBox.Show("校准零点电流失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                isAdjustZeroCurrent = false;
            }
        }

        bool isAdjustRTC = false;
        bool isRequireAdjustRTC = false;
        public void RequireAdjustRTC(bool _isRequireAdjustRTC)
        {
            isRequireAdjustRTC = _isRequireAdjustRTC;
            btnAdjustRTC_Click(null, null);
        }

        public void SetUID(string uid)
        {
            try
            {
                U_ID = uid;
                BqProtocol.BqInstance.m_bIsStopCommunication = true;

                if (isAdjustRTC)
                {
                    if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                    {
                        DateTime dt = DateTime.Parse(tbCurrentTime.Text.Trim());
                        BqProtocol.bReadBqBmsResp = true;
                        BqProtocol.BqInstance.AdjustRTC(dt);
                    }
                    else
                    {
                        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0);
                        uint _dt = (uint)(ts.Ticks / Math.Pow(10, 7));
                        DDProtocol.DdProtocol.DdInstance.AdjustDidiRTC(_dt);
                    }
                }
                else if (isAdjustCurrent)
                {
                    preAdjustCurrentVal = tbCurrent.Text.Trim();
                    BqProtocol.BqInstance.AdjustRtCurrent(int.Parse(tbRtCurrent.Text));
                }
                else if (isAdjustZeroCurrent)
                {
                    preAdjustCurrentVal = tbCurrent.Text.Trim();
                    BqProtocol.BqInstance.AdjustZeroCurrent(int.Parse(tbZeroCurrent.Text));
                }
                else if(isAdjustLoadVoltage)
                {
                    uint voltage = uint.Parse(tbAdjustLoadVoltage.Text.Trim());
                    BqProtocol.BqInstance.BQ_AdjustLoadVoltageParam(voltage);
                }
                else if(isAdjustBatteryVoltage)
                {
                    uint voltage = uint.Parse(tbAdjustBatteryVoltage.Text.Trim());
                    BqProtocol.BqInstance.BQ_AdjustBatteryVoltageParam(voltage);
                }
                else if(isAdjustOutResistance)
                {
                    ushort resistance = ushort.Parse(tbAdjustOutResistance.Text.Trim());
                    BqProtocol.BqInstance.BQ_AdjustOutResistanceParam(resistance);
                }
                else if(isAdjustInnerResistance)
                {
                    ushort resistance = ushort.Parse(tbAdjustInnerResistance.Text.Trim());
                    BqProtocol.BqInstance.BQ_AdjustInnerResistanceParam(resistance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public event EventHandler<EventArgs<string>> RequireReadUIDEvent;
        private void btnAdjustRTC_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.m_statusBarInfo.IsOnline)
            {
                try
                {
                    DateTime dt = DateTime.Parse(tbCurrentTime.Text.Trim());
                    if(null != dt)
                    {
                        isAdjustRTC = true;
                        isAdjustCurrent = false;
                        isAdjustZeroCurrent = false;
                        isAdjustLoadVoltage = false;
                        isAdjustBatteryVoltage = false;
                        isAdjustOutResistance = false;
                        isAdjustInnerResistance = false;
                        U_ID = "默认值";
                        RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
                    }
                    else
                    {
                        MessageBox.Show("请检查RTC时间格式是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("请检查RTC时间格式是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event EventHandler<EventArgs<bool>> AdjustRTCOverEvent;
        public void HandleAdjustRTCEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (isAdjustRTC)
            {
                BqProtocol.bReadBqBmsResp = true; 
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xA7 && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        if(isRequireAdjustRTC)
                        {
                            isRequireAdjustRTC = false;
                            AdjustRTCOverEvent?.Invoke(this, new EventArgs<bool>(true));
                        }
                        else
                        {
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.RTC校准, OperationResultEnum.成功, string.Empty);
                            AutoClosedMsgBox.Show("校准RTC成功！", "校准提示", 1000, 64);
                            BqProtocol.BqInstance.BQ_ReadRTC();
                        }
                    }
                    else
                    {
                        if (isRequireAdjustRTC)
                        {
                            isRequireAdjustRTC = false;
                            AdjustRTCOverEvent?.Invoke(this, new EventArgs<bool>(false));
                        }
                        else
                        {
                            SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.RTC校准, OperationResultEnum.失败, "校准RTC失败！");
                            MessageBox.Show("校准RTC失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                isAdjustRTC = false;
            }
        }

        public static DateTime systemStartTime = new DateTime(1970, 1, 1, 8, 0, 0);
        public event EventHandler<EventArgs<string>> ReadRTCOverEvent;
        public void HandleReadBqRTCEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.bReadBqBmsResp = true;
            if (e.RecvMsg[1] == 0xA3 && e.RecvMsg[0] == 0xCC && (e.RecvMsg.Count >= (e.RecvMsg[2] << 8 | e.RecvMsg[3])))
            {
                int nRegister = ((e.RecvMsg[7] << 24) | (e.RecvMsg[6] << 16) | (e.RecvMsg[5] << 8) | e.RecvMsg[4]);
                TimeSpan ts = new TimeSpan((long)(nRegister * Math.Pow(10, 7)));
                tbRtc.Text = (systemStartTime + ts).ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                string str = "RTC数据读取失败！";
                tbRtc.Text = str;
            }
            if (isReadRTC)
            {
                isReadRTC = false;
                ReadRTCOverEvent?.Invoke(this, new EventArgs<string>(tbRtc.Text.Trim()));
            }
        }
        string U_ID = "默认值";
        void SaveOperationRecord(string data, OperationTypeEnum type, OperationResultEnum result, string comments)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    StringBuilder sb = new StringBuilder();
                    string totalVoltage = string.Empty;
                    string current = string.Empty;
                    foreach (var it in UserCtrlBqBmsInfo.m_ListCellVoltage)
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
                    string cellTemp = string.Format("{0}${1}${2}${3}", UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度1").StrValue,
                        UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度2").StrValue, UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度3").StrValue,
                        UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "MOS温度1").StrValue);

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
                        AmbientTemp = UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "环境温度").StrValue,
                        CellTemp = cellTemp,
                        Humidity = UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue,
                        LoopNumber = Int32.Parse(UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "放电循环次数").StrValue),
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
                    foreach (var it in UserCtrlBqBmsInfo.m_ListCellVoltage)
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
                    string cellTemp = string.Format("{0}${1}${2}${3}", UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度1").StrValue,
                        UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度2").StrValue, UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "电芯温度3").StrValue,
                        UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "MOS温度1").StrValue);

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
                        AmbientTemp = UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "环境温度").StrValue,
                        CellTemp = cellTemp,
                        Humidity = UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "湿度").StrValue,
                        LoopNumber = Int32.Parse(UserCtrlBqBmsInfo.m_ListBmsInfo.FirstOrDefault(p => p.Description == "放电循环次数").StrValue),
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

        bool isAdjustLoadVoltage = false;
        bool isAdjustBatteryVoltage = false;
        bool isAdjustOutResistance = false;
        bool isAdjustInnerResistance = false;
        private void btnAdjustLoadVol_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                uint voltage = 0;
                if (uint.TryParse(tbLoadVoltage.Text.Trim(), out voltage))
                {
                    isAdjustRTC = false;
                    isAdjustCurrent = false;
                    isAdjustZeroCurrent = false;
                    isAdjustLoadVoltage = true;
                    isAdjustBatteryVoltage = false;
                    isAdjustOutResistance = false;
                    isAdjustInnerResistance = false;
                    U_ID = "默认值";
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
                }
                else
                {
                    MessageBox.Show("输入的负载端电压格式不正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void HandleRecvAdjustLoadVoltageEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustLoadVoltage)
            {
                isAdjustLoadVoltage = false;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xAB && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.总压校准, OperationResultEnum.成功, "校准负载端电压成功！");
                        MessageBox.Show("校准负载端电压成功！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.总压校准, OperationResultEnum.失败, "校准负载端电压失败！");
                        MessageBox.Show("校准负载端电压失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        private void btnAdjustBatteryVol_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                uint voltage = 0;
                if (uint.TryParse(tbBatteryVoltage.Text.Trim(), out voltage))
                {
                    isAdjustRTC = false;
                    isAdjustCurrent = false;
                    isAdjustZeroCurrent = false;
                    isAdjustLoadVoltage = false;
                    isAdjustBatteryVoltage = true;
                    isAdjustOutResistance = false;
                    isAdjustInnerResistance = false;
                    U_ID = "默认值";
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
                }
                else
                {
                    MessageBox.Show("输入的电池端电压格式不正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void HandleRecvAdjustBatteryVoltageEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustBatteryVoltage)
            {
                isAdjustBatteryVoltage = false;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xAA && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.总压校准, OperationResultEnum.成功, "校准电池端电压成功！");
                        MessageBox.Show("校准电池端电压成功！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.总压校准, OperationResultEnum.失败, "校准电池端电压失败！");
                        MessageBox.Show("校准电池端电压失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        private void btnAdjustOutResistance_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                uint voltage = 0;
                if (uint.TryParse(tbBatteryVoltage.Text.Trim(), out voltage))
                {
                    isAdjustRTC = false;
                    isAdjustCurrent = false;
                    isAdjustZeroCurrent = false;
                    isAdjustLoadVoltage = false;
                    isAdjustBatteryVoltage = false;
                    isAdjustOutResistance = true;
                    isAdjustInnerResistance = false;
                    U_ID = "默认值";
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
                }
                else
                {
                    MessageBox.Show("输入的外包进水阻抗格式不正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void HandleRecvAdjustOutResistanceEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustOutResistance)
            {
                isAdjustOutResistance = false;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xAD && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.进水阻抗校准, OperationResultEnum.成功, "校准外包进水阻抗成功！");
                        MessageBox.Show("校准外包进水阻抗成功！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.进水阻抗校准, OperationResultEnum.失败, "校准外包进水阻抗失败！");
                        MessageBox.Show("校准外包进水阻抗失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        private void btnAdjustInnerResistance_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                uint voltage = 0;
                if (uint.TryParse(tbBatteryVoltage.Text.Trim(), out voltage))
                {
                    isAdjustRTC = false;
                    isAdjustCurrent = false;
                    isAdjustZeroCurrent = false;
                    isAdjustLoadVoltage = false;
                    isAdjustBatteryVoltage = false;
                    isAdjustOutResistance = false;
                    isAdjustInnerResistance = true;
                    U_ID = "默认值";
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Adjust"));
                }
                else
                {
                    MessageBox.Show("输入的内包进水阻抗格式不正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void HandleRecvAdjustInnerResistanceEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustInnerResistance)
            {
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
                if (e.RecvMsg[0] == 0xDD && e.RecvMsg[1] == 0xAC && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                {
                    var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    if (res == 0)
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.进水阻抗校准, OperationResultEnum.成功, "校准内包进水阻抗成功！");
                        MessageBox.Show("校准内包进水阻抗成功！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.进水阻抗校准, OperationResultEnum.失败, "校准内包进水阻抗失败！");
                        MessageBox.Show("校准内包进水阻抗失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
