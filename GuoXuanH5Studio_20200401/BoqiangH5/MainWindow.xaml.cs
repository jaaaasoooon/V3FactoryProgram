using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using BoqiangH5.ISO15765;
using System.Windows.Media.Animation;
using BoqiangH5Entity;
using BoqiangH5.DDProtocol;
using BoqiangH5.BQProtocol;
using BoqiangH5Repository;
using System.IO.Ports;
using System.Windows.Documents;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using DBService;
using System.Linq;

namespace BoqiangH5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static OperateType m_OperateType = OperateType.NoAction;

        public static StatusBarInfo m_statusBarInfo = new StatusBarInfo();

        public static ZLGFuction zlgFuc = DataLinkLayer.DllZLGFun;

        public static byte[] byRecvData = new byte[0xC8];

        public static Color m_green = Color.FromArgb(255, 100, 255, 137);
        public static Color m_red = Color.FromArgb(255, 251, 1, 1);
        public static Color m_black = Color.FromArgb(255, 60, 60, 60);
        public static Color m_white = Color.FromArgb(255, 255, 255, 255);

        SolidColorBrush defBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x51, 0x5C, 0x66));  
        SolidColorBrush enterBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x2F, 0x36, 0x3C));  
        SolidColorBrush selectBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x23, 0x26, 0x29));

        public static bool bIsBreak = false;

        string strSelectMenu = "gridMenuInfo";

        public static SerialPort serialPort;
        public static SerialPort voltmeterSerialPort;
        public static SerialPort amperemeterSerialPort;
        public static SerialPort th10sbSerialPort;
        public static SerialPort kp184SerialPort_2;
        public static string UserID;
        public static int RoleID;
        public MainWindow(string userID,int roleID)
        {
            UserID = userID;
            RoleID = roleID;
            XmlHelper.LoadConfigInfo(false);//加载一键出厂配置信息
            InitializeComponent();

            InitRecvDataEvenHandle();
        }

        private void GetUserInfo()
        {
            try
            {
                using (V3Entities V3 = new V3Entities())
                {
                    var user = V3.users.FirstOrDefault(p => p.UserID == UserID);
                    if(user != null)
                    {
                        string userStr = string.Format("用户：{0}（{1}）", user.UserName, user.UserID);
                        labUser.Content = userStr;
                    }
                    else
                    {
                        MessageBox.Show("找不到ID为 {0} 的用户！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region 根据用户角色限制功能
            if(RoleID == 4)
            {
                ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                ucEepromWnd.Visibility = Visibility.Hidden;
                ucMcuWnd.Visibility = Visibility.Hidden;
                ucAdjustWnd.Visibility = Visibility.Hidden;
                ucDebugWnd.Visibility = Visibility.Hidden;
                ucProtectParamWnd.Visibility = Visibility.Hidden;
                ucDdRecordWnd.Visibility = Visibility.Hidden;
                ucProtectParamWnd.StartOrStopTimer(true);
                ucQueryWnd.Visibility = Visibility.Hidden;
                ucRepairWnd.Visibility = Visibility.Hidden;

                gridMenuInfo.Visibility = Visibility.Visible;
                gridMenuEeprom.Visibility = Visibility.Hidden;
                gridMenuMcu.Visibility = Visibility.Hidden;
                gridMenuParam.Visibility = Visibility.Hidden;
                gridMenuAdjust.Visibility = Visibility.Hidden;
                gridMenuDebug.Visibility = Visibility.Hidden;
                gridMenuDidiInfo.Visibility = Visibility.Hidden;
                gridMenuQuery.Visibility = Visibility.Hidden;
                gridMenuRecord.Visibility = Visibility.Hidden;
                gridMenuRepair.Visibility = Visibility.Hidden;
                Grid.SetRow(borderMsg, 0);
            }
            else if(RoleID == 3)
            {
                ucQueryWnd.Visibility = Visibility.Hidden;
                gridMenuQuery.Visibility = Visibility.Hidden;
                Grid.SetRow(borderRepair, 8);
            }
            GetUserInfo();

            //根据MAC地址设置限定功能
            ucBqBmsInfoWnd.GetMacSetting();
            #endregion

            ////全局异常捕捉
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppLayProtocol.Initialize();
            AppLayProtocol.RaiseAppLayerProtocolEvent += HandlerAppLayerProtocolEvent;

            this.labOnLine.DataContext = m_statusBarInfo;
                        
            menuConnect.IsEnabled = true;
            menuBreak.IsEnabled = false;
            menuSetting.IsEnabled = true;
   
            zlgFuc = (ZLGFuction)FindResource("ZLGCAN");
            zlgFuc.RaiseZLGRecvDataEvent += HandlerZLGRecvDataEvent;

            //zlgCANFuc = (ZLGCANFuction)FindResource("ZLGCANFD");
            //zlgCANFuc.RaiseZLGRecvDataEvent += HandlerZLGRecvDataEvent;


            gridMenuInfo.Background = selectBrush;

            ucBqBmsInfoWnd.ShallowSleepEvent += Sleep_Clicked;//点击浅休眠事件
            ucBqBmsInfoWnd.DeepSleepEvent += Sleep_Clicked;//点击深休眠事件

            //ucEepromWnd.GetFileEvent += OnGetEepromFile;
            //ucMcuWnd.GetFileEvent += OnGetMcuFile;
            ucBqBmsInfoWnd.WriteEepromEvent += OnWriteEepromEvent;
            ucBqBmsInfoWnd.WriteMcuEvent += OnWriteMcuEvent;
            ucBqBmsInfoWnd.RequireReadRTCEvent += OnRequireReadRTCEvent;
            ucAdjustWnd.ReadRTCOverEvent += OnReadRTCOverEvent;
            ucBqBmsInfoWnd.RequireAdjustRTCEvent += OnRequireAdjustRTCEvent;
            ucAdjustWnd.AdjustRTCOverEvent += OnAdjustRTCOverEvent;
            ucBqBmsInfoWnd.RequireReadBootMsgEvent += OnRequireReadBootMsgEvent;
            ucDebugWnd.ReadBootOverEvent += OnReadBootOverEvent;
            ucBqBmsInfoWnd.RequireAdjustSOCEvent += OnRequireAdjustSocEvent;
            ucDebugWnd.AlterSOCOverEvent += OnAlterSOCOverEvent;
            ucBqBmsInfoWnd.RequireAdjustZeroCurrentEvent += OnRequireAdjustZeroCurrentEvent;
            ucAdjustWnd.AdjustZeroCurrentOverEvent += OnAdjustZeroCurrentOverEvent;
            ucBqBmsInfoWnd.RequireAdjust10ACurrentEvent += OnRequireAdjust10ACurrentEvent;
            ucAdjustWnd.Adjust10ACurrentOverEvent += OnAdjust10ACurrentOverEvent;
            ucBqBmsInfoWnd.RequireWriteEepromEvent += OnRequireWriteEepromEvent;
            ucEepromWnd.WriteEepromDataOverEvent += OnWriteEepromDataOverEvent;
            ucBqBmsInfoWnd.RequireWriteMcuEvent += OnRequireWriteMcuEvent;
            ucMcuWnd.WriteMcuOverEvent += OnWriteMcuOverEvent;
            ucBqBmsInfoWnd.RequirePowerOnEvent += OnRequirePowerOnEvent;
            ucBqBmsInfoWnd.RequirePowerOffEvent += OnRequirePowerOffEvent;
            ucBqBmsInfoWnd.RequireCheckEepromEvent += OnRequireCheckEepomEvent;
            ucEepromWnd.CheckEepromParamOKEvent += OnCheckEepromParamOKEvent;
            ucBqBmsInfoWnd.RequireCheckMCUEvent += OnRequireCheckMCUEvent;
            ucMcuWnd.CheckMCUParamOKEvent += OnCheckMCUParamOKEvent;
            //ucBqBmsInfoWnd.OneClickEvent += OnOneClickEvent;
            ucMcuWnd.UpdateMcuConfigOKEvent += OnUpdateMcuConfigOKEvent;
            ucDebugWnd.PowerOnOverEvent += OnPowerOnOverEvent;
            ucDebugWnd.PowerOffOverEvent += OnPowerOffOverEvent;
            ucBqBmsInfoWnd.RequireReadRecordEvent += OnRequireReadRecordEvent;
            ucDdRecordWnd.GetRecordsEvent += OnGetRecordsEvent;
            ucMcuWnd.CheckMCUMsgEvent += OnCheckMCUMsgEvent;
            ucBqBmsInfoWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucEepromWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucMcuWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucAdjustWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucDdRecordWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucQueryWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucRepairWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;
            ucDebugWnd.GetUIDEvent += OnGetUIDEvent;
            ucBqBmsInfoWnd.IsCompanyMatchEvent += OnIsCompanyMatchEvent;
            ucProtectParamWnd.RequireReadUIDEvent += OnRequireReadUIDEvent;

            ucDdRecordWnd.RequireReadDeviceInfoEvent += OnRequireReadBqDeviceInfoEvent;
            ucBqBmsInfoWnd.GetBqDeviceInfoEvent += OnGetBqDeviceInfoEvent;
        }
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //throw new NotImplementedException();
            MessageBox.Show(e.Exception.Message + "\r\n" + e.Exception.ToString());
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }
        //点击休眠，调用断开函数，断开与BMS的连接
        private void Sleep_Clicked(object sender, EventArgs e)
        {
            MenuBreak(false);
        }
        void OnUpdateMcuConfigOKEvent(object sender, EventArgs<string> e)
        {
            //ucBqBmsInfoWnd.tbMcuFileName.Text = e.Args;
            MessageBox.Show("更新MCU配置文件成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        //void OnOneClickEvent(object sender, EventArgs<bool> e)
        //{
        //    //ucProtectParamWnd.StartOrStopTimer(e.Args);
        //}
        void OnWriteEepromEvent(object sender,EventArgs<string> e)
        {
            ucEepromWnd.RequireWriteEeprom(e.Args,false);
        }
        void OnWriteMcuEvent(object sender, EventArgs<string> e)
        {
            ucMcuWnd.RequireWriteMcu(e.Args,false);
        }
        void OnRequireReadRTCEvent(object sender, EventArgs e)
        {
            ucAdjustWnd.RequireReadRTC();
        }
        void OnReadRTCOverEvent(object sender,EventArgs<string> e)
        {
            ucBqBmsInfoWnd.ReadRTCOver(e.Args);
        }
        void OnRequireAdjustRTCEvent(object sender, EventArgs e)
        {
            ucAdjustWnd.RequireAdjustRTC(true);
        }
        void OnAdjustRTCOverEvent(object sender,EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.AdjustRTCOver(e.Args);
        }
        void OnRequireReadBootMsgEvent(object sender,EventArgs e)
        {
            ucDebugWnd.RequireReadBoot(true);
        }
        void OnReadBootOverEvent(object sender, EventArgs<string> e)
        {
            //ucBqBmsInfoWnd.ReadBootOver(e.Args);
        }
        void OnRequireAdjustSocEvent(object sender, EventArgs<string> e)
        {
            ucDebugWnd.RequireAlterSOC(e.Args);
        }
        void OnAlterSOCOverEvent(object sender, EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.AlterSOCOver(e.Args);
        }
        void OnRequireAdjustZeroCurrentEvent(object sender,EventArgs e)
        {
            ucAdjustWnd.RequireAdjustZeroCurrent();
        }
        void OnAdjustZeroCurrentOverEvent(object sender, EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.AdjustZeroCurrentOver(e.Args);
        }
        void OnRequireAdjust10ACurrentEvent(object sender, EventArgs e)
        {
            ucAdjustWnd.RequireAdjust10ACurrent();
        }
        void OnAdjust10ACurrentOverEvent(object sender, EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.Adjust10ACurrentOver(e.Args);
        }
        void OnRequireWriteEepromEvent(object sender, EventArgs<string> e)
        {
            ucEepromWnd.RequireWriteEeprom(e.Args,true);
        }
        void OnWriteEepromDataOverEvent(object sender,EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.WriteEepromOver(e.Args);
        }
        void OnRequireWriteMcuEvent(object sender, EventArgs<string> e)
        {
            ucMcuWnd.RequireWriteMcu(e.Args, true);
        }
        void OnWriteMcuOverEvent(object sender, EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.WriteMcuOver(e.Args);
        }
        void OnRequirePowerOnEvent(object sender,EventArgs e)
        {
            ucDebugWnd.RequirePowerOn();
        }
        void OnRequirePowerOffEvent(object sender, EventArgs e)
        {
            ucDebugWnd.RequirePowerOff();
        }

        void OnRequireCheckEepomEvent(object sender, EventArgs<string> e)
        {
            ucEepromWnd.RequireCheckEeprom(e.Args);
        }
        void OnCheckEepromParamOKEvent(object sender,EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.CheckEepromParamOK(e.Args);
        }

        void OnRequireCheckMCUEvent(object sender, EventArgs<string> e)
        {
            ucMcuWnd.RequireCheckMCU(e.Args);
        }
        void OnCheckMCUParamOKEvent(object sender, EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.CheckMCUParamOK(e.Args);
        }
        //void OnGetEepromFile(object sender, EventArgs<string> e)
        //{
        //    ucBqBmsInfoWnd.tbEepromFileName.Text = e.Args;
        //}
        //void OnGetMcuFile(object sender, EventArgs<string> e)
        //{
        //    ucBqBmsInfoWnd.tbMcuFileName.Text = e.Args;
        //}
        void OnPowerOnOverEvent(object sender,EventArgs e)
        {
            ucBqBmsInfoWnd.PowerOnOrPowerOffOver(false);
        }
        void OnPowerOffOverEvent(object sender, EventArgs e)
        {
            ucBqBmsInfoWnd.PowerOnOrPowerOffOver(true);
        }

        void OnRequireReadRecordEvent(object sender,EventArgs e)
        {
            //ucRecordWnd.RequireReadRecord();
            ucDdRecordWnd.RequireReadRecord();
        }

        void OnGetRecordsEvent(object sender,EventArgs<bool> e)
        {
            ucBqBmsInfoWnd.GetRecordsOver(e.Args);
        }

        void OnCheckMCUMsgEvent(object sender,EventArgs<Tuple<bool,string>> e)
        {
            ucBqBmsInfoWnd.ShowCheckMCuMsg(e.Args.Item1, e.Args.Item2);
        }

        void OnRequireReadUIDEvent(object sender,EventArgs<string> e)
        {
            ucDebugWnd.RequireReadUID(e.Args);
        }
        public void OnRequireReadBqDeviceInfoEvent(object sender, EventArgs e)
        {
            ucBqBmsInfoWnd.RequireReadBqDeviceInfo();
        }
        void OnGetUIDEvent(object sender,EventArgs<Tuple<string,string>> e)
        {
            if (e.Args.Item1 == "BmsInfo")
                ucBqBmsInfoWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Eeprom")
                ucEepromWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Mcu")
                ucMcuWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Adjust")
                ucAdjustWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Record")
                ucDdRecordWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Query")
                ucQueryWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "Repair")
                ucRepairWnd.SetUID(e.Args.Item2);
            else if (e.Args.Item1 == "RepairQuery")
                ucRepairWnd.SetQueryUID(e.Args.Item2);
            else if (e.Args.Item1 == "RepairBoth")
                ucRepairWnd.SetBothUID(e.Args.Item2);
            else if (e.Args.Item1 == "ProtectParam")
                ucProtectParamWnd.SetUID(e.Args.Item2);
        }
        public void OnGetBqDeviceInfoEvent(object sender, EventArgs<List<string>> e)
        {
            ucDdRecordWnd.SetBqDeviceInfo(e.Args);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            zlgFuc.StopDevice();

            this.Close();
        }

         public void HandleRaiseCloseEvent(object sender, EventArgs e)
        {
             Window_Closed(this, null);
        }

        private void HandlerAppLayerProtocolEvent(object sender, EventArgs e)
        {
            var appLayerEvent = e as AppLayerEvent;
            if (appLayerEvent == null)
            {
                return;
            }
            switch (appLayerEvent.eventType)
            {
                case AppEventType.AppSendEvent:
                    break;
                case AppEventType.AppReceiveEvent:

                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                    {
                        treatRecvFrame(appLayerEvent);
                    }));
                    break;
                case AppEventType.Other:
                    break;
                default:
                    break;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (null == menuItem)
                return;

            switch (menuItem.Name)
            {
                case "menuConnect":
                    MenuConnect();
                    break;

                case "menuBreak":
                    MenuBreak(false);
                    break;

                case "menuControl":
                    BmsControl();
                    break;

                case "menuCommunicateRecord":
                                        
                    break;

                case "menuRealtimeRecords":
                    break;

                case "menuDTCInfo":
                    break;

                case "menuAdjustAndControl":
                    break;

                case "menuPackInfo":               
                    break;

                case "menuSetting":
                    if(RoleID == 1 || RoleID == 2)
                    {
                        if (m_statusBarInfo.IsOnline == true)
                        {
                            MessageBox.Show("请断开连接再进行设置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            CAN_Setting();
                        }
                    }
                    else
                    {
                        MessageBox.Show("您无权进行设置操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "menuFactorySetting":
                    if (RoleID == 1 || RoleID == 2)
                    {
                        OneClickFactorySetting wnd = new OneClickFactorySetting();
                        wnd.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("您无权进行一键出厂的设置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "menuMacSetting":
                    if (RoleID == 1 || RoleID == 2)
                    {
                        if (m_statusBarInfo.IsOnline == true)
                        {
                            MessageBox.Show("请断开连接再进行设置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            SettingMacWnd wnd = new SettingMacWnd();
                            wnd.ShowDialog();
                        }
                    }
                    else
                    {
                        MessageBox.Show("您无权进行Mac设置操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "menuLanguageEn":

                    break;

                case "menuUpdateSystem":
                   
                    break;
                case "menuAddUser":
                    if(RoleID == 1)
                    {
                        AddUserWnd _wnd = new AddUserWnd();
                        _wnd.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("您无权进行用户的添加！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                default:
                    break;
            }
        }
        bool isCompanyMatch = false;
        /// <summary>
        /// 连接
        /// </summary>
        private void MenuConnect()
        {
            if (ucDdBmsInfoWnd.Visibility == Visibility.Visible || ucDdRecordWnd.Visibility == Visibility.Visible)
                SelectCANWnd.m_H5Protocol = H5Protocol.DI_DI;
            else
                SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
            int ret = zlgFuc.RunDevice();
            if (ret == 1)
            {
                labTip.Content = "打开ZLG接口卡失败，请检查设备连接！";
                return;
            }
            else if (ret == 2)
            {
                labTip.Content = "初始化CAN通道失败，请检查设备连接！";
                return;
            }
            else if (ret == 3)
            {
                labTip.Content = "启动CAN通道失败，请检查设备连接！";
                return;
            }


            try
            {
                if (SelectCANWnd.m_IsUsingKP182)
                {
                    if (string.IsNullOrEmpty(XmlHelper.m_strKP182Com) || (string.IsNullOrEmpty(XmlHelper.m_strKP182BaudRate)))
                    {
                        MessageBox.Show("请设置KP184-01的串口号和波特率！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int nBaudrate = int.Parse(XmlHelper.m_strKP182BaudRate);
                    serialPort = new SerialPort(XmlHelper.m_strKP182Com, nBaudrate, Parity.None, 8, StopBits.One);
                    serialPort.ReadTimeout = 3000;
                    serialPort.WriteTimeout = 3000;
                    serialPort.Open();

                    if (serialPort.IsOpen)
                    {
                        labTip.Content += string.Format("     KP184-01串口打开成功");
                    }
                    else
                    {
                        labTip.Content += string.Format("     KP184-01串口打开失败！");
                    }
                }

                if (SelectCANWnd.m_IsUsingKP182_2)
                {
                    if (string.IsNullOrEmpty(XmlHelper.m_strKP182Com_2) || (string.IsNullOrEmpty(XmlHelper.m_strKP182BaudRate_2)))
                    {
                        MessageBox.Show("请设置KP184-02的串口号和波特率！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int nBaudrate = int.Parse(XmlHelper.m_strKP182BaudRate_2);
                    kp184SerialPort_2 = new SerialPort(XmlHelper.m_strKP182Com_2, nBaudrate, Parity.None, 8, StopBits.One);
                    kp184SerialPort_2.ReadTimeout = 3000;
                    kp184SerialPort_2.WriteTimeout = 3000;
                    kp184SerialPort_2.Open();

                    if (kp184SerialPort_2.IsOpen)
                    {
                        labTip.Content += string.Format("     KP184-02串口打开成功");
                    }
                    else
                    {
                        labTip.Content += string.Format("     KP184-02串口打开失败！");
                    }
                }

                if (SelectCANWnd.m_IsUsingVoltmeter)
                {
                    if (string.IsNullOrEmpty(XmlHelper.m_strVoltmeterCom) || (string.IsNullOrEmpty(XmlHelper.m_strVoltmeterBaudRate)))
                    {
                        MessageBox.Show("请设置电压表的串口号和波特率！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int nBaudrate = int.Parse(XmlHelper.m_strVoltmeterBaudRate);
                    voltmeterSerialPort = new SerialPort(XmlHelper.m_strVoltmeterCom, nBaudrate, Parity.None, 8, StopBits.One);
                    voltmeterSerialPort.ReadTimeout = 3000;
                    voltmeterSerialPort.WriteTimeout = 3000;
                    voltmeterSerialPort.Open();

                    if (voltmeterSerialPort.IsOpen)
                    {
                        labTip.Content += string.Format("     电压表串口打开成功");
                    }
                    else
                    {
                        labTip.Content += string.Format("     电压表串口打开失败！");
                    }
                }
                if (SelectCANWnd.m_IsUsingAmperemeter)
                {
                    if(string.IsNullOrEmpty(XmlHelper.m_strAmperemeterCom) || (string.IsNullOrEmpty(XmlHelper.m_strAmperemeterBaudRate)))
                    {
                        MessageBox.Show("请设置电流表的串口号和波特率！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int nBaudrate = int.Parse(XmlHelper.m_strAmperemeterBaudRate);
                    amperemeterSerialPort = new SerialPort(XmlHelper.m_strAmperemeterCom, nBaudrate, Parity.None, 8, StopBits.One);
                    amperemeterSerialPort.ReadTimeout = 3000;
                    amperemeterSerialPort.WriteTimeout = 3000;
                    amperemeterSerialPort.Open();

                    if (amperemeterSerialPort.IsOpen)
                    {
                        labTip.Content += string.Format("     电流表串口打开成功");
                    }
                    else
                    {
                        labTip.Content += string.Format("     电流表串口打开失败！");
                    }
                }

                if (SelectCANWnd.m_IsUsingTH10SB)
                {
                    if (string.IsNullOrEmpty(XmlHelper.m_strTH10SBCom) || (string.IsNullOrEmpty(XmlHelper.m_strTH10SBBaudRate)))
                    {
                        MessageBox.Show("请设置TH10S-B温湿度仪的串口号和波特率！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int nBaudrate = int.Parse(XmlHelper.m_strTH10SBBaudRate);
                    th10sbSerialPort = new SerialPort(XmlHelper.m_strTH10SBCom, nBaudrate, Parity.None, 8, StopBits.One);
                    th10sbSerialPort.ReadTimeout = 3000;
                    th10sbSerialPort.WriteTimeout = 3000;
                    th10sbSerialPort.Open();

                    if (th10sbSerialPort.IsOpen)
                    {
                        labTip.Content += string.Format("     TH10S-B温湿度仪串口打开成功");
                    }
                    else
                    {
                        labTip.Content += string.Format("     TH10S-B温湿度仪串口打开失败！");
                    }
                }

                //连接成功，保留当天日志并插入分割，删除7天前的日志
                string dir = AppDomain.CurrentDomain.BaseDirectory + @"log";
                string logPath = AppDomain.CurrentDomain.BaseDirectory + @"log\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                if (Directory.Exists(dir))
                {
                    string[] fileSystemEntries = Directory.GetFileSystemEntries(dir);
                    for (int i = 0; i < fileSystemEntries.Length; i++)
                    {
                        string file = fileSystemEntries[i];
                        if (File.Exists(file))
                        {
                            if (file != logPath)
                            {
                                DateTime dt = File.GetCreationTime(file);
                                TimeSpan ts = DateTime.Now.Subtract(dt);
                                if (ts.Days > 7)
                                {
                                    File.Delete(file);
                                }
                            }
                            else
                            {
                                StreamWriter sw = new StreamWriter(logPath, true, System.Text.Encoding.Default);
                                for (int j = 0; j < 5; j++)
                                {
                                    sw.WriteLine("\r\n");
                                }
                                sw.Close();
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            bIsBreak = false;

            menuConnect.IsEnabled = false;
            TextBlock tb = (TextBlock)menuConnect.Header;
            tb.Foreground = new SolidColorBrush(Colors.White);
            menuBreak.IsEnabled = true;
            tb = (TextBlock)menuBreak.Header;
            tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 0, 0));
            //menuConnect.Icon = new Image() { Source = new  BitmapImage( new Uri("Images/start_gray.png", UriKind.RelativeOrAbsolute) )};
            //menuBreak.Icon = new Image() { Source = new BitmapImage(new Uri("Images/stop_red.png", UriKind.RelativeOrAbsolute)) };
            connectBrush.Color = m_white;
            breakBrush.Color = m_red;
            //m_statusBarInfo.IsOnline = true;
            statusBrush.Color = m_red;
            m_statusBarInfo.OnlineStatus = "离线";
            labTip.Content = "类型:" + ZLGInfo.DevType.ToString() +
                             "    索引号: " + zlgFuc.zlgInfo.DevIndex.ToString() +
                             "    通道号: " + zlgFuc.zlgInfo.DevChannel.ToString() +
                             "    波特率: " + ZLGInfo.Baudrate.ToString();

            isCompanyMatch = false;
            //BqProtocol.BqInstance.SetTimer();
            ThreadPool.QueueUserWorkItem(BqProtocol.BqInstance.ThreadReadMasterTeleData);
            ThreadPool.QueueUserWorkItem(DdProtocol.DdInstance.ThreadReadMasterTeleData);
        }

        /// <summary>
        /// 断开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuBreak(bool isReconnection)
        {
            if (isReconnection == false)
            {
                bIsBreak = true;
                //if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
                //    BqProtocol.BqInstance.StopTimerHandshake();//关闭握手
                //else
                //    DdProtocol.DdInstance.StopTimerReadSOH();

                zlgFuc.StopDevice();
                zlgFuc.zlgInfo.IsRecFrame = false;

                menuConnect.IsEnabled = true;
                TextBlock tb = (TextBlock)menuConnect.Header;
                tb.Foreground = new SolidColorBrush(Colors.LightGreen);
                menuBreak.IsEnabled = false;
                tb = (TextBlock)menuBreak.Header;
                tb.Foreground = new SolidColorBrush(Colors.White);
                menuSetting.IsEnabled = true;
                //menuConnect.Icon = new Image() { Source = new BitmapImage(new Uri("Images/start_green.png", UriKind.RelativeOrAbsolute)) };
                //menuBreak.Icon = new Image() { Source = new BitmapImage(new Uri("Images/stop_gray.png", UriKind.RelativeOrAbsolute)) };
                labTip.Content = "";
                m_statusBarInfo.OnlineStatus = "断开";
                statusBrush.Color = m_black;
                breakBrush.Color = m_white;
                connectBrush.Color = m_green;
                //epNormal.Fill = new SolidColorBrush(Colors.White);
                //tbNormal.Foreground = new SolidColorBrush(Colors.White);
                //epProtect.Fill = new SolidColorBrush(Colors.White);
                //tbProtect.Foreground = new SolidColorBrush(Colors.White);
                //epWarning.Fill = new SolidColorBrush(Colors.White);
                //tbWarning.Foreground = new SolidColorBrush(Colors.White);
                if(SelectCANWnd.m_IsUsingKP182 && serialPort != null)
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                }
                if (SelectCANWnd.m_IsUsingKP182_2 && kp184SerialPort_2 != null)
                {
                    if (kp184SerialPort_2.IsOpen)
                    {
                        kp184SerialPort_2.Close();
                    }
                }
                if (SelectCANWnd.m_IsUsingVoltmeter && voltmeterSerialPort != null)
                {
                    if (voltmeterSerialPort.IsOpen)
                    {
                        voltmeterSerialPort.Close();
                    }
                }

                if (SelectCANWnd.m_IsUsingAmperemeter && amperemeterSerialPort != null)
                {
                    if (amperemeterSerialPort.IsOpen)
                    {
                        amperemeterSerialPort.Close();
                    }
                }
            }
            else
            {
                m_statusBarInfo.OnlineStatus = "离线";
                statusBrush.Color = m_red;
                labTip.Content = "系统连接断开，正在重新连接......";
            }
            m_statusBarInfo.IsOnline = false;

            if (SelectCANWnd.m_H5Protocol == H5Protocol.BO_QIANG)
            {
                ucBqBmsInfoWnd.SetOffLineUIStatus();
            }
            else
            {
                // bms
                ucDdBmsInfoWnd.SetOffLineUIStatus();
            }
            //ucEepromWnd.isSetPassword = false;
            //ucMcuWnd.isSetPassword = false;
            OnRaiseEepromWndUpdateEvent(null);
            OnRaiseMcuWndUpdateEvent(null);
            OnRaiseAdjustWndUpdateEvent(null);
            OnRaiseDebugWndUpdateEvent(null);
            OnRaiseQueryWndUpdateEvent(null);
            OnRaiseRepairWndUpdateEvent(null);
        }

        public void OnIsCompanyMatchEvent(object sender, EventArgs<bool> e)
        {
            if (e.Args)
                isCompanyMatch = true;
            else
            {
                isCompanyMatch = false;
                MenuBreak(false);
                MessageBox.Show("BMS保护板和上位机不匹配！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void CAN_Setting()
        {
            zlgFuc = DataLinkLayer.DllZLGFun;
            SelectCANWnd settingWindow = new SelectCANWnd();
            settingWindow.RaiseCloseEvent += HandleRaiseCloseEvent;
            settingWindow.ShowDialog();
            //settingWindow.Activate();
        }

        private void BmsControl()
        {
            ControlWnd cw = new ControlWnd();
            cw.ShowDialog();

        }


        private Storyboard CommMaskStoryBoard;
        public enum CommunicationStatus { Txd = 0, RcvOk = 1, RcvError = 2 };

        private void BlinkCommStatus(CommunicationStatus which)
        {                
            if (which == CommunicationStatus.Txd)
                CommMaskStoryBoard = InitBlinkLedStoryBoard("commAnimatedBrush", Colors.LightGreen, Colors.Black, 0.5);
            else if (which == CommunicationStatus.RcvOk)
                CommMaskStoryBoard = InitBlinkLedStoryBoard("commAnimatedBrush", Colors.LightGreen, Colors.Black, 0.5);
            else
                CommMaskStoryBoard = InitBlinkLedStoryBoard("commAnimatedBrush", Colors.Red, Colors.Black, 0.5);

            CommMaskStoryBoard.Begin(this);
            
        }

        private Storyboard InitBlinkLedStoryBoard(string tarName, Color lightOnlColor, Color lightOfflColor, double seconds)
        {
            ColorAnimationUsingKeyFrames colorAnimation = new ColorAnimationUsingKeyFrames();
            colorAnimation.Duration = TimeSpan.FromSeconds(1);
            colorAnimation.FillBehavior = FillBehavior.Stop;

            colorAnimation.KeyFrames.Add(new DiscreteColorKeyFrame(lightOnlColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));

            colorAnimation.KeyFrames.Add(new DiscreteColorKeyFrame(lightOfflColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(seconds))));

            Storyboard.SetTargetName(colorAnimation, tarName);
            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath(SolidColorBrush.ColorProperty));

            // Create a storyboard to apply the animation.
            Storyboard myStoryboard = new Storyboard();
            myStoryboard.Children.Add(colorAnimation);

            return myStoryboard;
        }         

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            byte[] arrLen = BitConverter.GetBytes(0x1001);
            byte[] bytes = System.Text.Encoding.Default.GetBytes("1001");

            RequestFrameTestWnd reqFrameWnd = new RequestFrameTestWnd();
            reqFrameWnd.Show();
        }      

        private void gridMenu_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Grid gridMenu = sender as Grid;
            if(gridMenu == null)
            {
                return;
            }           

            switch(gridMenu.Name)
            {
                case "gridMenuInfo":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;
                    break;

                case "gridMenuParam":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Visible;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(false);

                    gridMenuInfo.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;
                    break;

                case "gridMenuEeprom":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Visible;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;
                    OnRaiseEepromWndUpdateEvent(null);
                    break;

                case "gridMenuMcu":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Visible;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;
                    OnRaiseMcuWndUpdateEvent(null);
                    break;

                case "gridMenuRecord":
                    SelectCANWnd.m_H5Protocol = H5Protocol.DI_DI;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Visible;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;

                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;
                    break;

                case "gridMenuAdjust":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Visible;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;

                    OnRaiseAdjustWndUpdateEvent(null);
                    break;

                case "gridMenuDebug":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Visible;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;

                    OnRaiseDebugWndUpdateEvent(null);
                    break;

                case "gridMenuQuery":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Visible;
                    ucRepairWnd.Visibility = Visibility.Hidden;

                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;

                    OnRaiseQueryWndUpdateEvent(null); 
                    break;
                case "gridMenuRepair":
                    SelectCANWnd.m_H5Protocol = H5Protocol.BO_QIANG;
                    ucBqBmsInfoWnd.Visibility = Visibility.Visible;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Visible;
                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuInfo.Background = defBrush;
                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuDidiInfo.Background = defBrush;

                    OnRaiseRepairWndUpdateEvent(null);
                    break;
                case "gridMenuDidiInfo":
                    SelectCANWnd.m_H5Protocol = H5Protocol.DI_DI;
                    ucBqBmsInfoWnd.Visibility = Visibility.Hidden;
                    ucDdBmsInfoWnd.Visibility = Visibility.Visible;
                    ucEepromWnd.Visibility = Visibility.Hidden;
                    ucMcuWnd.Visibility = Visibility.Hidden;
                    ucAdjustWnd.Visibility = Visibility.Hidden;
                    ucDebugWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.Visibility = Visibility.Hidden;
                    //ucRecordWnd.Visibility = Visibility.Hidden;
                    ucDdRecordWnd.Visibility = Visibility.Hidden;
                    ucQueryWnd.Visibility = Visibility.Hidden;
                    ucRepairWnd.Visibility = Visibility.Hidden;
                    ucProtectParamWnd.StartOrStopTimer(true);

                    gridMenuEeprom.Background = defBrush;
                    gridMenuMcu.Background = defBrush;
                    gridMenuRecord.Background = defBrush;
                    gridMenuAdjust.Background = defBrush;
                    gridMenuDebug.Background = defBrush;
                    gridMenuParam.Background = defBrush;
                    gridMenuQuery.Background = defBrush;
                    gridMenuRepair.Background = defBrush;
                    gridMenuInfo.Background = defBrush;
                    break;
                default:
                    break;
            }

            gridMenu.Background = selectBrush; 
            strSelectMenu = gridMenu.Name;
        }

        private void gridMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Grid gridMenu = sender as Grid;
            if (gridMenu == null)
            {
                return;
            }

            gridMenu.Background = enterBrush;
        }

        private void gridMenu_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Grid gridMenu = sender as Grid;
            if (gridMenu == null)
            {
                return;
            }

            if (strSelectMenu == gridMenu.Name)
            {
                gridMenu.Background = selectBrush;
            }
            else
            {
                gridMenu.Background = defBrush; 
            }
        }

        //private void BtnMin_Click(object sender, RoutedEventArgs e)
        //{
        //    this.WindowState = WindowState.Minimized;
        //}

        //private void BtnClose_Click_1(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}

    }
}
