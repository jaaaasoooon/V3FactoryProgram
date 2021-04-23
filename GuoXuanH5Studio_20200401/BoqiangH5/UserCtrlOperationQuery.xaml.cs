using DBService;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoqiangH5
{
    /// <summary>
    /// UserCtrlOperationQuery.xaml 的交互逻辑
    /// </summary>
    public partial class UserCtrlOperationQuery : UserControl
    {
        public UserCtrlOperationQuery()
        {
            InitializeComponent();
        }

        List<OperationRecord> RecordList = new List<OperationRecord>();
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cbID.SelectedIndex = 0;
            cbOperationType.SelectedIndex = 0;
            StartTime.Value = DateTime.Now - new TimeSpan(1,0,0,0);
            EndTime.Value = DateTime.Now;
            dgBqBmsInfo.ItemsSource = RecordList;
            ucPager.ChangePage += CurrentPageChanged;
        }

        private void CurrentPageChanged(object sender, EventArgs<int> e)
        {
            if(RecordList.Count > 0)
            {
                int pageIndex = e.Args;
                int pageNum = ucPager.GetCurrentPageNum();
                if(RecordList.Count >= pageIndex * pageNum)
                {
                    ucPager.ShowPages(RecordList.Count);
                    var list = RecordList.GetRange(pageNum * (pageIndex - 1), pageNum);
                    dgBqBmsInfo.ItemsSource = list;
                    dgBqBmsInfo.Items.Refresh();
                }
                else
                {
                    int num = pageIndex * pageNum - RecordList.Count;
                    if (num < pageNum)
                    {
                        ucPager.ShowPages(RecordList.Count);
                        var list = RecordList.GetRange(pageNum * (pageIndex - 1),pageNum - num);
                        dgBqBmsInfo.ItemsSource = list;
                        dgBqBmsInfo.Items.Refresh();
                    }
                }
            }
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //CSVFileHelper.WriteLogs("log", "测试", "点击按钮！");
                if (string.IsNullOrEmpty(tbID.Text.Trim()))
                {
                    MessageBox.Show("查询ID不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                CSVFileHelper.WriteLogs("log", "测试", "开始！");
                RecordList.Clear();
                dgBqBmsInfo.Items.Refresh();
                using (V3Entities eb12 = new V3Entities())
                {
                    CSVFileHelper.WriteLogs("log", "测试", "查询开始！");
                    string _uid = string.Empty;
                    string _bid = string.Empty;
                    if(cbID.SelectedIndex == 0)
                    {
                        _bid = tbID.Text.Trim();
                        uidrecord record = eb12.uidrecord.FirstOrDefault(p => p.BMSID == _bid);
                        if(record != null)
                            _uid = record.UID;
                        else
                            MessageBox.Show("未能查询到该BMS条码绑定的UID！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _uid = tbID.Text.Trim();
                        uidrecord record = eb12.uidrecord.FirstOrDefault(p => p.UID == _uid);
                        if(record != null)
                            _bid = record.BMSID;
                    }
                    if (chbAllType.IsChecked == true)
                    {
                        Task.Factory.StartNew(new Action(() =>
                        {
                            using (V3Entities v3 = new V3Entities())
                            {
                                var items = from oper in v3.operationrecord
                                            join user in v3.users on oper.UserID equals user.UserID
                                            join op in v3.operation on oper.OperationID equals op.OperationID
                                            join res in v3.result on oper.ResultID equals res.ResultID
                                            where oper.UID == _uid
                                            select new
                                            {
                                                uid = _uid,
                                                bmsID = _bid,
                                                operate = op.Type,
                                                modifiedTime = oper.ModifiedTime,
                                                result = res.Result1,
                                                rtc = oper.RTC,
                                                loopNumber = oper.LoopNumber,
                                                mcuCheckTime = oper.MCUCheckTime,
                                                data = oper.Data,
                                                comments = oper.Comments,
                                                totalVoltage = oper.TotalVoltage,
                                                current = oper.Current,
                                                cellVoltage = oper.CellVoltage,
                                                ambient = oper.AmbientTemp,
                                                cellTemp = oper.CellTemp,
                                                humidity = oper.Humidity,
                                                name = user.UserName,
                                                no = user.UserID
                                            };
                                int index = 1;
                                foreach (var it in items.OrderBy(p => p.modifiedTime))
                                {
                                    OperationRecord record = new OperationRecord();
                                    record.Index = index;
                                    index++;
                                    record.UID = it.uid;
                                    record.BID = it.bmsID;
                                    record.OperationType = it.operate;
                                    record.Result = it.result;
                                    record.ModifiedTime = it.modifiedTime;
                                    record.TotalVoltage = it.totalVoltage;
                                    record.Current = it.current;
                                    record.CellVoltage = it.cellVoltage;
                                    record.Ambient = it.ambient;
                                    record.CellTemp = it.cellTemp;
                                    record.Humidity = it.humidity;
                                    record.LoopNumber = it.loopNumber;
                                    record.Data = it.data;
                                    record.MCUCheckTime = it.mcuCheckTime;
                                    record.RTC = it.rtc;
                                    record.Comments = it.comments;
                                    record.UserName = string.Format("{0}({1})", it.name, it.no);
                                    RecordList.Add(record);

                                    //ShowQueryData(record, index);
                                }

                                Dispatcher.Invoke(new Action(() =>
                                {
                                    ucPager.ShowPages(RecordList.Count);
                                    var list = RecordList.GetRange(0, ucPager.GetCurrentPageNum());
                                    dgBqBmsInfo.ItemsSource = list;
                                    dgBqBmsInfo.Items.Refresh();
                                }));

                                //Dispatcher.Invoke(new Action(() =>
                                //{
                                //    MessageBox.Show("数据查询完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                //}));
                            }
                        }));

                        ////CSVFileHelper.WriteLogs("log", "测试", "数据处理完成！");
                    }
                    else
                    {
                        int operationType = cbOperationType.SelectedIndex + 1;
                        var items = from oper in eb12.operationrecord
                                    join user in eb12.users on oper.UserID equals user.UserID
                                    join op in eb12.operation on oper.OperationID equals op.OperationID
                                    join res in eb12.result on oper.ResultID equals res.ResultID
                                    where oper.UID == _uid && oper.OperationID == operationType
                                    select new
                                    {
                                        uid = _uid,
                                        bmsID = _bid,
                                        operate = op.Type,
                                        modifiedTime = oper.ModifiedTime,
                                        result = res.Result1,
                                        rtc = oper.RTC,
                                        loopNumber = oper.LoopNumber,
                                        mcuCheckTime = oper.MCUCheckTime,
                                        data = oper.Data,
                                        comments = oper.Comments,
                                        totalVoltage = oper.TotalVoltage,
                                        current = oper.Current,
                                        cellVoltage = oper.CellVoltage,
                                        ambient = oper.AmbientTemp,
                                        cellTemp = oper.CellTemp,
                                        humidity = oper.Humidity,
                                        name = user.UserName,
                                        no = user.UserID
                                    };
                        int index = 1;
                        foreach (var it in items)
                        {
                            OperationRecord record = new OperationRecord();
                            record.Index = index;
                            index++;
                            record.UID = it.uid;
                            record.BID = it.bmsID;
                            record.OperationType = it.operate;
                            record.Result = it.result;
                            record.ModifiedTime = it.modifiedTime;
                            record.TotalVoltage = it.totalVoltage;
                            record.Current = it.current;
                            record.CellVoltage = it.cellVoltage;
                            record.Ambient = it.ambient;
                            record.CellTemp = it.cellTemp;
                            record.Humidity = it.humidity;
                            record.LoopNumber = it.loopNumber;
                            record.Data = it.data;
                            record.MCUCheckTime = it.mcuCheckTime;
                            record.RTC = it.rtc;
                            record.Comments = it.comments;
                            record.UserName = string.Format("{0}({1})", it.name, it.no);
                            RecordList.Add(record);
                            //dgBqBmsInfo.Items.Refresh();
                            //ShowQueryData(record,index);
                        }

                        Dispatcher.Invoke(new Action(() =>
                        {
                            ucPager.ShowPages(RecordList.Count);
                            List<OperationRecord> list;
                            if(RecordList.Count < ucPager.GetCurrentPageNum())
                                list = RecordList.GetRange(0, RecordList.Count);
                            else
                                list = RecordList.GetRange(0, ucPager.GetCurrentPageNum());
                            dgBqBmsInfo.ItemsSource = list;
                            dgBqBmsInfo.Items.Refresh();
                        }));
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowQueryData(OperationRecord record,int index)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            {
                RecordList.Add(record);
                dgBqBmsInfo.Items.Refresh();
            }));
        }
        private void btnExport_Click(object sender, RoutedEventArgs e)
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
                CSVFileHelper.SaveOperationCSV(RecordList, desFilePath);
                AutoClosedMsgBox.Show("数据保存成功！", "提示", 1000, 64);
            }
        }
        public event EventHandler<EventArgs<string>> RequireReadUIDEvent;
        private void cbID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbID.Text = string.Empty;
            if(cbID.SelectedIndex == 1)
            {
                if(MainWindow.m_statusBarInfo.IsOnline)
                {
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Query"));
                }
                else
                {
                    MessageBox.Show("系统未连接，请连接后再进行操作！","提示",MessageBoxButton.OK,MessageBoxImage.Information);
                }
            }
        }

        public void SetUID(string uid)
        {
            tbID.Text = uid;
        }

        public void HandleQueryWndUpdateEvent(object sender, EventArgs e)
        {
            if(MainWindow.m_statusBarInfo.IsOnline)
            {
                if (cbID.SelectedIndex == 1)
                {
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Query"));
                }
            }
        }

        private void dgBqBmsInfo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = dgBqBmsInfo.SelectedItem as OperationRecord;
            if(item != null)
            {
                string[] voltages = item.CellVoltage.Split('$');
                string[] temps = item.CellTemp.Split('$');
                StringBuilder sbv = new StringBuilder();
                foreach(var it in voltages)
                {
                    sbv.Append(it);
                    sbv.Append(",");
                }
                StringBuilder sbt = new StringBuilder();
                foreach (var it in temps)
                {
                    sbt.Append(it);
                    sbt.Append(",");
                }
                tbMsg.Text = string.Format("UID：{0}\r\nBMSID：{1}\r\n电芯电压：{2}\r\n电芯温度：{3}\r\n数据：{4}\r\n备注：{5}\r\n",
                    item.UID, item.BID,sbv.ToString().TrimEnd(','), sbt.ToString().TrimEnd(','),item.Data,item.Comments);
            }

        }
    }

}
