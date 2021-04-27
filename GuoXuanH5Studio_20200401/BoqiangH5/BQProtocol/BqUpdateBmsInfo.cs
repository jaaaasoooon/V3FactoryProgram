using BoqiangH5.BQProtocol;
using BoqiangH5Entity;
using BoqiangH5Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace BoqiangH5
{
    public partial class UserCtrlBqBmsInfo : UserControl
    {
        static SolidColorBrush brushGreen = new SolidColorBrush(Color.FromArgb(255, 150, 255, 150));
        static SolidColorBrush brushRed = new SolidColorBrush(Color.FromArgb(255, 255, 150, 150));
        static SolidColorBrush brushGray = new SolidColorBrush(Colors.LightGray);
        static SolidColorBrush brushYellow = new SolidColorBrush(Colors.Yellow);
        int nBqByteIndex = 1;

        static byte[] btHBalanceStatus = new byte[2];
        static byte[] btLBalanceStatus = new byte[2];
        static byte[] btBMSStatus = new byte[2];
        static byte[] btPackStatus = new byte[2];
        static byte[] btMOSStatus = new byte[2];
        static byte[] btVoltageStatus = new byte[2];
        static byte[] btCurrentStatus = new byte[2];
        static byte[] btTemperatureStatus = new byte[2];
        static byte[] btHumidityStatus = new byte[2];
        static byte[] btConfigStatus = new byte[2];
        static byte[] btCommunicationStatus = new byte[2];
        static byte[] btModeStatus = new byte[2];
        static byte[] btLogicStatus = new byte[2];

        public void BqProtocolUpdateBmsInfo(List<byte> listRecv)
        {
            if (listRecv.Count < 0x59 || listRecv[0] != 0xA1)
            {
                return;
            }

            for (int n = 0; n < m_ListCellVoltage.Count; n++)
            {
                m_ListCellVoltage[n].StrValue = ((listRecv[nBqByteIndex] << 8) | listRecv[nBqByteIndex + 1]).ToString();
                nBqByteIndex += 2;
            }

            m_ListBmsInfo[4].StrValue = ((listRecv[nBqByteIndex] << 24) | (listRecv[nBqByteIndex + 1] << 16) |
                                       (listRecv[nBqByteIndex + 2] << 8) | listRecv[nBqByteIndex + 3]).ToString();
            nBqByteIndex += 4;

            m_ListBmsInfo[5].StrValue = ((listRecv[nBqByteIndex] << 24) | (listRecv[nBqByteIndex + 1] << 16) |
                                       (listRecv[nBqByteIndex + 2] << 8) | listRecv[nBqByteIndex + 3]).ToString();
            nBqByteIndex += 4;

        }

        int cellMaxVoltage = 0;
        int cellMinVoltage = 0;
        int maxVoltageCellNum = 0;
        int minVoltageCellNum = 0;
        public void BqUpdateBmsInfo(List<byte> rdBuf)
        {
            if (rdBuf[0] != 0xCC || rdBuf[1] != 0xA2)
            {
                return;
            }
            int len = rdBuf[2] << 8 | rdBuf[3];
            if (rdBuf.Count < len)
            {
                return;
            }
            {
                BqProtocol.bReadBqBmsResp = true;
                //isWarning = false;
                //isProtect = false;
                nBqByteIndex = 4;

                UpdateCellInfo(rdBuf);
                UpdateBmsInfo(rdBuf);

                UpdateStatusInfo(btPackStatus, m_ListPackStatus);
                UpdateStatusInfo(btMOSStatus, m_ListMosStatus);
                UpdateStatusInfo(btVoltageStatus, m_ListVoltageProtectStatus);
                UpdateStatusInfo(btCurrentStatus, m_ListCurrentProtectStatus);
                UpdateStatusInfo(btTemperatureStatus, m_ListTemperatureProtectStatus);
                UpdateStatusInfo(btHumidityStatus, m_ListHumidityProtectStatus);
                UpdateStatusInfo(btConfigStatus, m_ListConfigStatus);
                UpdateStatusInfo(btCommunicationStatus, m_ListCommunicationStatus);
                UpdateStatusInfo(btModeStatus, m_ListModeStatus);
                UpdateStatusInfo(btLogicStatus, m_ListLogicStatus);
                Array.Reverse(btHBalanceStatus);
                UpdateBalanceStatus(btHBalanceStatus, m_ListCellVoltage, true);
                Array.Reverse(btLBalanceStatus);
                UpdateBalanceStatus(btLBalanceStatus, m_ListCellVoltage, false);
                //RefreshStatusEvent?.Invoke(this, new EventArgs<List<bool>>(new List<bool>() { isWarning, isProtect }));
            }

            count++;

            if(count == 5)
            {
                if (SelectCANWnd.m_IsAutoSetting)
                {
                    AutoStartOneClickFactory();
                }
                else
                {
                    if (SelectCANWnd.m_IsAutoCheck)
                    {
                        AutoStartOneClickCheck();
                    }
                }
            }
            else if (count == 1)
            {
                if (SelectCANWnd.m_IsAutoChargeOrDischarge && isChargeOrDischargeTest == false)
                {
                    //CSVFileHelper.WriteLogs("log", "充放电", "开启自动充放电！");
                    AutoChargeOrDischarge();
                }
            }
        }

        bool isVoltageDiffHigh = false;
        public void UpdateCellInfo(List<byte> rdBuf)
        {
            isVoltageDiffHigh = false;
            cellMinVoltage = -1;
            cellMaxVoltage = -1;
            maxVoltageCellNum = -1;
            minVoltageCellNum = -1;
            byte[] array = rdBuf.ToArray();
            for (int n = 0; n < m_ListCellVoltage.Count; n++)
            {
                int nCellVol = 0;
                byte[] bytes = new byte[m_ListCellVoltage[n].ByteCount];
                Buffer.BlockCopy(array, nBqByteIndex, bytes, 0, bytes.Length);
                if (m_ListCellVoltage[n].Description == "电池包电压" || m_ListCellVoltage[n].Description == "实时电流")
                {
                    nCellVol = BitConverter.ToInt32(bytes, 0);
                }
                else
                {
                    nCellVol = BitConverter.ToInt16(bytes, 0);
                }

                m_ListCellVoltage[n].StrValue = nCellVol.ToString();
                if (m_ListCellVoltage[n].Description != "实时电流" && m_ListCellVoltage[n].Description != "电池包电压")
                {
                    if (nCellVol != 0)
                    {
                        if (nCellVol > cellMaxVoltage)
                        {
                            cellMaxVoltage = nCellVol;
                            maxVoltageCellNum = n - 1;
                        }
                        else
                        {
                            if (cellMinVoltage == -1 || nCellVol < cellMinVoltage)
                            {
                                cellMinVoltage = nCellVol;
                                minVoltageCellNum = n - 1;
                            }
                        }
                    }
                }
                nBqByteIndex += m_ListCellVoltage[n].ByteCount;
            }
        }
        bool isTempDiffHigh = false;
        bool isLoopNumberIsOK = false;
        int maxTemperature = 0;

        int minTemperature = 0;
        public void UpdateBmsInfo(List<byte> rdBuf)
        {
            isTempDiffHigh = false;
            double ambientTemp = 0;

            maxTemperature = 0; minTemperature = 0;
            for (int i = 0; i < m_ListBmsInfo.Count; i++)
            {
                if (!m_ListBmsInfo[i].Description.Contains("状态"))
                {
                    if (m_ListBmsInfo[i].Description == "均衡通道_高")
                    {
                        for (int k = 0; k < m_ListBmsInfo[i].ByteCount; k++)
                        {
                            btHBalanceStatus[k] = rdBuf[nBqByteIndex + k];
                        }
                        m_ListBmsInfo[i].StrValue = string.Format("{0} {1}", btHBalanceStatus[1].ToString("X2"), btHBalanceStatus[0].ToString("X2"));
                    }
                    else if (m_ListBmsInfo[i].Description == "均衡通道_低")
                    {
                        for (int k = 0; k < m_ListBmsInfo[i].ByteCount; k++)
                        {
                            btLBalanceStatus[k] = rdBuf[nBqByteIndex + k];
                        }
                        m_ListBmsInfo[i].StrValue = string.Format("{0} {1}", btLBalanceStatus[1].ToString("X2"), btLBalanceStatus[0].ToString("X2"));
                    }
                    else
                    {
                        int nBmsVal = 0;
                        for (int j = m_ListBmsInfo[i].ByteCount - 1; j >= 0; j--)
                        {
                            nBmsVal = (nBmsVal << 8 | rdBuf[nBqByteIndex + j]);
                        }
                        if (m_ListBmsInfo[i].Description.Contains("温度"))
                        {
                            m_ListBmsInfo[i].StrValue = ((nBmsVal - 2731) / 10.0).ToString("F1");
                            if (nBmsVal > maxTemperature)
                                maxTemperature = nBmsVal;
                            else
                            {
                                if (minTemperature == 0 || nBmsVal < minTemperature)
                                    minTemperature = nBmsVal;
                            }

                            if(m_ListBmsInfo[i].Description == "环境温度")
                            {
                                ambientTemp = nBmsVal;
                            }
                            else
                            {
                                if (Math.Abs(Math.Abs(nBmsVal) - Math.Abs(ambientTemp)) > Math.Abs(OneClickFactorySetting.m_TemperatureError))
                                {
                                    isTempDiffHigh = true;
                                }
                            }
                        }
                        else if (m_ListBmsInfo[i].Description.Contains("FCC"))
                        {
                            m_ListBmsInfo[i].StrValue = (nBmsVal * decimal.Parse(m_ListBmsInfo[i].Scale)).ToString();
                        }
                        else if (m_ListBmsInfo[i].Description == "放电循环次数")
                        {
                            if (nBmsVal == 0)
                            {
                                isLoopNumberIsOK = true;
                            }
                            else
                            {
                                isLoopNumberIsOK = false;
                            }
                        }
                        else if (m_ListBmsInfo[i].Description.Contains("-X") || m_ListBmsInfo[i].Description.Contains("-Y") || m_ListBmsInfo[i].Description.Contains("-Z"))
                        {
                            byte[] bytes = new byte[m_ListCellVoltage[i].ByteCount];
                            Buffer.BlockCopy(rdBuf.ToArray(), nBqByteIndex, bytes, 0, bytes.Length);
                            Int16 val = BitConverter.ToInt16(bytes, 0);
                            m_ListBmsInfo[i].StrValue = (val / 1000.0).ToString();
                        }
                        else
                            m_ListBmsInfo[i].StrValue = (nBmsVal * decimal.Parse(m_ListBmsInfo[i].Scale)).ToString();
                    }
                }
                else
                {
                    byte[] bytes = new byte[2];
                    for (int k = 0; k < m_ListBmsInfo[i].ByteCount; k++)
                    {
                        bytes[k] = rdBuf[nBqByteIndex + k];
                    }
                    if (m_ListBmsInfo[i].Description == "BMS状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btBMSStatus, 0, bytes.Length);
                        string strStatus = GetHexDataString(btBMSStatus);
                        if (strStatus == "00 00") m_ListBmsInfo[i].StrValue = "工作准备模式";
                        else if (strStatus == "00 01") m_ListBmsInfo[i].StrValue = "工作模式";
                        else if (strStatus == "00 10") m_ListBmsInfo[i].StrValue = "休眠准备模式";
                        else if (strStatus == "00 11") m_ListBmsInfo[i].StrValue = "休眠模式";
                        else if (strStatus == "00 12") m_ListBmsInfo[i].StrValue = "休眠退出模式";
                        else if (strStatus == "00 20") m_ListBmsInfo[i].StrValue = "过放准备模式";
                        else if (strStatus == "00 21") m_ListBmsInfo[i].StrValue = "过放模式";
                        else if (strStatus == "00 22") m_ListBmsInfo[i].StrValue = "过放退出模式";
                        else if (strStatus == "00 F0") m_ListBmsInfo[i].StrValue = "关机准备模式";
                        else if (strStatus == "00 F1") m_ListBmsInfo[i].StrValue = "关机模式";
                        else
                            m_ListBmsInfo[i].StrValue = strStatus;
                    }
                    else if (m_ListBmsInfo[i].Description == "电池包状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btPackStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btPackStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "MOS状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btMOSStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btMOSStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "电压保护状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btVoltageStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btVoltageStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "电流保护状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btCurrentStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btCurrentStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "温度保护状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btTemperatureStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btTemperatureStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "外挂通讯状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btCommunicationStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btCommunicationStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "湿度/进水状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btHumidityStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btHumidityStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "参数配置状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btConfigStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btConfigStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "模式状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btModeStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btModeStatus);
                    }
                    else if (m_ListBmsInfo[i].Description == "逻辑状态")
                    {
                        Buffer.BlockCopy(bytes, 0, btLogicStatus, 0, bytes.Length);
                        m_ListBmsInfo[i].StrValue = GetHexDataString(btLogicStatus);
                    }
                }

                nBqByteIndex += m_ListBmsInfo[i].ByteCount;
            }
        }

        private string GetHexDataString(byte[] bytes)
        {
            Array.Reverse(bytes);
            string Str = string.Empty;
            foreach (var it in bytes)
            {
                Str += it.ToString("X2");
                Str += " ";
            }
            return Str.Trim();
        }
        private void UpdateStatusInfo(byte[] byteArr, List<BitStatInfo> listBatInfo)
        {
            for (int k = 0; k < listBatInfo.Count; k++)
            {
                if (0 == ((1 << listBatInfo[k].BitIndex) & byteArr[listBatInfo[k].ByteIndex]))
                {
                    listBatInfo[k].IsSwitchOn = false;
                    if (listBatInfo[k].BitInfo == "电芯压差大")//2020 04 20增加压差大判断显示
                    {
                        if(isVoltageDiffHigh)
                        {
                            listBatInfo[k].BackColor = brushRed;
                        }
                        else
                        {
                            listBatInfo[k].BackColor = brushGray;
                        }
                    }
                    else if (listBatInfo[k].BitInfo == "电芯温差大")//2020 04 20增加温差大判断显示
                    {
                        if (isTempDiffHigh)
                        {
                            listBatInfo[k].BackColor = brushRed;
                        }
                        else
                        {
                            listBatInfo[k].BackColor = brushGray;
                        }
                    }
                    else
                        listBatInfo[k].BackColor = brushGray;
                }
                else
                {
                    listBatInfo[k].IsSwitchOn = true;
                    if(listBatInfo[k].IsWarning)
                    {
                        listBatInfo[k].BackColor = brushRed;
                    }
                    else
                    { 
                        if (listBatInfo[k].IsProtect)
                        {
                            listBatInfo[k].BackColor = brushYellow;
                        }
                        else
                        {
                            listBatInfo[k].BackColor = brushGreen;
                        }
                    }
                }

            }
        }

        private void UpdateBalanceStatus(byte[] byteArr, List<H5BmsInfo> listBmsInfo, bool ishigh)
        {
            int n = 2;
            if (ishigh)
            {
                n += 16;
                for (; n < listBmsInfo.Count; n++)
                {
                    int bitNoVal = 0;
                    if (n >= 18 && n < 26)
                    {
                        bitNoVal = (((int)Math.Pow(2, n - 18)) & byteArr[1]) == ((int)Math.Pow(2, n - 18)) ? 1 : 0;
                    }
                    else if (n >= 26)
                    {
                        bitNoVal = (((int)Math.Pow(2, n - 26)) & byteArr[0]) == ((int)Math.Pow(2, n - 26)) ? 1 : 0;
                    }
                    if (0 == bitNoVal)
                    {
                        listBmsInfo[n].BalanceStat = BoqiangH5Entity.BalanceStatusEnum.No;
                    }
                    else
                    {
                        listBmsInfo[n].BalanceStat = BoqiangH5Entity.BalanceStatusEnum.Yes;
                    }
                }
            }
            else
            {
                for (; n < listBmsInfo.Count - 16; n++)
                {
                    int bitNoVal = 0;
                    if (n < 10)
                    {
                        bitNoVal = (((int)Math.Pow(2, n - 2)) & byteArr[1]) == ((int)Math.Pow(2, n - 2)) ? 1 : 0;
                    }
                    else if (n >= 10 && n < 18)
                    {
                        bitNoVal = (((int)Math.Pow(2, n - 10)) & byteArr[0]) == ((int)Math.Pow(2, n - 10)) ? 1 : 0;
                    }
                    if (0 == bitNoVal)
                    {
                        listBmsInfo[n].BalanceStat = BoqiangH5Entity.BalanceStatusEnum.No;
                    }
                    else
                    {
                        listBmsInfo[n].BalanceStat = BoqiangH5Entity.BalanceStatusEnum.Yes;
                    }
                }
            }
        }


        #region
        int nMcuByteIndex = 1;

        public bool BqUpdateMcuInfo(List<byte> listRecv)
        {
            try
            {
                if (listRecv.Count < 0x85 || listRecv[0] != 0xA2)
                {
                    return false;
                }

                BqProtocol.bReadBqBmsResp = true;

                nMcuByteIndex = 1;

                BqUpdateSysInfo1(listRecv);

                BqUpdateSysInfo2(listRecv);

                BqUpdateChargeInfo(listRecv);

                //MessageBox.Show("读取 MCU 参数成功！", "读取MCU提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void BqUpdateSysInfo1(List<byte> listRecv)
        {
            for (int n = 0; n < ListSysInfo1.Count; n++)
            {
                string strVal = null;
                switch (ListSysInfo1[n].ByteCount)
                {
                    case 1:
                        strVal = listRecv[nMcuByteIndex].ToString();
                        break;

                    case 2:
                        strVal = BqUpdateMcuInfo_2Byte(ListSysInfo1[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;

                    case 4:
                        strVal = BqUpdateMcuInfo_4Byte(ListSysInfo1[n], listRecv, nMcuByteIndex);
                        break;

                    case 16:
                        strVal = BqUpdateMcuInfo_16Byte(listRecv, nMcuByteIndex);
                        break;
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
                    case 1:
                        strVal2 = listRecv[nMcuByteIndex].ToString();
                        break;

                    case 2:
                        strVal2 = BqUpdateMcuInfo_2Byte(ListSysInfo2[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;

                    case 4:
                        strVal2 = BqUpdateMcuInfo_4Byte(ListSysInfo2[n], listRecv, nMcuByteIndex);
                        break;

                    case 16:
                        strVal2 = BqUpdateMcuInfo_16Byte(listRecv, nMcuByteIndex);
                        break;
                    default:
                        break;
                }

                ListSysInfo2[n].StrValue = strVal2;

                nMcuByteIndex += ListSysInfo2[n].ByteCount;
            }

            nMcuByteIndex += 5;
        }

        private void BqUpdateChargeInfo(List<byte> listRecv)
        {
            for (int n = 0; n < ListChargeInfo.Count; n++)
            {
                string strVal3 = null;
                switch (ListChargeInfo[n].ByteCount)
                {
                    case 1:
                        strVal3 = listRecv[nMcuByteIndex].ToString();
                        break;

                    case 2:
                        strVal3 = BqUpdateMcuInfo_2Byte(ListChargeInfo[n], listRecv[nMcuByteIndex], listRecv[nMcuByteIndex + 1]);
                        break;
                    default:
                        break;
                }

                ListChargeInfo[n].StrValue = strVal3;

                nMcuByteIndex += ListChargeInfo[n].ByteCount;
            }
        }

        private string BqUpdateMcuInfo_2Byte(H5BmsInfo nodeInfo, byte bt1, byte bt2)
        {
            byte[] byteVal = new byte[2] { bt1, bt2 };

            string strVal = null;
            switch (nodeInfo.Description)
            {
                case "MCU配置参数":
                    strVal = byteVal[0].ToString("X2") + byteVal[1].ToString("X2");
                    //BqUpdateMcuCfg(byteVal[0], byteVal[1]);
                    break;
                case "软件版本":
                case "硬件版本":
                    strVal = byteVal[0].ToString("X2") + "." + byteVal[1].ToString("X2");
                    break;

                case "设备ID":
                    strVal = byteVal[0].ToString();
                    break;

                case "序列号":
                    strVal = byteVal[0].ToString("X2") + byteVal[1].ToString("X2");
                    break;

                case "生产厂商":
                case "电池条码":
                    strVal = System.Text.Encoding.ASCII.GetString(byteVal);
                    strVal = strVal.Trim("\0".ToCharArray());
                    break;

                default:
                    strVal = ((byteVal[0] << 8) | byteVal[1]).ToString();
                    break;
            }

            return strVal;
        }

        private string BqUpdateMcuInfo_4Byte(H5BmsInfo nodeInfo, List<byte> listRecv, int nByteIndex)
        {
            string strVal = null;

            if (nodeInfo.Description == "生产日期")
            {
                int nYear = int.Parse((listRecv[nByteIndex] << 8 | listRecv[nByteIndex + 1]).ToString());
                int nMonth = int.Parse(listRecv[nByteIndex + 2].ToString());
                int nDate = int.Parse(listRecv[nByteIndex + 3].ToString());
                //StringBuilder sb = new StringBuilder();
                //sb.Append((listRecv[nByteIndex] & 0xFF).ToString("X2"));
                //sb.Append((listRecv[nByteIndex + 1] & 0xFF).ToString("X2"));
                //string nYear = sb.ToString(); 
                //int nMonth = int.Parse((listRecv[nByteIndex + 2] & 0xFF).ToString("X2"));
                //int nDate = int.Parse((listRecv[nByteIndex + 3] & 0xFF).ToString("X2"));

                if (nYear != 0 && nMonth != 0 && nDate != 0)
                {
                    //DateTime dt = new DateTime(nYear, nMonth, nDate);
                    strVal = string.Format("{0}-{1}-{2}", nYear, nMonth.ToString(), nDate.ToString());
                }
                else
                {
                    strVal = "2020-3-1";
                }
            }
            else
            {
                strVal = ((listRecv[nByteIndex] << 24) | (listRecv[nByteIndex + 1] << 16) |
                          (listRecv[nByteIndex + 2] << 8) | (listRecv[nByteIndex + 3])).ToString();
            }

            return strVal;
        }

        private string BqUpdateMcuInfo_16Byte(List<byte> listRecv, int nByteIndex)
        {
            byte[] arr = new byte[16];

            listRecv.CopyTo(nByteIndex, arr, 0, 16);

            string strVal = null;
            strVal = System.Text.Encoding.ASCII.GetString(arr);
            strVal = strVal.Trim("\0".ToCharArray());
            return strVal;
        }

        //private void BqUpdateMcuCfg(byte byHigh0, byte byLow1)
        //{
        //    byte temp = (byte)(byLow1 & 0x01);
        //    if (temp == 0x01)
        //        cbChgEnd.IsChecked = true;

        //    temp = (byte)(byLow1 & 0x02);
        //    if (temp == 0x02)
        //        cbDsgEnd.IsChecked = true;

        //    temp = (byte)(byLow1 & 0x08);
        //    if (temp == 0x08)
        //        cbEnEeprom.IsChecked = true;

        //    temp = (byte)(byHigh0 & 0x01);
        //    if (temp == 0x01)
        //        cbIsCclb.IsChecked = true;

        //    temp = (byte)(byHigh0 & 0x02);
        //    if (temp == 0x02)
        //        cbIsPreCharge.IsChecked = true;
        //}
    }
    #endregion
}
