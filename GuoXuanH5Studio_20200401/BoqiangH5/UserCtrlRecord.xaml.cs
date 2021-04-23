using BoqiangH5.BQProtocol;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Text;
using System.Collections.ObjectModel;
using System.Timers;
using DBService;
using System.Linq;

namespace BoqiangH5
{
    /// <summary>
    /// UserCtrlRecord.xaml 的交互逻辑
    /// </summary>
    public partial class UserCtrlRecord : UserControl
    {
        public static ObservableCollection<H5RecordInfo> m_ListRecordsInfo = new ObservableCollection<H5RecordInfo>();
        string U_ID = "默认值";

        public UserCtrlRecord()
        {
            InitializeComponent();
            InitBqBmsInfoWnd();
        }

        private void InitBqBmsInfoWnd()
        {
            recordInfoDic.Clear();
            string strConfigFile = System.AppDomain.CurrentDomain.BaseDirectory + @"ProtocolFiles\bq_h5_record_info.xml";
            //XmlHelper.LoadBackupRecordConfig(strConfigFile, recordInfoDic, recordTypeDic, packInfoList, batteryInfoList);
        }

        Dictionary<string, string> recordInfoDic = new Dictionary<string, string>();
        Dictionary<string, string> recordTypeDic = new Dictionary<string, string>();
        List<Tuple<string, string, string>> packInfoList = new List<Tuple<string, string, string>>();
        List<Tuple<string, string, string>> batteryInfoList = new List<Tuple<string, string, string>>();
        private void ucRecordInfo_Loaded(object sender, RoutedEventArgs e)
        {
            m_ListRecordsInfo.Clear();
            dataGridRecord.Items.Clear();
            dataGridRecord.ItemsSource = m_ListRecordsInfo;
            //getDataEvent += RefreshData;
            if (SelectCANWnd.m_H5Protocol == H5Protocol.DI_DI)
            {
                btnReadData.IsEnabled = false;
                btnClear.IsEnabled = false;
                btnSave.IsEnabled = false;
            }
            btnStopRead.IsEnabled = false;
            timer = new System.Windows.Forms.Timer();
            timer.Tick += OnTimer;
            timer.Interval = 2000;
        }

        bool isRequireRead = false;
        System.Windows.Forms.Timer timer;
        public event EventHandler<EventArgs<bool>> GetRecordsEvent;
        public void RequireReadRecord()
        {
            //CSVFileHelper.WriteLogs("log", "Eeprom测试", "请求读故障记录！");
            isRequireRead = true;
            isRead = true;
            isStopRead = false;
            //timer = new System.Windows.Forms.Timer();
            //timer.Tick += OnTimer;
            //timer.Interval = 2000;
            timer.Start();
            BqProtocol.bReadBqBmsResp = true;
            BqProtocol.BqInstance.ReadRecordData(0);
        }
        private void OnTimer(object sender, EventArgs e)
        {
            //CSVFileHelper.WriteLogs("log", "Eeprom测试", "未读到故障记录！");
            isRequireRead = false;
            isRead = false;
            isStopRead = true;
            //CSVFileHelper.WriteLogs("log", "Eeprom测试", "关闭定时器开始！");
            timer.Stop();
            //CSVFileHelper.WriteLogs("log", "Eeprom测试", "关闭定时器完成！");

            GetRecordsEvent?.Invoke(this, new EventArgs<bool>(false));
            CSVFileHelper.WriteLogs("log", "Eeprom测试", "上报Eeprom损坏！");
        }
        bool isRead = false;
        private void btnReadData_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                if (isRead == false)
                {
                    m_ListRecordsInfo.Clear();
                    dataGridRecord.Items.Refresh();
                    isRead = false;
                    BqProtocol.bReadBqBmsResp = true;
                    BqProtocol.BqInstance.m_bIsStopCommunication = true;
                    BqProtocol.BqInstance.ReadRecordData(0);
                    btnStopRead.IsEnabled = true;
                    btnReadData.IsEnabled = false;
                    isRead = true;
                    isStopRead = false;
                    preTime = DateTime.Now;
                    //300ms没收到数据，认为失败，重读当前数据
                    Task.Factory.StartNew(() =>
                    {
                        int count = 0;
                        while (isRead)
                        {
                            if ((DateTime.Now - preTime) > new TimeSpan(0, 0, 0, 0, 250))
                            {
                                BqProtocol.bReadBqBmsResp = true;
                                BqProtocol.BqInstance.ReadRecordData(2);
                                preTime = DateTime.Now;
                                count++;
                            }
                            if(count > 2)
                            {
                                break;
                            }
                        }
                    });
                }
                else
                {
                    MessageBoxForm.Show("正在读取备份数据，请先停止读取，再进行操作！", "提示", 1000);
                    //MessageBox.Show("正在读取备份数据，请先停止读取，再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                //MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //AutoClosedMsgBox.Show("系统未连接，请连接后再进行操作！", "提示", 3000,MsgBoxStyle.BlueInfo_OK);
                MessageBoxForm.Show("系统未连接，请连接后再进行操作！", "提示", 1000);
            }
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = "csv files(*.csv)|*.csv";
                dlg.FileName = System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
                //dlg.InitialDirectory = "D:\\";
                dlg.AddExtension = false;
                dlg.RestoreDirectory = true;
                System.Nullable<bool> result = dlg.ShowDialog();
                if (result == true)
                {
                    string desFilePath = dlg.FileName.ToString();
                    if (File.Exists(desFilePath))
                    {
                        File.Delete(desFilePath);
                    }
                    CSVFileHelper.SaveRecordDataCSV(m_ListRecordsInfo, desFilePath);
                    MessageBoxForm.Show("备份数据保存成功！", "提示", 1000);
                }

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        bool isErase = false;
        private void btnClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("确定要擦除备份数据？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information))
                {
                    U_ID = "默认值";
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Record"));
                }
            }
            else
            {
                ////MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                MessageBoxForm.Show("系统未连接，请连接后再进行操作！", "提示", 1000);
            }
        }

