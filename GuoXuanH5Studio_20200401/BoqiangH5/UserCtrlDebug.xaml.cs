﻿using BoqiangH5.BQProtocol;
using BoqiangH5.DDProtocol;
using BoqiangH5Entity;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using BoqiangH5Repository;
using System.Linq;
using System.Threading;
using DBService;
using System.Text;

namespace BoqiangH5
{

    public partial class UserCtrlDebug : UserControl
    {
        public static bool m_bIsNotUpdateBmsInfo = false;
        public static List<H5BmsInfo> m_ListBmsInfo = new List<H5BmsInfo>();
        public static List<H5BmsInfo> m_ListCellVoltage = new List<H5BmsInfo>();
        static byte[] btSysStatus = new byte[2];
        static byte[] btPackStatus = new byte[2];
        static byte[] btBalanceStatus = new byte[4];
        public UserCtrlDebug()
        {
            InitializeComponent();

            InitDebugWnd();
        }

        private void InitDebugWnd()
        {

            userCtrlDebug.IsEnabled = false;
            userCtrlDebug.IsEnabled = false;

            m_ListCellVoltage.Clear();
            m_ListBmsInfo.Clear();

            string strConfigFile = XmlHelper.m_strBqProtocolFile;

            XmlHelper.LoadXmlConfig(strConfigFile, "bms_info/cell_votage_info", m_ListCellVoltage);
            XmlHelper.LoadXmlConfig(strConfigFile, "bms_info/bms_info_node", m_ListBmsInfo);
        }


        private void UpdateDebugWndStatus()
        {

            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                userCtrlDebug.IsEnabled = true;
            }
            else
            {
                userCtrlDebug.IsEnabled = false;
            }

        }

        public void HandleDebugWndUpdateEvent(object sender, EventArgs e)
        {
            UpdateDebugWndStatus();
        }

