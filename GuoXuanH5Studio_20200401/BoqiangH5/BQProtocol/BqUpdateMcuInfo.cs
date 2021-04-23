using BoqiangH5.BQProtocol;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace BoqiangH5
{
    public partial class UserCtrlMCU : UserControl
    {

        int nMcuByteIndex = 1;
        bool isShowMsg = true;
        public event EventHandler<EventArgs<bool>> CheckMCUParamOKEvent;
        public event EventHandler<EventArgs<Tuple<bool, string>>> CheckMCUMsgEvent;
  		StringBuilder sb = new StringBuilder();
        public void BqUpdateMcuInfo(List<byte> listRecv)
        {
            try
            {
                if (listRecv.Count < (listRecv[2] << 8 | listRecv[3]) || listRecv[0] != 0xCC || listRecv[1] != 0xA8)
                {
                    return;
                }

                BqProtocol.bReadBqBmsResp = true;

                nMcuByteIndex = 4;
                sb.Clear();

                BqUpdateSysInfo1(listRecv);

                BqUpdateSysInfo2(listRecv);

                BqUpdateChargeInfo(listRecv);

                //if(isCheckMcu)
                //{
                //    var hardware = ListSysInfo2.FirstOrDefault(p => p.Description == "硬件版本");
                //    var software = ListSysInfo2.FirstOrDefault(p => p.Description == "软件版本");

                //    if(hardware != null && software != null)
                //    {
                //        if(hardware.StrValue == XmlHelper.m_strHardwareVersion)
                //        {
                //            //AutoClosedMsgBox.Show("硬件版本号检验成功！", "检验硬件版本号提示", 500, 64);
                //            CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(true, "硬件版本号检验成功！")));
                //        }
                //        else
                //        {
                //            //MessageBox.Show(string.Format("硬件版本号检验失败！当前硬件版本号 {0} 和设置硬件版本号 {1} 不一致！", hardware.StrValue, XmlHelper.m_strHardwareVersion), "检验硬件版本号提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //            CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(false, string.Format("硬件版本号检验失败！当前硬件版本号 {0} 和设置硬件版本号 {1} 不一致！", hardware.StrValue, XmlHelper.m_strHardwareVersion))));
                //            return;
                //        }

                //        if (software.StrValue == XmlHelper.m_strSoftwareVersion)
                //        {
                //            //AutoClosedMsgBox.Show("软件版本号检验成功！", "检验软件版本号提示", 500, 64);
                //            CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(true, "软件版本号检验成功！")));
                //        }
                //        else
                //        {
                //            //MessageBox.Show(string.Format("软件版本号检验失败！当前软件版本号 {0} 和设置软件版本号 {1} 不一致！",software.StrValue,XmlHelper.m_strSoftwareVersion), "检验软件版本号提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //            CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(false, string.Format("软件版本号检验失败！当前软件版本号 {0} 和设置软件版本号 {1} 不一致！", software.StrValue, XmlHelper.m_strSoftwareVersion))));
                //            return;
                //        }
                //    }

                //    #region 不在MCU中检查生产日期
                //    //var producedDate1 = ListSysInfo2.FirstOrDefault(p => p.Description == "生产日期");
                //    //DateTime dt1, dt2;
                //    //if(producedDate1 != null)
                //    //{
                //    //    if(producedDate1.StrValue == "2019-10-21")
                //    //    {
                //    //        //MessageBox.Show("生产日期检查为默认初始值2019-10-21，请检查！", "生产日期检查提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //    //        CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(false, "生产日期检查为默认初始值2019-10-21，请检查！")));
                //    //        isCheckMcu = false;
                //    //        return;
                //    //    }
                //    //    if (!DateTime.TryParse(producedDate1.StrValue, out dt1))
                //    //    {
                //    //        CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //        isCheckMcu = false;
                //    //        return;
                //    //    }
                //    //}
                //    //else
                //    //{
                //    //    CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //    isCheckMcu = false;
                //    //    return;
                //    //}
                //    //byte[] MCUData = new byte[141];
                //    //Buffer.BlockCopy(listRecv.ToArray(), 1, MCUData, 0, MCUData.Length);

                //    //Thread.Sleep(100);
                //    //LoadPara(FilePath);

                //    //var producedDate2 = ListSysInfo2.FirstOrDefault(p => p.Description == "生产日期");
                //    //if (producedDate2 != null)
                //    //{
                //    //    if (producedDate2.StrValue == "2019-10-21")
                //    //    {
                //    //        //MessageBox.Show("读取MCU文件生产日期为默认值2019-10-21，请检查！", "生产日期检查提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //    //        CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(false, "读取MCU文件生产日期为默认值2019-10-21，请检查！")));
                //    //        isCheckMcu = false;
                //    //        return;
                //    //    }
                //    //    if (DateTime.TryParse(producedDate2.StrValue, out dt2))
                //    //    {
                //    //        TimeSpan ts = dt2.Subtract(dt1);
                //    //        if (Math.Abs(ts.Days) <= Math.Abs(OneClickFactorySetting.m_ProducedDateError))
                //    //        {
                //    //            producedDate2.StrValue = string.Format("{0}-{1}-{2}", dt1.Year, dt1.Month, dt1.Day);
                //    //            byte[] mcuData = new byte[141];
                //    //            int len = 0;
                //    //            if (GetMcuDataBuf(mcuData, ref len))
                //    //            {
                //    //                if (Enumerable.SequenceEqual(MCUData, mcuData))
                //    //                {
                //    //                    CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(true));
                //    //                }
                //    //                else
                //    //                {
                //    //                    CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //                }
                //    //            }
                //    //            else
                //    //            {
                //    //                //MessageBox.Show("获取MCU参数失败，请检查！", "获取MCU参数提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //    //                CheckMCUMsgEvent?.Invoke(this, new EventArgs<Tuple<bool, string>>(new Tuple<bool, string>(false, "获取MCU参数失败，请检查！")));
                //    //            }
                //    //            isCheckMcu = false;
                //    //            return;
                //    //        }
                //    //        else
                //    //        {
                //    //            CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //            isCheckMcu = false;
                //    //            return;
                //    //        }
                //    //    }
                //    //    else
                //    //    {
                //    //        CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //        isCheckMcu = false;
                //    //        return;
                //    //    }
                //    //}
                //    //else
                //    //{
                //    //    CheckMCUParamOKEvent?.Invoke(this, new EventArgs<bool>(false));
                //    //    isCheckMcu = false;
                //    //    return;
                //    //}
                //    #endregion
                //}

                //if (isUpdateMCUFile)
                //{
                //    isUpdateMCUFile = false;
                //    isShowMsg = false;
                //    Thread.Sleep(200);
                //    btnWriteMcuData_Click(null, null);
                //    Thread.Sleep(200);
                //    btnReadMcuData_Click(null, null);
                //    Thread.Sleep(200);
                //    string path = AppDomain.CurrentDomain.BaseDirectory + @"ProtocolFiles\" + string.Format("MCU参数_{0:yyyyMMdd_HHmm}.txt", DateTime.Now);
                //    SavePara(path);
                //    XmlHelper.m_strMCUFilePath = path;
                //    XmlHelper.SaveConfigInfo(false);
                //    UpdateMcuConfigOKEvent?.Invoke(this, new EventArgs<string>(path));
                //    isShowMsg = false;
                //}

                if(isShowMsg)
                    AutoClosedMsgBox.Show("读取 MCU 参数成功！", "读取MCU提示",1000,64);
                isShowMsg = true;
                  
            }
            catch (Exception ex)
            {                
            }
        }
        public event EventHandler<EventArgs<string>> UpdateMcuConfigOKEvent;
        private void BqUpdateSysInfo1(List<byte> listRecv)
        {
            for (int n = 0; n < ListSysInfo1.Count; n++)
            {
                string strVal = null;
                switch (ListSysInfo1[n].ByteCount)
                {
                    //case 1:
                    //    if(ListSysInfo1[n].Description == "自放电率")
                    //    {
                    //        strVal = (listRecv[nMcuByteIndex] & 0xFF).ToString();
                    //    }
                    //    else
                    //    {
                    //        strVal = listRecv[nMcuByteIndex].ToString();
                    //    }
                    //    break;

                    case 2:
                        strVal = BqUpdateMcuInfo_2Byte(ListSysInfo1[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;

                    case 4:
                        strVal = BqUpdateMcuInfo_4Byte(ListSysInfo1[n], listRecv, nMcuByteIndex);
                        break;

                    //case 16:
                    //    strVal = BqUpdateMcuInfo_16Byte(listRecv, nMcuByteIndex);
                    //    break;
                    default:
                        break;
                }

                ListSysInfo1[n].StrValue = strVal;

                nMcuByteIndex += ListSysInfo1[n].ByteCount;
            }
        }

        private void BqUpdateSysInfo2(List<byte> listRecv)
        {
            for (int n = 0; n < ListSysInfo2.Count; n++)
            {
                string strVal2 = null;
                switch (ListSysInfo2[n].ByteCount)
                {
                    //case 1:
                    //    strVal2 = listRecv[nMcuByteIndex].ToString();
                    //    break;

                    case 2:
                        strVal2 = BqUpdateMcuInfo_2Byte(ListSysInfo2[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;

                    case 4:
                        strVal2 = BqUpdateMcuInfo_4Byte(ListSysInfo2[n], listRecv, nMcuByteIndex);
                        break;

                    //case 16:
                    //    strVal2 = BqUpdateMcuInfo_16Byte(listRecv, nMcuByteIndex);
                    //    break;
                    default:
                        break;
                }

                ListSysInfo2[n].StrValue = strVal2;

                nMcuByteIndex += ListSysInfo2[n].ByteCount;
            }
        }

        private void BqUpdateChargeInfo(List<byte> listRecv)
        {
            for (int n = 0; n < ListChargeInfo.Count; n++)
            {
                string strVal3 = null;
                switch (ListChargeInfo[n].ByteCount)
                {
                    //case 1:
                    //    strVal3 = listRecv[nMcuByteIndex].ToString();
                    //    break;

                    case 2:
                        strVal3 = BqUpdateMcuInfo_2Byte(ListChargeInfo[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;

                    case 4:
                        strVal3 = BqUpdateMcuInfo_4Byte(ListChargeInfo[n], listRecv, nMcuByteIndex);
                        break;
                    default:
                        break;
                }

                ListChargeInfo[n].StrValue = strVal3;

                nMcuByteIndex += ListChargeInfo[n].ByteCount;
            }
        }

        private string BqUpdateMcuInfo_2Byte(H5BmsInfo nodeInfo,byte bt1, byte bt2)
        {
            byte[] byteVal = new byte[2] { bt2, bt1};
            if (nodeInfo.Description == "容量学习最低允许温度")
            {
                return ((((byteVal[0] << 8) | byteVal[1]) - 2731) / 10).ToString();
            }
            else
                return ((byteVal[0] << 8) | byteVal[1]).ToString();
        }

        private string BqUpdateMcuInfo_4Byte(H5BmsInfo nodeInfo, List<byte> listRecv, int nByteIndex)
        {
            //return ((listRecv[nByteIndex] << 24) | (listRecv[nByteIndex + 1] << 16) |
            //              (listRecv[nByteIndex + 2] << 8) | (listRecv[nByteIndex + 3])).ToString();
            return ((listRecv[nByteIndex + 3] << 24) | (listRecv[nByteIndex + 2] << 16) |
              (listRecv[nByteIndex + 1] << 8) | (listRecv[nByteIndex])).ToString();
        }
    }
}
