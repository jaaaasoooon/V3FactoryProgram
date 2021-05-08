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
using System.Linq;
using DBService;

namespace BoqiangH5
{
    /// <summary>
    /// RemoveBindingWnd.xaml 的交互逻辑
    /// </summary>
    public partial class RemoveBindingWnd : Window
    {
        string UID = string.Empty;
        string BMSID = string.Empty;
        public RemoveBindingWnd(string uid)
        {
            InitializeComponent();
            UID = uid;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbUID.Text = UID;
            if(!string.IsNullOrEmpty(UID))
            {
                using (V3Entities edm = new V3Entities())
                {
                    var item = edm.uidrecord.FirstOrDefault(p => p.UID == UID);
                    if(item != null)
                    {
                        tbBMSID.Text = item.BMSID;
                    }
                    else
                    {
                        MessageBox.Show(string.Format("未找到UID为 {0} 的绑定信息，请确认该BMS是否完成绑定操作！",UID), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnRemoveBanding_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(tbUID.Text) || string.IsNullOrEmpty(tbBMSID.Text))
            {
                MessageBox.Show("请获取完整信息后再进行解绑操作！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBoxResult.Yes == MessageBox.Show(string.Format("确定要解除UID：{0} 和 BMSID：{1}之间的绑定关系吗？",UID,BMSID),"提示", MessageBoxButton.YesNo, MessageBoxImage.Information))
            {
                using (V3Entities edm = new V3Entities())
                {
                    var item = edm.uidrecord.FirstOrDefault(p => p.UID == UID);
                    if(item != null)
                    {
                        item.BMSID = string.Empty;
                        edm.SaveChanges();
                        MessageBox.Show("解除绑定成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                this.Close();
            }
        }
    }
}