        //private event EventHandler<EventArgs<bool>> getDataEvent;

        DateTime preTime;
        //bool isNewData = false;
        //bool isEnd = false;
        bool isStopRead = false;

        public void HandleReadRecordInfoDataEvent(object sender, CustomRecvDataEventArgs e)
        {
            try
            {
                //BoqiangH5Repository.CSVFileHelper.WriteLogs("log", "recv", "record A6\r\n");
                if (isStopRead)
                {
                    isRead = false;
                    BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    return;
                }
                if(isRead)
                {
                    H5RecordInfo info = ReadRecordInfo(e.RecvMsg);
                    if(isRequireRead)
                    {
                        //CSVFileHelper.WriteLogs("log", "Eeprom测试", "读到故障记录！");
                        isRequireRead = false;
                        isRead = false;
                        isStopRead = true;
                        //CSVFileHelper.WriteLogs("log", "Eeprom测试", "关定时器开始！");
                        timer.Stop();
                        //CSVFileHelper.WriteLogs("log", "Eeprom测试", "关定时器完成！");
                        //CSVFileHelper.WriteLogs("log", "Eeprom测试", "上报Eeprom正常！");
                        GetRecordsEvent?.Invoke(this, new EventArgs<bool>(true));
                        return;
                    }
                    if (info != null)
                    {
                        preTime = DateTime.Now;
                        if (info.RecordInfo == 0x00)
                        {
                            isRead = false;
                            //MessageBox.Show("没有备份数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxForm.Show("没有备份数据！", "提示", 1000);
                        }
                        else if (info.RecordInfo == 0x01 || info.RecordInfo == 0x02)
                        {
                            info.Index = m_ListRecordsInfo.Count + 1;
                            m_ListRecordsInfo.Add(info);
                            BqProtocol.bReadBqBmsResp = true;
                            BqProtocol.BqInstance.ReadRecordData(1);
                        }
                        else if (info.RecordInfo == 0x03)
                        {
                            info.Index = m_ListRecordsInfo.Count + 1;
                            m_ListRecordsInfo.Add(info);
                            isRead = false;
                            //MessageBox.Show("备份数据读取完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxForm.Show("备份数据读取完成！", "提示", 1000);
                            BqProtocol.BqInstance.m_bIsStopCommunication = false;
                            btnStopRead.IsEnabled = false;
                            btnReadData.IsEnabled = true;
                        }
                        else
                        {
                            BqProtocol.bReadBqBmsResp = true;
                            BqProtocol.BqInstance.ReadRecordData(2);
                        }
                    }
                    #region
                    //if (info != null)
                    //{
                    //    if (info.RecordInfo == 0x00)
                    //    {
                    //        MessageBox.Show("没有备份数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    }
                    //    else if (info.RecordInfo == 0x01)
                    //    {
                    //        isNewData = true;
                    //        isEnd = false;
                    //        info.Index = m_ListRecordsInfo.Count + 1;
                    //        m_ListRecordsInfo.Add(info);
                    //        getDataEvent?.Invoke(this, new EventArgs<bool>(false));
                    //        Task.Factory.StartNew(() =>
                    //        {
                    //            while (true)
                    //            {
                    //                if(isStopRead)
                    //                {
                    //                    isEnd = true;
                    //                    BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    //                    isRead = false;
                    //                    break;
                    //                }

                    //                if (isEnd == true)
                    //                {
                    //                    break;
                    //                }

                    //                if (isNewData)
                    //                {
                    //                    isNewData = false;
                    //                    preTime = DateTime.Now;
                    //                    BqProtocol.bReadBqBmsResp = true;
                    //                    BqProtocol.BqInstance.ReadRecordData(1);
                    //                }
                    //                else
                    //                {
                    //                    DateTime currentTime = DateTime.Now;
                    //                    if (currentTime - preTime > new TimeSpan(0, 0, 0,0,300))
                    //                    {
                    //                        isNewData = true;
                    //                    }

                    //                    if(currentTime - preTime > new TimeSpan(0,0,5))//超过5秒还未读到返回数据，则结束读取
                    //                    {
                    //                        isEnd = true;
                    //                        BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    //                        isRead = false;
                    //                        break;
                    //                    }
                    //                }
                    //            }
                    //            isEnd = false;//读取结束，将标志位标为false，以备下次读取
                    //        });
                    //    }
                    //    else if (info.RecordInfo == 0x02)
                    //    {
                    //        isEnd = false;
                    //        isNewData = true;
                    //        info.Index = m_ListRecordsInfo.Count + 1;
                    //        m_ListRecordsInfo.Add(info);

                    //        getDataEvent?.Invoke(this, new EventArgs<bool>(false));
                    //    }
                    //    else if (info.RecordInfo == 0x03)
                    //    {
                    //        isEnd = true;
                    //        info.Index = m_ListRecordsInfo.Count + 1;
                    //        m_ListRecordsInfo.Add(info);
                    //        getDataEvent?.Invoke(this, new EventArgs<bool>(true));
                    //        MessageBox.Show("备份数据读取完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    //        BqProtocol.BqInstance.m_bIsStopCommunication = false;
                    //        isRead = false;
                    //    }
                    //    else
                    //    {
                    //        System.Threading.Thread.Sleep(10);
                    //        BqProtocol.bReadBqBmsResp = true;
                    //        BqProtocol.BqInstance.ReadRecordData(2);
                    //    }
                    //}
                    #endregion
                }

            }
            catch (Exception ex)
            {
                btnReadData.IsEnabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        public event EventHandler<EventArgs<string>> RequireReadUIDEvent;
        public void SetUID(string uid)
        {
            try
            {
                U_ID = uid;
                BqProtocol.BqInstance.m_bIsStopCommunication = false;

                isErase = false;
                BqProtocol.bReadBqBmsResp = true;
                BqProtocol.BqInstance.EraseRecord();
                isErase = true;
            }
            catch (Exception ex)
            {

            }
        }
        public void HandleEraseInfoDataEvent(object sender, CustomRecvDataEventArgs e)
        {
            if(isErase)
            {
                BqProtocol.bReadBqBmsResp = true;
                if (e.RecvMsg[0] == 0xD6)
                {
                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.故障擦除, OperationResultEnum.成功, string.Empty);
                    MessageBoxForm.Show("备份数据擦除成功！", "擦除提示", 1000);
                    //MessageBox.Show("备份数据擦除成功！", "擦除提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    //MessageBoxForm.Show("备份数据擦除失败！", "擦除提示", 3000);
                    SaveOperationRecord(BoqiangH5Repository.CSVFileHelper.ToHexStrFromByte(BqProtocol.BqInstance.IssuedBytesList.ToArray()), OperationTypeEnum.故障擦除, OperationResultEnum.失败, "备份数据擦除失败！");
                    MessageBox.Show("备份数据擦除失败！", "擦除提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                isErase = false;
            }
        }

        private void btnStopRead_Click(object sender, RoutedEventArgs e)
        {
            isStopRead = true;
            isRead = false;
            btnReadData.IsEnabled = true;
            btnStopRead.IsEnabled = false;
            BqProtocol.BqInstance.m_bIsStopCommunication = false;
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
    }
}