        public void HandleDebugEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.bReadBqBmsResp = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
            if (e.RecvMsg[0] == 0xDD && e.RecvMsg.Count == (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
            {
                switch (e.RecvMsg[1])
                {
                    case 0xD0:
                        if (isJumpBoot)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("跳转boot成功！", "跳转boot提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("跳转boot失败！", "跳转boot提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            isJumpBoot = false;
                        }
                        break;
                    case 0xBC:
                        if (isSoftReset)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("系统复位成功！", "系统复位提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("系统复位成功！", "系统复位提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            isSoftReset = false;
                        }
                        break;
                    case 0xB9:
                        if (isAlterSOC)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                            {
                                if(isRequireAlterSOC)
                                {
                                    isRequireAlterSOC = false;
                                    AlterSOCOverEvent?.Invoke(this, new EventArgs<bool>(true));
                                }
                                else
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.成功, string.Empty);
                                    MessageBox.Show("SOC设置成功！", "设置SOC提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            else
                            {
                                if (isRequireAlterSOC)
                                {
                                    isRequireAlterSOC = false;
                                    AlterSOCOverEvent?.Invoke(this, new EventArgs<bool>(false)) ;
                                }
                                else
                                {
                                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.SOC校准, OperationResultEnum.失败, "SOC设置失败");
                                    MessageBox.Show("SOC设置失败！", "设置SOC提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            isAlterSOC = false;
                        }
                        break;
                    case 0xBE:
                        if (isFactoryReset)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("恢复出厂设置成功！", "恢复出厂设置提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("恢复出厂设置失败！", "恢复出厂设置提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            isFactoryReset = false;
                        }
                        break;
                    case 0xB8:
                        if (isTestMode)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("进入测试模式设置成功！", "进入测试模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("进入测试模式设置失败！", "进入测试模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            isTestMode = false;
                        }
                        if (isExitTestMode)
                        {
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("退出测试模式设置成功！", "退出测试模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("退出测试模式设置失败！", "退出测试模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            isExitTestMode = false;
                        }
                        break;
                    //case 0xBA:
                    //    if (isDeepSleep)
                    //    {
                    //        isDeepSleep = false;
                    //        DeepSleepEvent?.Invoke(this, EventArgs.Empty);//设置深休眠成功，断开连接
                    //        var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    //        if (res == 0)
                    //            MessageBox.Show("进入深休眠设置成功！", "进入深休眠提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //        else
                    //            MessageBox.Show("进入深休眠设置失败！", "进入深休眠提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    }
                    //    break;
                    //case 0xBB:
                    //    if (isShallowSleep)
                    //    {
                    //        isShallowSleep = false;
                    //        ShallowSleepEvent?.Invoke(this, EventArgs.Empty);//设置浅休眠成功，断开连接
                    //        var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                    //        if (res == 0)
                    //            MessageBox.Show("进入浅休眠设置成功！", "进入浅休眠提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //        else
                    //            MessageBox.Show("进入浅休眠设置失败！", "进入浅休眠提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    }
                    //    break;
                    case 0xC0:
                        if (isExitChargeMos)
                        {
                            isExitChargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("充电MOS开关退出设置成功！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("充电MOS开关退出设置失败！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (isCloseChargeMos)
                        {
                            isCloseChargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("充电MOS开关闭合设置成功！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("充电MOS开关闭合设置失败！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (isOpenChargeMos)
                        {
                            isOpenChargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("充电MOS开关断开设置成功！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("充电MOS开关断开设置失败！", "充电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        break;
                    case 0xC1:
                        if (isExitDischargeMos)
                        {
                            isExitDischargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("放电MOS开关退出设置成功！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("放电MOS开关退出设置失败！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (isCloseDischargeMos)
                        {
                            isCloseDischargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("放电MOS开关闭合设置成功！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("放电MOS开关闭合设置失败！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (isOpenDischargeMos)
                        {
                            isOpenDischargeMos = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("放电MOS开关断开设置成功！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("放电MOS开关断开设置失败！", "放电MOS开关提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        break;
                    case 0xC3:
                        if (isEnterProductionMode)
                        {
                            isEnterProductionMode = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("进入生产模式设置成功！", "进入生产模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("进入生产模式设置失败！", "进入生产模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (isExitProductionMode)
                        {
                            isExitProductionMode = false;
                            var res = e.RecvMsg[5] << 8 | e.RecvMsg[4];
                            if (res == 0)
                                MessageBox.Show("退出生产模式设置成功！", "退出生产模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("退出生产模式设置失败！", "退出生产模式提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        break;
                    default:
                        break;

                }
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            InitDebugWnd();
        }

        bool isBqPowerOn = false;
        bool isBqPowerOff = false;

        public void RequirePowerOn()
        {
            isRequirePowerOn = true;
            btnPowerOn_Click(null, null);
        }
        /// <summary>
        /// 上电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPowerOn_Click(object sender, RoutedEventArgs e)
        { 
            isBqPowerOn = true;
            isDdPowerOn = false;
            isAdjustRTC = false;
            isDdPowerOff = false;
            isBqPowerOff = false;
            isAlterSOC = false;
            U_ID = "默认值";
            RequireReadUID("debug");
        }

        public event EventHandler PowerOnOverEvent;
        public event EventHandler PowerOffOverEvent;
        bool isRequirePowerOn = false;
        bool isRequirePowerOff = false;
        public void RequirePowerOff()
        {
            isRequirePowerOff = true;
            btnPowerOff_Click(null, null);
        }
        /// <summary>
        /// 下电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPowerOff_Click(object sender, RoutedEventArgs e)
        {
            isBqPowerOff = true;
            isDdPowerOn = false;
            isAdjustRTC = false;
            isDdPowerOff = false;
            isBqPowerOn = false;
            isAlterSOC = false;
            U_ID = "默认值";
            RequireReadUID("debug");
        }

        public void HandleRaisePowerOnOffEvent(object sender, EventArgs e)
        {
            DdProtocol.bReadDdBmsResp = true;
            if(isDdPowerOn || isDdPowerOff)
            {
                DdProtocol.DdInstance.m_bIsStopCommunication = false;
            }
            if (isBqPowerOn || isBqPowerOff)
            {
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
            }
            DdProtocol.DdInstance.m_bIsStopCommunication = false;
            if(isDdPowerOn || isBqPowerOn)
            {
                if (isRequirePowerOn)
                {
                    isRequirePowerOn = false;
                    PowerOnOverEvent?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.上电, OperationResultEnum.成功, string.Empty);
                    MessageBox.Show("上电成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                isDdPowerOn = false;
                isBqPowerOn = false;
            }

            if (isDdPowerOff || isBqPowerOff)
            {
                if(isRequirePowerOff)
                {
                    isRequirePowerOff = false;
                    PowerOffOverEvent?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.下电, OperationResultEnum.成功, string.Empty);
                    MessageBox.Show("下电成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                isDdPowerOff = false;
                isBqPowerOff = false;
            }


        }

        bool isJumpBoot = false;
        private void btnJumpBoot_Click(object sender, RoutedEventArgs e)
        {
            isJumpBoot = false;
            BqProtocol.BqInstance.BQ_JumpToBoot();
            isJumpBoot = true;
        }

        bool isSoftReset = false;
        private void btnSoftReset_Click(object sender, RoutedEventArgs e)
        {
            isSoftReset = false;
            BqProtocol.BqInstance.BQ_Reset();
            isSoftReset = true;
        }

        bool isAlterSOC = false;
        bool isRequireAlterSOC = false;
        public event EventHandler<EventArgs<bool>> AlterSOCOverEvent;
        public void RequireAlterSOC(string val)
        {
            tbSOC.Text = val;
            isRequireAlterSOC = true;
            btnAlterSOC_Click(null, null);
        }
        private void btnAlterSOC_Click(object sender, RoutedEventArgs e)
        {

            string str = @"^[0-9]{1,3}$";
            if (!Regex.IsMatch(tbSOC.Text, str))    // if (string.IsNullOrEmpty(txtBoxBarcode.Text))
            {
                MessageBox.Show("请输入正确的 SOC 值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            byte socVal = byte.Parse(tbSOC.Text);

            if (socVal < 0 || socVal > 100)
            {
                MessageBox.Show("请输入正确的 SOC 值！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            isAlterSOC = true;
            isDdPowerOn = false;
            isAdjustRTC = false;
            isDdPowerOff = false;
            isBqPowerOff = false;
            isBqPowerOn = false;
            U_ID = "默认值";
            RequireReadUID("debug");
        }

        public void HandleRecvBmsInfoDataEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (e.RecvMsg[0] != 0xCC && e.RecvMsg[1] != 0xA2)
            {
                return;
            }
            if (e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
            {
                return;
            }
            BqProtocol.bReadBqBmsResp = true;
            int nBqByteIndex = 4;
            for (int n = 0; n < m_ListCellVoltage.Count; n++)
            {
                int nCellVol = 0;
                for (int m = m_ListCellVoltage[n].ByteCount - 1; m >= 0; m--)
                {
                    nCellVol = (nCellVol >> 8 | e.RecvMsg[nBqByteIndex + m]);
                }

                m_ListCellVoltage[n].StrValue = nCellVol.ToString();

                nBqByteIndex += m_ListCellVoltage[n].ByteCount;
            }

            for (int i = 0; i < m_ListBmsInfo.Count; i++)
            {
                int nBmsVal = 0;

                if (!m_ListBmsInfo[i].Description.Contains("状态"))
                {
                    for (int j = m_ListBmsInfo[i].ByteCount - 1; j >= 0; j--)
                    {
                        nBmsVal = (nBmsVal << 8 | e.RecvMsg[nBqByteIndex + j]);
                    }
                    if (m_ListBmsInfo[i].Description.Contains("温度"))
                    {
                        m_ListBmsInfo[i].StrValue = ((nBmsVal - 2731) / 10.0).ToString("F1");
                    }
                    else
                        m_ListBmsInfo[i].StrValue = (nBmsVal * decimal.Parse(m_ListBmsInfo[i].Scale)).ToString();
                }

                nBqByteIndex += m_ListBmsInfo[i].ByteCount;
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = m_ListBmsInfo.SingleOrDefault(p => p.Description == "SOC");
                if (null != item)
                {
                    tbCurrentSOC.Text = item.StrValue.ToString();
                }

                var utc = m_ListBmsInfo.SingleOrDefault(p => p.Description == "UTC");
                if (utc != null)
                {
                    uint dt = 0;
                    bool ret = UInt32.TryParse(utc.StrValue, out dt);
                    if (dt < 4294967295)
                    {
                        if (ret)
                        {
                            TimeSpan ts = new TimeSpan((long)(dt * Math.Pow(10, 7)));
                            tbDdUTCRTC.Text = (new DateTime(1970, 1, 1, 8, 0, 0) + ts).ToString("yyyy/MM/dd HH:mm:ss");
                            tbDdCurrentUTC.Text = dt.ToString();
                        }
                    }
                }
            }), null);
        }

        bool isFactoryReset = false;
        private void btnFactoryReset_Click(object sender, RoutedEventArgs e)
        {
            isFactoryReset = false;
            BqProtocol.BqInstance.BQ_FactoryReset();
            isFactoryReset = true;
        }

        // 5
        private void btnShutdown_Click(object sender, RoutedEventArgs e)
        {
            //BqProtocol.BqInstance.BQ_Shutdown();
        }

        // 6
        private void btnSleep_Click(object sender, RoutedEventArgs e)
        {
            //BqProtocol.BqInstance.BQ_Sleep();
        }

        bool isReadBoot = false;
        public void RequireReadBoot(bool _isRequireRead)
        {
            isRequireRead = _isRequireRead;
            btnReadBoot_Click(null, null);
        }
        private void btnReadBoot_Click(object sender, RoutedEventArgs e)
        {
            isReadBoot = false;
            BqProtocol.BqInstance.BQ_ReadBootInfo();
            isReadBoot = true;
        }
        public event EventHandler<EventArgs<string>> ReadBootOverEvent;
        bool isRequireRead = false;
        public void HandleReadBqBootEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                if (isReadBoot)
                {
                    if (e.RecvMsg[0] != 0xCC || e.RecvMsg[1] != 0xB9 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                    {
                        return;
                    }

                    int offset = 4;
                    byte[] array = e.RecvMsg.ToArray();
                    string projectName = System.Text.Encoding.ASCII.GetString(array, offset, 8);
                    offset += 8;
                    string hardwareVersion = System.Text.Encoding.ASCII.GetString(array, offset, 8);
                    offset += 8;
                    string bootVersion = System.Text.Encoding.ASCII.GetString(array, offset, 8);
                    offset += 8;
                    string appNum = System.Text.Encoding.ASCII.GetString(array, offset, 8);
                    offset += 8;
                    string company = System.Text.Encoding.ASCII.GetString(array, offset, 16);
                    offset += 16;
                    string programStatusStr = string.Empty;
                    byte[] _array = new byte[2];
                    Buffer.BlockCopy(array, offset, _array, 0, _array.Length);
                    int programStatus = ((_array[1] << 8) | (((_array[0]))));
                    if (programStatus == 1)
                        programStatusStr = "APP初始化完成";
                    else if (programStatus == 0)
                        programStatusStr = "BOOT";
                    else if (programStatus == 2)
                        programStatusStr = "APP初始化";

                    List<string> list = new List<string>();
                    list.Add(projectName.Substring(0, projectName.IndexOf('\0')));
                    list.Add(hardwareVersion.Substring(0, hardwareVersion.IndexOf('\0')));
                    list.Add(bootVersion.Substring(0, bootVersion.IndexOf('\0')));
                    list.Add(appNum.Substring(0, appNum.IndexOf('\0')));
                    list.Add(company.Substring(0, company.IndexOf('\0')));
                    list.Add(programStatusStr);

                    UpdateBqBootInfo(list);
                    MessageBox.Show("读取Boot信息成功！", "读取Boot提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    isReadBoot = false;
                }
                BqProtocol.BqInstance.m_bIsStopCommunication = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Boot信息解析异常", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateBqBootInfo(List<string> list)
        {
            if (list.Count == 6)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tbProjectName.Text = list[0].Trim();
                    tbHardwareVersion.Text = list[1].Trim();
                    tbBootVersion.Text = list[2].Trim();
                    tbAppNum.Text = list[3].Trim();
                    tbCompany.Text = list[4].Trim();
                    tbProgramStatus.Text = list[5].Trim();
                }));
            }
        }

        bool isAdjustRTC = false;
        //private void btnAdjustRTC_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        DateTime dt = DateTime.Parse(tbAlterRTC.Text.Trim());
        //        if (null != dt)
        //        {
        //            if(dt.Year < 2000 || dt.Year > 2999)
        //            {
        //                MessageBox.Show("RTC的年份范围为：2000~2999");
        //            }
        //            else
        //            {
        //                isAdjustRTC = true;
        //                isDdPowerOff = false;
        //                isDdPowerOn = false;
        //                isBqPowerOff = false;
        //                isBqPowerOn = false;
        //                isAlterSOC = false;
        //                U_ID = "默认值";
        //                RequireReadUID("debug");
        //            }
        //        }
        //        else
        //        {
        //            MessageBox.Show("请检查RTC时间格式是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("请检查RTC时间格式是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        public void HandleAdjustRTCEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isAdjustRTC)
            {
                if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                {
                    BqProtocol.bReadBqBmsResp = true;
                    if (e.RecvMsg[0] == 0x10)
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.UTC校准, OperationResultEnum.成功, string.Empty);
                        MessageBox.Show("校准UTC成功！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        DDProtocol.DdProtocol.DdInstance.Didi_ReadRTC();
                    }
                    else
                    {
                        SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(DdProtocol.DdInstance.IssuedBytesList.ToArray()), OperationTypeEnum.UTC校准, OperationResultEnum.失败, "校准UTC失败！");
                        MessageBox.Show("校准UTC失败！", "校准提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    BqProtocol.BqInstance.m_bIsStopCommunication = false;
                }
                isAdjustRTC = false;
            }

        }

        public void HandleReadBqRTCEvent(object sender, CustomRecvDataEventArgs e)
        {
            if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
            {
                BqProtocol.bReadBqBmsResp = true;
                if (e.RecvMsg[1] == 0xA3 && e.RecvMsg[0] == 0xCC)
                {
                    int nRegister = ((e.RecvMsg[7] << 24) | (e.RecvMsg[6] << 16) | (e.RecvMsg[5] << 8) | e.RecvMsg[4]);
                    TimeSpan ts = new TimeSpan((long)(nRegister * Math.Pow(10, 7)));
                    tbDdUTCRTC.Text = (new DateTime(1970, 1, 1, 8, 0, 0) + ts).ToString("yyyy/MM/dd HH:mm:ss");
                    tbDdCurrentUTC.Text = nRegister.ToString();
                }
            }
            else
            {
                DDProtocol.DdProtocol.bReadDdBmsResp = true;
                if (e.RecvMsg[0] == 0x03 && e.RecvMsg[1] == 0x04)
                {
                    int nRegister = ((e.RecvMsg[2] << 24) | (e.RecvMsg[3] << 16) | (e.RecvMsg[4] << 8) | e.RecvMsg[5]);
                    tbDdCurrentUTC.Text = nRegister.ToString();
                    TimeSpan ts = new TimeSpan((long)(nRegister * Math.Pow(10, 7)));
                    tbDdUTCRTC.Text = (new DateTime(1970,1,1,8,0,0) + ts).ToString("yyyy/MM/dd HH:mm:ss");
                };
            }
        }

        bool isDdPowerOn = false;
        bool isDdPowerOff = false;
        private void btnDDPowerOff_Click(object sender, RoutedEventArgs e)
        {
            isDdPowerOff = true;
            isAdjustRTC = false;

            isDdPowerOn = false;
            isBqPowerOff = false;
            isBqPowerOn = false;
            isAlterSOC = false;
            U_ID = "默认值";
            RequireReadUID("debug");
        }
        private void btnDDPowerOn_Click(object sender, RoutedEventArgs e)
        {
            isDdPowerOn = true;
            isAdjustRTC = false;
            isDdPowerOff = false;
            isBqPowerOff = false;
            isBqPowerOn = false;
            isAlterSOC = false;
            U_ID = "默认值";
            RequireReadUID("debug");
        }

        private void btnDdAdjustUTC_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                try
                {
                    uint dt = 0;
                    bool ret = UInt32.TryParse(tbDdAlterUTC.Text.Trim(),out dt);
                    if(dt >= 4294967295)
                    {
                        MessageBox.Show("UTC值的范围0~4294967295，请检查UTC输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        if (ret)
                        {
                            isAdjustRTC = true;
                            isDdPowerOff = false;
                            isDdPowerOn = false;
                            isBqPowerOff = false;
                            isBqPowerOn = false;
                            isAlterSOC = false;
                            U_ID = "默认值";
                            RequireReadUID("debug");
                        }
                        else
                        {
                            MessageBox.Show("请检查修改UTC值是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("请检查修改UTC值是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnCalRTC_Click(object sender, RoutedEventArgs e)
        {
            uint dt = 0;
            bool ret = UInt32.TryParse(tbDdAlterUTC.Text.Trim(), out dt);
            if (dt >= 4294967295)
            {
                MessageBox.Show("UTC值的范围0~4294967294，请检查UTC输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (ret)
                {
                    TimeSpan ts = new TimeSpan((long)(dt * Math.Pow(10, 7)));
                    tbDdAlterRTC.Text = (new DateTime(1970, 1, 1, 8, 0, 0) + ts).ToString("yyyy/MM/dd HH:mm:ss");
                }
                else
                {
                    MessageBox.Show("请检查修改UTC值是否正确！", "计算时间提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnCalUTC_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt;
            bool ret = DateTime.TryParse(tbDdAlterRTC.Text.Trim(),out dt);
            if (ret)
            {
                TimeSpan ts = dt - new DateTime(1970, 1, 1, 8, 0, 0);
                long ticks = (long)(ts.Ticks / Math.Pow(10,7));
                if (ticks >= 4294967295)
                {
                    MessageBox.Show("输入时间大于最大UTC值，请输入正确的时间！","计算UTC提示",MessageBoxButton.OK,MessageBoxImage.Information);
                }
                else
                {
                    tbDdAlterUTC.Text = ticks.ToString();
                }
            }
            else
            {
                MessageBox.Show("请检查RTC时间格式是否正确！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            byte[] registerAddrBytes;
            byte registerNum;
            byte[] dataBytes;
            string regexStr = @"([^A-Fa-f0-9]|\s+?)+";
            string registerAddrStr = tbRegisterAddr.Text.Trim();
            if(!string.IsNullOrEmpty(registerAddrStr))
            {
                registerAddrStr = registerAddrStr.Replace(" ", "");
                if ((registerAddrStr.Length % 2) != 0)
                    registerAddrStr += " ";
                if (!Regex.IsMatch(registerAddrStr, regexStr))
                {
                    registerAddrBytes = new byte[registerAddrStr.Length / 2];
                    for (int i = 0; i < registerAddrBytes.Length; i++)
                        registerAddrBytes[i] = Convert.ToByte(registerAddrStr.Substring(i * 2, 2), 16);
                }
                else
                {
                    MessageBox.Show("输入的寄存器地址包含非十六进制数字，请检查！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                MessageBox.Show("请输入寄存器地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string registerNumStr = tbRegisterNum.Text.Trim();
            if (!string.IsNullOrEmpty(registerNumStr))
            {
                uint num = 0;
                if(uint.TryParse(registerNumStr, out num))
                {
                    if(num > 255 || num <= 0)
                    {
                        MessageBox.Show("请输入正确的寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    registerNum = Convert.ToByte(num);
                }
                else
                {
                    MessageBox.Show("请输入正确的寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                MessageBox.Show("请输入寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string dataStr = tbData.Text.Trim();
            if (!string.IsNullOrEmpty(dataStr))
            {
                dataStr = dataStr.Replace(" ", "");
                if ((dataStr.Length % 2) != 0)
                    dataStr += " ";
                //if(Regex.IsMatch(dataStr,regexStr))
                {
                    dataBytes = new byte[dataStr.Length / 2];
                    for (int i = 0; i < dataBytes.Length; i++)
                        dataBytes[i] = Convert.ToByte(dataStr.Substring(i * 2, 2), 16);

                    if (dataBytes.Length != 2 * Convert.ToInt16(registerNum))
                    {
                        MessageBox.Show("输入的寄存器个数和输入的数据不匹配，请检查！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                //else
                //{
                //    MessageBox.Show("输入的数据包含非十六进制数字，请检查！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //    return;
                //}

            }
            else
            {
                MessageBox.Show("请输入寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        public void HandleWriteRegisterEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(e.RecvMsg[0] == 0x10)
            {
                MessageBox.Show("写寄存器成功！", "写寄存器提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        bool isTestMode = false;
        private void btnEnterTestMode_Click(object sender, RoutedEventArgs e)
        {
            isTestMode = false;
            BqProtocol.BqInstance.BQ_EnterTestMode();
            isTestMode = true;
        }
        bool isExitTestMode = false;
        private void btnExitTestMode_Click(object sender, RoutedEventArgs e)
        {
            isExitTestMode = false;
            BqProtocol.BqInstance.BQ_ExitTestMode();
            isExitTestMode = true;
        }
        bool isRead = false;
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            byte[] registerAddrBytes;
            byte registerNum;
            string regexStr = @"^[A-Fa-f0-9]+$";
            string registerAddrStr = tbRegisterAddr.Text.Trim();
            if (!string.IsNullOrEmpty(registerAddrStr))
            {
                registerAddrStr = registerAddrStr.Replace(" ", "");
                if (Regex.IsMatch(registerAddrStr, regexStr))
                {
                    if ((registerAddrStr.Length % 2) != 0)
                        registerAddrStr += "0";
                    registerAddrBytes = new byte[registerAddrStr.Length / 2];
                    for (int i = 0; i < registerAddrBytes.Length; i++)
                        registerAddrBytes[i] = Convert.ToByte(registerAddrStr.Substring(i * 2, 2), 16);
                }
                else
                {
                    MessageBox.Show("输入的寄存器地址包含非十六进制数字，请检查！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                MessageBox.Show("请输入寄存器地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string registerNumStr = tbRegisterNum.Text.Trim();
            if (!string.IsNullOrEmpty(registerNumStr))
            {
                uint num = 0;
                if (uint.TryParse(registerNumStr, out num))
                {
                    if (num > 255 || num <= 0)
                    {
                        MessageBox.Show("请输入正确的寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    registerNum = Convert.ToByte(num);
                }
                else
                {
                    MessageBox.Show("请输入正确的寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                MessageBox.Show("请输入寄存器个数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            isRead = false;
            DdProtocol.DdInstance.m_bIsStopCommunication = true;
            DdProtocol.DdInstance.DD_ReadRegister(registerAddrBytes, registerNum);
            isRead = true;
        }

        public void HandleReadRegisterEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                if (isRead)
                {
                    if (e.RecvMsg[0] == 0x03)
                    {
                        string registerNumStr = tbRegisterNum.Text.Trim();
                        int registerNum = int.Parse(registerNumStr);
                        byte[] bytes = new byte[registerNum * 2 + 4];
                        if (e.RecvMsg.Count() >= bytes.Length)
                            Buffer.BlockCopy(e.RecvMsg.ToArray(), 0, bytes, 0, bytes.Length);
                        else
                            Buffer.BlockCopy(e.RecvMsg.ToArray(), 0, bytes, 0, e.RecvMsg.Count());
                        tbData.Text = BitConverter.ToString(bytes);
                        MessageBox.Show("读寄存器成功！", "读寄存器提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        DdProtocol.DdInstance.m_bIsStopCommunication = false;
                    }
                    isRead = false;

                }
            }
            catch (Exception ex)
            {
                isRead = false;
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbData.Text = string.Empty;
        }

        bool isReadUID = false;
        string operationWnd = "debug";
        public void RequireReadUID(string wndName)
        {
            isReadUID = false;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            operationWnd = wndName;
            Thread.Sleep(177);
            BqProtocol.BqInstance.BQ_ReadUID();
            isReadUID = true;
        }
        public event EventHandler<EventArgs<Tuple<string,string>>> GetUIDEvent;
        string U_ID = "默认值";
        public void HandleReadUIDEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                if (isReadUID)
                {
                    BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    isReadUID = false;
                    if (e.RecvMsg[0] != 0xCC || e.RecvMsg[1] != 0xA1 || e.RecvMsg.Count < (e.RecvMsg[2] << 8 | e.RecvMsg[3]))
                    {
                        return;
                    }

                    byte[] array = new byte[16];
                    Buffer.BlockCopy(e.RecvMsg.ToArray(), 4, array, 0, array.Length);
                    U_ID = CSVFileHelper.ToHexStrFromByte(array, true);
                    if (operationWnd != "debug")
                    {
                        GetUIDEvent?.Invoke(this, new EventArgs<Tuple<string, string>>(new Tuple<string, string>(operationWnd, U_ID)));
                    }
                    else
                    {
                        if (IsUIDExist(U_ID))
                        {
                            if (isBqPowerOn)
                            {
                                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                                Thread.Sleep(200);
                                DdProtocol.DdInstance.DD_PowerOn();
                            }
                            else if (isBqPowerOff)
                            {
                                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                                Thread.Sleep(200);
                                DdProtocol.DdInstance.DD_PowerOff();
                            }
                            else if (isDdPowerOff)
                            {
                                DdProtocol.DdInstance.m_bIsStopCommunication = true;
                                Thread.Sleep(200);
                                DdProtocol.DdInstance.DD_PowerOff();
                            }
                            else if (isDdPowerOn)
                            {
                                DdProtocol.DdInstance.m_bIsStopCommunication = true;
                                Thread.Sleep(200);
                                DdProtocol.DdInstance.DD_PowerOn();
                            }
                            else if (isAdjustRTC)
                            {
                                //if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                                //{
                                //    BqProtocol.BqInstance.m_bIsStopCommunication = true;
                                //    Thread.Sleep(200);
                                //    BqProtocol.bReadBqBmsResp = true;
                                //    BqProtocol.BqInstance.AdjustRTC(DateTime.Parse(tbAlterRTC.Text.Trim()));
                                //}
                                //else
                                {
                                    DdProtocol.DdInstance.m_bIsStopCommunication = true;
                                    Thread.Sleep(200);
                                    DDProtocol.DdProtocol.DdInstance.AdjustDidiRTC(UInt32.Parse(tbDdAlterUTC.Text.Trim()));
                                }
                            }
                            else if (isAlterSOC)
                            {
                                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                                Thread.Sleep(200);
                                ushort soc = ushort.Parse(tbSOC.Text.Trim());
                                BqProtocol.BqInstance.BQ_AlterSOC(BitConverter.GetBytes(soc));
                            }
                        }
                        else
                        {
                            MessageBox.Show(string.Format("该BMS的UID {0} 未进行注册，请注册后再进行操作！", U_ID), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

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

        bool isExitChargeMos = false;
        private void btnExitChargeMos_Click(object sender, RoutedEventArgs e)
        {
            isExitChargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_ExitChargeMos();
        }
        bool isCloseChargeMos = false;
        private void btnCloseChargeMos_Click(object sender, RoutedEventArgs e)
        {
            isCloseChargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_CloseChargeMos();
        }
        bool isOpenChargeMos = false;
        private void btnOpenChargeMos_Click(object sender, RoutedEventArgs e)
        {
            isOpenChargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_OpenChargeMos();
        }
        bool isExitDischargeMos = false;
        private void btnExitDischargeMos_Click(object sender, RoutedEventArgs e)
        {
            isExitDischargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_ExitDischargeMos();
        }
        bool isCloseDischargeMos = false;
        private void btnCloseDischargeMos_Click(object sender, RoutedEventArgs e)
        {
            isCloseDischargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_CloseDischargeMos();
        }
        bool isOpenDischargeMos = false;
        private void btnOpenDischargeMos_Click(object sender, RoutedEventArgs e)
        {
            isOpenDischargeMos = true;
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            BqProtocol.BqInstance.BQ_OpenDischargeMos();
        }

        private void btnSettingBatteryStatus_Click(object sender, RoutedEventArgs e)
        {
            byte status = Convert.ToByte(cmbBatteryStatus.SelectedIndex);

            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            DDProtocol.DdProtocol.DdInstance.DD_SettingBatteryStatus(status);
        }

        public void HandleRecvSettingBatteryStatusEvent(object sender, CustomRecvDataEventArgs e)
        {
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
            MessageBox.Show("设置电池使用状态成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        bool isEnterProductionMode = false;
        private void btnEnterProductionMode_Click(object sender, RoutedEventArgs e)
        {
            uint days = 0;
            if (uint.TryParse(tbCountDown.Text.Trim(), out days))
            {
                BqProtocol.BqInstance.m_bIsStopCommunication = true;
                Thread.Sleep(200);
                isEnterProductionMode = true;
                BqProtocol.BqInstance.BQ_EnterProductionMode((byte)days);
            }
            else
            {
                MessageBox.Show("请输入正确的生产模式倒计时天数！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        bool isExitProductionMode = false;
        private void btnExitProductionMode_Click(object sender, RoutedEventArgs e)
        {
            BqProtocol.BqInstance.m_bIsStopCommunication = true;
            Thread.Sleep(200);
            isExitProductionMode = true;
            BqProtocol.BqInstance.BQ_ExitProductionMode();
        }
    }
}
