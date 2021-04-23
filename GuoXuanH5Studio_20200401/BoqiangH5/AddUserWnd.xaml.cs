using DBService;
using BoqiangH5Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BoqiangH5
{
    /// <summary>
    /// AddUserWnd.xaml 的交互逻辑
    /// </summary>
    public partial class AddUserWnd : Window
    {
        public AddUserWnd()
        {
            InitializeComponent();
        }
        List<UserRecord> UsersList = new List<UserRecord>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (V3Entities eb12 = new V3Entities())
                {
                    List<string> list = new List<string>();
                    foreach(var it in eb12.userrole.OrderBy(p =>p.RoleID))
                    {
                        list.Add(it.RoleName);
                    }
                    cbDuty.ItemsSource = list;
                    cbDuty.SelectedIndex = 0;

                    var items = from role in eb12.userrole
                               join user in eb12.users on role.RoleID equals user.RoleID
                               orderby user.UserID
                               select new
                               {
                                   no = user.UserID,
                                   name = user.UserName,
                                   password = user.Password,
                                   duty = role.RoleName,
                               };
                    foreach(var it in items)
                    {
                        UserRecord record = new UserRecord();
                        record.Index = UsersList.Count + 1;
                        record.UserNo = it.no;
                        record.UserName = it.name;
                        record.Password = it.password;
                        record.Duty = it.duty;
                        UsersList.Add(record);
                    }
                    dgUserInfo.ItemsSource = UsersList;

                    //if(UsersList.Count == 0)
                    //{
                    //    tbUserNo.Text = "JSBQ-0001";
                    //}
                    //else
                    //{
                    //    string no = UsersList[UsersList.Count - 1].UserNo;
                    //    string[] _nos = no.Split('-');
                    //    int val = Int32.Parse(_nos[1]) + 1;
                    //    tbUserNo.Text = "JSBQ-" + val.ToString("X4");
                    //}
                    tbPassword.Text = "123456";
                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbUserNo.Text.Trim()))
            {
                System.Windows.MessageBox.Show("工号不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(tbUserName.Text.Trim()))
            {
                System.Windows.MessageBox.Show("姓名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(tbPassword.Text.Trim()))
            {
                System.Windows.MessageBox.Show("密码不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (tbPassword.Text.Trim().Length > 20 || tbPassword.Text.Trim().Length < 6)
            {
                System.Windows.MessageBox.Show("密码为6-20个字符之间的字符串！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (0 != UsersList.Where(p =>p.UserNo == tbUserNo.Text.Trim()).Count())
            {
                System.Windows.MessageBox.Show("该工号已经存在，请重新输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            using (V3Entities eb12 = new V3Entities())
            {
                users user = new users()
                {
                    UserID = tbUserNo.Text.Trim(),
                    UserName = tbUserName.Text.Trim(),
                    Password = BoqiangH5Repository.MD5.Md5(tbPassword.Text.Trim()),
                    RoleID = cbDuty.SelectedIndex + 1,
                    IsInit = false
                };
                eb12.users.Add(user);
                eb12.SaveChanges();
                System.Windows.MessageBox.Show("添加用户成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                UserRecord record = new UserRecord();
                record.Index = UsersList.Count + 1;
                record.UserNo = tbUserNo.Text.Trim();
                record.UserName = tbUserName.Text.Trim();
                record.Password = user.Password;
                record.Duty = cbDuty.Text;
                UsersList.Add(record);
                dgUserInfo.Items.Refresh();
                //string no = UsersList[UsersList.Count - 1].UserNo;
                //string[] _nos = no.Split('-');
                //int val = Int32.Parse(_nos[1]) + 1;
                //tbUserNo.Text = "JSBQ-" + val.ToString("X4");
                tbPassword.Text = "123456";
                tbUserName.Text = string.Empty;
            }
        }

        private void btnAlter_Click(object sender, RoutedEventArgs e)
        {
            UserRecord record = dgUserInfo.SelectedItem as UserRecord;
            if (record != null)
            {
                AlterUser wnd = new AlterUser(record);
                wnd.ShowDialog();

                dgUserInfo.Items.Refresh();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            UserRecord record = dgUserInfo.SelectedItem as UserRecord;
            if(record != null)
            {
                if(MessageBoxResult.Yes == System.Windows.MessageBox.Show("确定要删除选定的用户？","提示",MessageBoxButton.YesNo,MessageBoxImage.Information))
                {
                    using (V3Entities eb12 = new V3Entities())
                    {
                        var item = eb12.users.FirstOrDefault(p => p.UserID == record.UserNo);
                        eb12.users.Remove(item);
                        eb12.SaveChanges();
                    }
                    UsersList.Remove(record);
                    dgUserInfo.Items.Refresh();
                    System.Windows.MessageBox.Show("删除用户成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
