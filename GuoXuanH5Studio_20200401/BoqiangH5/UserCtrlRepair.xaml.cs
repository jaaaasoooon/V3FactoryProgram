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
    /// UserCtrlRepair.xaml 的交互逻辑
    /// </summary>
    public partial class UserCtrlRepair : UserControl
    {
        public UserCtrlRepair()
        {
            InitializeComponent();
        }
        public event EventHandler<EventArgs<string>> RequireReadUIDEvent;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cbID.SelectedIndex = 0;
            cbIDQuery.SelectedIndex = 0;
            cbResult.SelectedIndex = 0;

            dgRepairInfo.ItemsSource = m_RepairList;
            StartTime.Value = DateTime.Now - new TimeSpan(1, 0, 0, 0);
            EndTime.Value = DateTime.Now;

            listBoxDesc.ItemsSource = reasonList;
            listBoxComm.ItemsSource = methodList;
        }

        public void SetUID(string uid)
        {
            tbID.Text = uid;
        }

        public void SetQueryUID(string uid)
        {
            tbIDQuery.Text = uid;
        }
        public void SetBothUID(string uid)
        {
            tbIDQuery.Text = uid;
            tbID.Text = uid;
        }
        public void HandleRepairWndUpdateEvent(object sender, EventArgs e)
        {
            if (MainWindow.m_statusBarInfo.IsOnline)
            {
                if (cbID.SelectedIndex == 1)
                {
                    if(cbIDQuery.SelectedIndex == 1)
                    {
                        RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("RepairBoth"));
                    }
                    else
                    {
                        RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Repair"));
                    }
                }
                else
                {
                    if(cbIDQuery.SelectedIndex == 1)
                    {
                        RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("RepairQuery"));
                    }
                }
            }
        }
        private void cbID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbID.Text = string.Empty;
            if (cbID.SelectedIndex == 1)
            {
                if (MainWindow.m_statusBarInfo.IsOnline)
                {
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("Repair"));
                }
                else
                {
                    MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbID.Text.Trim()))
                {
                    MessageBox.Show("ID不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (string.IsNullOrEmpty(tbDescription.Text.Trim()))
                {
                    MessageBox.Show("返修原因描述不能为空，请填写！","提示",MessageBoxButton.OK,MessageBoxImage.Information);
                    return;
                }
                if (string.IsNullOrEmpty(tbComments.Text.Trim()))
                {
                    MessageBox.Show("返修情况描述不能为空，请填写！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                using (V3Entities eb12 = new V3Entities())
                {
                    if (cbID.SelectedIndex == 0)
                    {
                        var item = eb12.uidrecord.FirstOrDefault(p => p.BMSID == tbID.Text);
                        if(item != null)
                        {
                            string uid = item.UID;
                            repair record = new repair()
                            {
                                UID = uid,
                                ModifiedTime = DateTime.Now,
                                Description = tbDescription.Text.Trim(),
                                Comment = tbComments.Text.Trim(),
                                RepairResultID = (sbyte)(cbResult.SelectedIndex + 1),
                                UserID = MainWindow.UserID
                            };
                            eb12.repair.Add(record);
                            eb12.SaveChanges();
                            AutoClosedMsgBox.Show("数据提交成功！", "提示", 1000, 64);
                            tbComments.Text = string.Empty;
                            tbDescription.Text = string.Empty;
                        }
                        else
                        {
                            MessageBox.Show("未找到该BMS条码的绑定信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Information); 
                        }
                    }
                    else
                    {
                        repair record = new repair()
                        {
                            UID = tbID.Text.Trim(),
                            ModifiedTime = DateTime.Now,
                            Description = tbDescription.Text.Trim(),
                            Comment = tbComments.Text.Trim(),
                            RepairResultID = (sbyte)(cbResult.SelectedIndex + 1),
                            UserID = MainWindow.UserID
                        };
                        eb12.repair.Add(record);
                        eb12.SaveChanges();
                        AutoClosedMsgBox.Show("数据提交成功！", "提示", 1000, 64);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }           
        }
        List<RepairRecord> m_RepairList = new List<RepairRecord>();
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    if(string.IsNullOrEmpty(tbIDQuery.Text.Trim()))
                    {
                        MessageBox.Show("ID不能为空！","提示",MessageBoxButton.OK,MessageBoxImage.Information);
                        return;
                    }
                    if(StartTime.Value > EndTime.Value)
                    {
                        MessageBox.Show("查询起始时间大于查询结束时间，请输入正确的时间！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    m_RepairList.Clear();
                    dgRepairInfo.Items.Refresh();
                    if(cbIDQuery.SelectedIndex == 0)
                    {

                        var item = eb12.uidrecord.FirstOrDefault(p => p.BMSID == tbIDQuery.Text.Trim());
                        if(item != null)
                        {
                            var its = from res in eb12.repairresult
                                      join rep in eb12.repair on res.RepairResultID equals rep.RepairResultID
                                      join user in eb12.users on rep.UserID equals user.UserID
                                      where rep.UID == item.UID && rep.ModifiedTime >= StartTime.Value && rep.ModifiedTime <= EndTime.Value
                                      select new
                                      {
                                          UID = rep.UID,
                                          ModifiedTime = rep.ModifiedTime,
                                          Result = res.Result,
                                          Description = rep.Description,
                                          Comments = rep.Comment,
                                          Name = user.UserName,
                                          No = user.UserID
                                      };

                            if (its.Count() == 0)
                            {
                                MessageBox.Show("未查询到该设备的返修信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                foreach (var it in its)
                                {
                                    RepairRecord record = new RepairRecord();
                                    record.Index = m_RepairList.Count + 1;
                                    record.UID = it.UID;
                                    record.BMSID = item.BMSID;
                                    record.ModifiedTime = it.ModifiedTime;
                                    record.Description = it.Description;
                                    record.Comments = it.Comments;
                                    record.Result = it.Result;
                                    record.UserName = string.Format("{0}({1})", it.Name, it.No);
                                    m_RepairList.Add(record);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("未查询到该BMS ID的绑定信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {

                        var item = eb12.uidrecord.FirstOrDefault(p => p.UID == tbIDQuery.Text.Trim());
                        if (item != null)
                        {
                            //var its = eb12.repair.Where(p => p.UID == item.UID && p.ModifiedTime >= StartTime.Value && p.ModifiedTime <= EndTime.Value);
                            var its = from res in eb12.repairresult
                                      join rep in eb12.repair on res.RepairResultID equals rep.RepairResultID
                                      join user in eb12.users on rep.UserID equals user.UserID
                                      where rep.UID == item.UID && rep.ModifiedTime >= StartTime.Value && rep.ModifiedTime <= EndTime.Value
                                      select new 
                                      {
                                          UID = rep.UID,
                                          ModifiedTime = rep.ModifiedTime,
                                          Result = res.Result,
                                          Description = rep.Description,
                                          Comments = rep.Comment,
                                          Name = user.UserName,
                                          No = user.UserID
                                      };

                            if (its.Count() == 0)
                            {
                                MessageBox.Show("未查询到该设备的返修信息！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                foreach (var it in its)
                                {
                                    RepairRecord record = new RepairRecord();
                                    record.Index = m_RepairList.Count + 1;
                                    record.UID = it.UID;
                                    record.BMSID = item.BMSID;
                                    record.ModifiedTime = it.ModifiedTime;
                                    record.Description = it.Description;
                                    record.Comments = it.Comments;
                                    record.Result = it.Result;
                                    record.UserName = string.Format("{0}({1})", it.Name, it.No);
                                    m_RepairList.Add(record);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                CSVFileHelper.SaveRepairRecordCSV(m_RepairList, desFilePath);
                AutoClosedMsgBox.Show("数据保存成功！", "提示", 1000,64);
            }
        }

        private void cbIDQuery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbIDQuery.Text = string.Empty;
            if (cbIDQuery.SelectedIndex == 1)
            {
                if (MainWindow.m_statusBarInfo.IsOnline)
                {
                    RequireReadUIDEvent?.Invoke(this, new EventArgs<string>("RepairQuery"));
                }
                else
                {
                    MessageBox.Show("系统未连接，请连接后再进行操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbDescription.Text.Trim()))
                {
                    MessageBox.Show("请输入要添加的返修原因！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                using (V3Entities eb12 = new V3Entities())
                {
                    if (null != eb12.repairreason.FirstOrDefault(p => p.Reason == tbDescription.Text.Trim()))
                    {
                        MessageBox.Show("返修原因已存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    repairreason reason = new repairreason()
                    {
                        Reason = tbDescription.Text.Trim()
                    };
                    eb12.repairreason.Add(reason);
                    eb12.SaveChanges();
                    MessageBox.Show("添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    reasonList.Add(tbDescription.Text.Trim());
                    listBoxDesc.Items.Refresh();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnCAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbComments.Text.Trim()))
                {
                    MessageBox.Show("请输入要添加的返修处理方法！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                using (V3Entities eb12 = new V3Entities())
                {
                    if(null != eb12.repairmethod.FirstOrDefault(p =>p.RepairMethods == tbComments.Text.Trim()))
                    {
                        MessageBox.Show("返修处理方法已存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    repairmethod method = new repairmethod()
                    {
                        RepairMethods = tbComments.Text.Trim()
                    };                                                                                                                                                                                                                                                     
                    eb12.repairmethod.Add(method);
                    eb12.SaveChanges();
                    MessageBox.Show("添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    methodList.Add(tbComments.Text.Trim());
                    listBoxComm.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        List<string> reasonList = new List<string>();
        private void tbDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            reasonList.Clear();
            string strVal = tbDescription.Text.Trim();
            using (V3Entities eb12 = new V3Entities())
            {
                var items = eb12.repairreason.Where(p => p.Reason.Contains(strVal));
                foreach(var it in items)
                {
                    reasonList.Add(it.Reason);
                }
                listBoxDesc.Items.Refresh();
            }
        }
        List<string> methodList = new List<string>();
        private void tbComments_TextChanged(object sender, TextChangedEventArgs e)
        {
            methodList.Clear();
            string strVal = tbComments.Text.Trim();
            using (V3Entities eb12 = new V3Entities())
            {
                var items = eb12.repairmethod.Where(p => p.RepairMethods.Contains(strVal));
                foreach (var it in items)
                {
                    methodList.Add(it.RepairMethods);
                }
                listBoxComm.Items.Refresh();
            }
        }

        private void listBoxComm_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string item = (string)listBoxComm.SelectedItem;
            if (!string.IsNullOrEmpty(item))
            {
                tbComments.Text = item;
            }
        }

        private void listBoxDesc_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string item = (string)listBoxDesc.SelectedItem;
            if (!string.IsNullOrEmpty(item))
            {
                tbDescription.Text = item;
            }
        }
    }
}
