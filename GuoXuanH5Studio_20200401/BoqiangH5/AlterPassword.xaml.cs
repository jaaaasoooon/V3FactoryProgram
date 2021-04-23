using DBService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// AlterPassword.xaml 的交互逻辑
    /// </summary>
    public partial class AlterPassword : Window
    {
        public AlterPassword()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbUserNo.Text.Trim()))
            {
                MessageBox.Show("请输入工号！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(pwdBox.Password.Trim()))
            {
                MessageBox.Show("请输入旧密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(newPwdBox.Password.Trim()))
            {
                MessageBox.Show("请输入新密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (newPwdBox.Password.Length > 20 || newPwdBox.Password.Length < 6)
            {
                MessageBox.Show("密码为6-20个字符之间的字符串！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (pwdBox.Password.Trim() == newPwdBox.Password.Trim())
            {
                MessageBox.Show("输入的新密码和旧密码一致，请修改新密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using (V3Entities eb12 = new V3Entities())
            {
                var user = eb12.users.FirstOrDefault(p => p.UserID == tbUserNo.Text.Trim());
                if (user != null)
                {
                    string _pwd = string.Empty;
                    if(user.IsInit)
                    {
                        _pwd = pwdBox.Password.Trim();
                    }
                    else
                    {
                        _pwd = BoqiangH5Repository.MD5.Md5(pwdBox.Password.Trim());
                    }

                    if (user.Password == _pwd)
                    {
                        user.Password = BoqiangH5Repository.MD5.Md5(newPwdBox.Password.Trim());
                        eb12.SaveChanges();
                        MessageBox.Show("密码修改成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("输入旧密码不正确，请重试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("工号不存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
