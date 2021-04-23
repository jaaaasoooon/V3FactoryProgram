using DBService;
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
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAlterPassword_Click(object sender, RoutedEventArgs e)
        {
            AlterPassword alter = new AlterPassword();
            alter.ShowDialog();
        }

        public event EventHandler<EventArgs<Tuple<string,int>>> LoginedEvent;
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(tbUserNo.Text.Trim()))
            {
                MessageBox.Show("请输入工号！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(pwdBox.Password.Trim()))
            {
                MessageBox.Show("请输入密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            using (V3Entities eb12 = new V3Entities())
            {
                var user = eb12.users.FirstOrDefault(p => p.UserID == tbUserNo.Text.Trim());
                if(user != null)
                {
                    if(user.Password == BoqiangH5Repository.MD5.Md5(pwdBox.Password.Trim()))
                    {
                        var role = eb12.userrole.FirstOrDefault(p => p.RoleID == user.RoleID);
                        Tuple<string, int> tuple = new Tuple<string, int>(user.UserID,role.RoleID);
                        LoginedEvent?.Invoke(this, new EventArgs<Tuple<string,int>>(tuple));
                    }
                    else
                    {
                        MessageBox.Show("输入密码不正确，请重试！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("工号不存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
