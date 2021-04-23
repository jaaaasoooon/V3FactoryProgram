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
    /// VerifyWnd.xaml 的交互逻辑
    /// </summary>
    public partial class VerifyWnd : Window
    {
        public VerifyWnd()
        {
            InitializeComponent();
            password = string.Empty;
            isOK = false;
        }

        public string password = string.Empty;
        public bool isOK = false;
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(tbPassword.Text.Trim()))
            {
                MessageBox.Show("请输入密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            password = tbPassword.Text.Trim();
            if ("BQeb12" != password)
            {
                MessageBox.Show("密码错误，请重新输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            isOK = true;
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            isOK = false;
            this.Close();
        }
    }
}
