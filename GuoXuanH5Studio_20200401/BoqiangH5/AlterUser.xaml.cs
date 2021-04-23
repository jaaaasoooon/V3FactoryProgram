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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BoqiangH5
{
    /// <summary>
    /// AlterUser.xaml 的交互逻辑
    /// </summary>
    public partial class AlterUser : Window
    {
        UserRecord record;
        public AlterUser(UserRecord user)
        {
            InitializeComponent();
            record = user;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnAlter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbUserName.Text.Trim()))
            {
                System.Windows.MessageBox.Show("姓名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(tbUserNo.Text.Trim()))
            {
                System.Windows.MessageBox.Show("工号不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            using (V3Entities eb12 = new V3Entities())
            {
                if(record.UserNo != tbUserNo.Text.Trim())
                {
                    if (0 != eb12.users.Where(p => p.UserID == tbUserNo.Text.Trim()).Count())
                    {
                        System.Windows.MessageBox.Show("工号已经存在，请更换其他工号！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    var item = eb12.users.FirstOrDefault(p => p.UserID == record.UserNo);
                    eb12.users.Remove(item);
                    eb12.SaveChanges();
                    users user = new users()
                    {
                        UserID = tbUserNo.Text.Trim(),
                        UserName = tbUserName.Text.Trim(),
                        Password = tbPassword.Text.Trim(),
                        RoleID = cbDuty.SelectedIndex + 1,
                        IsInit = isInit
                    };
                    eb12.users.Add(user);
                }
                else
                {
                    var item = eb12.users.FirstOrDefault(p => p.UserID == record.UserNo);
                    item.UserName = tbUserName.Text.Trim();
                    item.Password = tbPassword.Text.Trim();
                    item.RoleID = cbDuty.SelectedIndex + 1;
                    if (isInit)
                    {
                        item.IsInit = true;
                    }
                    else
                    {
                        item.IsInit = false;
                    }
                }

                eb12.SaveChanges();
                record.UserName = tbUserName.Text.Trim();
                record.UserNo = tbUserNo.Text.Trim();
                record.Password = tbPassword.Text.Trim();
                record.Duty = cbDuty.Text;
                System.Windows.MessageBox.Show("修改用户信息成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (V3Entities eb12 = new V3Entities())
            {
                List<string> list = new List<string>();
                foreach (var it in eb12.userrole.OrderBy(p => p.RoleID))
                {
                    list.Add(it.RoleName);
                }
                cbDuty.ItemsSource = list;
            }

            tbUserNo.Text = record.UserNo;
            tbUserName.Text = record.UserName;
            tbPassword.Text = record.Password;
            cbDuty.SelectedItem = record.Duty;
        }

        bool isInit = false;
        private void btnInitPassword_Click(object sender, RoutedEventArgs e)
        {
            tbPassword.Text = "123456";
            isInit = true;
        }
    }
}
