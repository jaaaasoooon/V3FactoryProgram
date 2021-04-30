using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using DBService;
using System.Linq;
using System.Text.RegularExpressions;

namespace BoqiangH5
{
    /// <summary>
    /// SettingMacWnd.xaml 的交互逻辑
    /// </summary>
    public partial class SettingMacWnd : Window
    {
        public SettingMacWnd()
        {
            InitializeComponent();
        }

        Dictionary<string, Dictionary<int, string>> dic = new Dictionary<string, Dictionary<int, string>>();
        List<MacInfo> macInfoList = new List<MacInfo>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (V3Entities V3 = new V3Entities())
                {
                    foreach (var item in V3.computermac.ToList())
                    {
                        var items = V3.mac_operation.Where(p => p.MACID == item.ID).ToList();
                        foreach(var it in items)
                        {
                            var _it = V3.operation.FirstOrDefault(p => p.OperationID == it.OperationID);
                            if(_it != null)
                            {
                                if (dic.ContainsKey(item.MAC))
                                {
                                    var _dic = dic[item.MAC];
                                    if (_dic.ContainsKey(_it.OperationID))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        _dic.Add(_it.OperationID, _it.Type);
                                    }
                                }
                                else
                                {
                                    Dictionary<int, string> _dic = new Dictionary<int, string>();
                                    _dic.Add(_it.OperationID, _it.Type);
                                    dic.Add(item.MAC, _dic);
                                }
                            }
                        }
                    }


                    foreach (var item in dic)
                    {
                        MacInfo info = new MacInfo();
                        info.Mac = item.Key;
                        foreach (var it in item.Value)
                        {
                            info.Operations += it.Value;
                            info.Operations += ";";
                        }
                        info.Operations = info.Operations.TrimEnd(';');
                        macInfoList.Add(info);
                    }

                    dgMacInfo.ItemsSource = macInfoList;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbMac.Text.Trim()))
                {
                    MessageBox.Show("请输入MAC地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                string mac = tbMac.Text.Trim();
                string regex = @"^([0-9a-fA-F]{2})(([/-][0-9a-fA-F]{2}){5})$";
                if (!Regex.IsMatch(mac, regex))
                {
                    MessageBox.Show("请输入正确的MAC地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                bool isCheck = false;
                foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
                {
                    if(chk.IsChecked == true)
                    {
                        isCheck = true;
                        break;
                    }
                }
                if(isCheck == false)
                {
                    MessageBox.Show("请给该MAC地址配置相应的操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                using (V3Entities V3 = new V3Entities())
                {
                    var it = V3.computermac.FirstOrDefault(p => p.MAC == mac);
                    if (it != null)
                    {
                        MessageBox.Show("该MAC地址已设置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    computermac computermacs = new computermac();
                    computermacs.MAC = mac;
                    V3.computermac.Add(computermacs);
                    V3.SaveChanges();
                    sbyte macID = (sbyte)(V3.computermac.Max(p => p.ID));
                    string operStr = string.Empty;
                    foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
                    {
                        if(chk.IsChecked == true)
                        {
                            mac_operation mac_oper = new mac_operation();
                            var oper = V3.operation.FirstOrDefault(p => p.Type == chk.Content.ToString());
                            if(oper != null)
                            {
                                mac_oper.MACID =  macID;
                                mac_oper.OperationID = oper.OperationID;
                                V3.mac_operation.Add(mac_oper);
                                operStr += oper.Type;
                                operStr += ";";
                            }
                            else
                            {
                                MessageBox.Show(string.Format("找不到 {0} 对应的操作信息！",chk.Content.ToString()), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }
                    }
                    V3.SaveChanges();
                    MessageBox.Show("MAC信息设置添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    MacInfo info = new MacInfo();
                    info.Mac = mac;
                    info.Operations = operStr.TrimEnd(';');
                    macInfoList.Add(info);
                    dgMacInfo.Items.Refresh();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        private void btnAlter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MacInfo info = dgMacInfo.SelectedItem as MacInfo;
                if(info == null)
                {
                    MessageBox.Show("请先在表格中选择要修改的项！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (tbMac.Text.Trim() != info.Mac)
                {
                    MessageBox.Show("修改MAC配置时只能修改相关的操作，不可更改MAC地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                bool isCheck = false;
                foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
                {
                    if (chk.IsChecked == true)
                    {
                        isCheck = true;
                        break;
                    }
                }
                if (isCheck == false)
                {
                    MessageBox.Show("请给该MAC地址配置相应的操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                using (V3Entities V3 = new V3Entities())
                {
                    var mac = V3.computermac.FirstOrDefault(p => p.MAC == tbMac.Text.Trim());
                    if(mac != null)
                    {
                        var mac_opers = V3.mac_operation.Where(p => p.MACID == mac.ID);
                        foreach(var item in mac_opers)
                        {
                            V3.mac_operation.Remove(item);
                        }
                    }

                    string operStr = string.Empty;
                    foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
                    {
                        if (chk.IsChecked == true)
                        {
                            mac_operation mac_oper = new mac_operation();
                            var oper = V3.operation.FirstOrDefault(p => p.Type == chk.Content.ToString());
                            if (oper != null)
                            {
                                mac_oper.MACID = mac.ID;
                                mac_oper.OperationID = oper.OperationID;
                                V3.mac_operation.Add(mac_oper);
                                operStr += oper.Type;
                                operStr += ";";
                            }
                            else
                            {
                                MessageBox.Show(string.Format("找不到 {0} 对应的操作信息！", chk.Content.ToString()), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }
                    }
                    V3.SaveChanges();
                    MessageBox.Show("MAC信息设置修改成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    info.Operations = operStr.TrimEnd(';');
                    dgMacInfo.Items.Refresh();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        private void dgMacInfo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
            {
                chk.IsChecked = false;
            }
            tbMac.Text = string.Empty;
            MacInfo info = dgMacInfo.SelectedItem as MacInfo;
            if(info != null)
            {
                tbMac.Text = info.Mac;
                string[] opers = info.Operations.Split(';');
                foreach(var item in opers)
                {
                    foreach(CheckBox chk in ((Grid)gbOperation.Content).Children)
                    {
                        if(chk.Content.ToString() == item)
                        {
                            chk.IsChecked = true;
                        }
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MacInfo info = dgMacInfo.SelectedItem as MacInfo;
                if (info != null)
                {
                    using (V3Entities V3 = new V3Entities())
                    {
                        var item = V3.computermac.FirstOrDefault(p => p.MAC == info.Mac);
                        if (item != null)
                        {
                            var mac_opers = V3.mac_operation.Where(p => p.MACID == item.ID);
                            foreach (var it in mac_opers)
                            {
                                V3.mac_operation.Remove(it);
                            }
                            V3.computermac.Remove(item);
                            V3.SaveChanges();
                            MessageBox.Show("MAC信息设置删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            macInfoList.Remove(info);
                            dgMacInfo.Items.Refresh();
                            tbMac.Text = string.Empty;
                            foreach (CheckBox chk in ((Grid)gbOperation.Content).Children)
                            {
                                chk.IsChecked = false;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
    }

    public class MacInfo
    {
        public string Mac { set; get; }
        public string Operations { set; get; }
    }
}
