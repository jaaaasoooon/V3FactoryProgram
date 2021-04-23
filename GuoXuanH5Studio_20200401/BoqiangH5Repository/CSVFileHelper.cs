using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BoqiangH5Entity;

namespace BoqiangH5Repository
{
    public class CSVFileHelper
    {

        /// <summary>
        /// 将数据写入到CSV文件中
        /// </summary>
        /// <param name="listData">提供保存数据的list</param>
        /// <param name="path">CSV的文件路径</param>
        public static void SaveCSV(List<string> listData, string path)
        {
            bool isCreate = false;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();  
                }

                if (!File.Exists(path))
                {
                    fs = File.Create(path);//创建该文件
                    isCreate = true;
                }
                else
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }
      
                sw = new StreamWriter(fs, System.Text.Encoding.Default);

                //写出列名称
                if (isCreate)
                {
                    string strColumnsName = "序号,时间,收发,ID,CAN数据类型,协议数据类型,服务类型，数据";
                    sw.WriteLine(strColumnsName);
                }

                //写出各行数据
                if (null != listData)
                {
                    for (int i = 0; i < listData.Count; i++)
                    {
                        sw.WriteLine(listData[i], false);
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }

        /// <summary>
        /// 将CSV文件的数据读取到DataTable中
        /// </summary>
        /// <param name="fileName">CSV文件路径</param>
        /// <returns>返回读取了CSV数据的DataTable</returns>
        public static DataTable OpenCSV(string filePath)
        {
            DataTable dt = new DataTable();
            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                Encoding encoding = System.Text.Encoding.Default; // System.Text.Encoding.GetEncoding(936); // System.Data.Common.GetType(filePath); //Encoding.ASCII;//
                
                fs = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                //StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                sr = new StreamReader(fs, encoding);
            
                string strLine = "";
          
                string[] aryLine = null;
                string[] tableHead = null;
    
                int columnCount = 10;
          
                bool IsFirst = true;
     
                while ((strLine = sr.ReadLine()) != null)
                {

                    if (IsFirst == true)
                    {
                        tableHead = strLine.Split(',');
                        IsFirst = false;
                        columnCount = tableHead.Length;
                        //创建列
                        for (int i = 0; i < columnCount; i++)
                        {
                            DataColumn dc = new DataColumn(tableHead[i]);
                            dt.Columns.Add(dc);
                        }
                    }
                    else
                    {
                        aryLine = strLine.Split(',');
                        DataRow dr = dt.NewRow();
                        for (int j = 0; j < columnCount; j++)
                        {
                            dr[j] = aryLine[j];
                        }
                        dt.Rows.Add(dr);
                    }
                }
                if (aryLine != null && aryLine.Length > 0)
                {
                    dt.DefaultView.Sort = tableHead[0] + " " + "asc";
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (null != sr)
                    sr.Close();
                if (null != fs)
                    fs.Close();
            }
            return dt;
        }


        /// <summary>
        /// 读取Excel文件到DataSet中
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static DataSet LoadExcel(string filePath)
        {
            string connStr = "";
            string fileType = System.IO.Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(fileType)) return null;

            if (fileType == ".xls")
                connStr = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + filePath + ";" + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";
            else
                connStr = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + filePath + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";
            string sql_F = "Select * FROM [{0}]";

            OleDbConnection conn = null;
            OleDbDataAdapter da = null;
            System.Data.DataTable dtSheetName = null;

            DataSet ds = new DataSet();
            try
            {
                conn = new OleDbConnection(connStr);
                conn.Open();
                    
                string SheetName = "";
                dtSheetName = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

                // 初始化适配器
                da = new OleDbDataAdapter();
                //for (int i = 0; i < dtSheetName.Rows.Count; i++)
                {
                    SheetName = (string)dtSheetName.Rows[0]["TABLE_NAME"];
                    
                    if (SheetName.Contains("$") && !SheetName.Replace("'", "").EndsWith("$"))
                    {
                        //continue;
                    }

                    da.SelectCommand = new OleDbCommand(String.Format(sql_F, SheetName), conn);
                    DataSet dsItem = new DataSet();
                   
                    da.Fill(dsItem);

                    ds.Tables.Add(dsItem.Tables[0].Copy());
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                // 关闭连接
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    da.Dispose();
                    conn.Dispose();
                }
            }
            return ds;
        }


        /// <summary>
        /// 将BMS数据或单体数据写入到CSV文件中               lipeng   2020.3.26,增加实时信息记录
        /// </summary>
        /// <param name="listData">提供保存数据的list</param>
        /// <param name="path">CSV的文件路径</param>
        public static void SaveBmsORCellCSV(List<H5BmsInfo> listBmsData, string path, List<H5BmsInfo> listCellData)
        {
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                if (File.Exists(path))
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);

                    sw = new StreamWriter(fs, System.Text.Encoding.Default);
                    //写出各行数据
                    if (null != listBmsData  && null != listCellData)
                    {
                       // if (isBms)
                        {
                            sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{47}",
                                DateTime.Now.ToString("yyyy年MM月dd日 hh时mm分ss秒"), listBmsData[0].StrValue, listBmsData[1].StrValue, listBmsData[2].StrValue, listBmsData[3].StrValue, listBmsData[4].StrValue,
                                listBmsData[5].StrValue, listBmsData[6].StrValue, listBmsData[7].StrValue, listBmsData[8].StrValue, listBmsData[9].StrValue, listBmsData[10].StrValue, listBmsData[11].StrValue,
                                listBmsData[12].StrValue, listBmsData[13].StrValue, listBmsData[14].StrValue, listBmsData[15].StrValue, listBmsData[16].StrValue, listBmsData[17].StrValue, listBmsData[18].StrValue,
                                listBmsData[19].StrValue, listBmsData[20].StrValue, listBmsData[21].StrValue, listBmsData[22].StrValue, listBmsData[23].StrValue, listBmsData[24].StrValue,
                                listBmsData[25].StrValue, listBmsData[26].StrValue, listBmsData[27].StrValue, listBmsData[28].StrValue, listCellData[0].StrValue, listCellData[1].StrValue, listCellData[2].StrValue,
                                listCellData[3].StrValue, listCellData[4].StrValue, listCellData[5].StrValue, listCellData[6].StrValue, listCellData[7].StrValue, listCellData[8].StrValue, listCellData[9].StrValue,
                                listCellData[10].StrValue, listCellData[11].StrValue, listCellData[12].StrValue, listCellData[13].StrValue, listCellData[14].StrValue, listCellData[15].StrValue, listCellData[16].StrValue, listCellData[17].StrValue));
                        }
                        //else
                        //{
                        //    sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                        //        listData[0].StrValue, listData[1].StrValue, listData[2].StrValue, listData[3].StrValue, listData[4].StrValue, listData[5].StrValue, listData[6].StrValue, listData[7].StrValue,
                        //        listData[8].StrValue, listData[9].StrValue, listData[10].StrValue, listData[11].StrValue, listData[12].StrValue, listData[13].StrValue, listData[14].StrValue, listData[15].StrValue,
                        //        listData[16].StrValue, listData[17].StrValue));
                        //}
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }

        /// <summary>
        /// 写BMS数据或单体数据CSV文件标题             lipeng   2020.3.26,增加实时信息记录
        /// </summary>
        /// <param name="listData">提供保存数据的list</param>
        /// <param name="path">CSV的文件路径</param>
        public static void SaveBmsORCellCSVTitle(string path,bool isBqProtocol,List<H5BmsInfo> listBMS, List<H5BmsInfo> listCell, List<H5BmsInfo> listDevice)
        {
            StreamWriter sw = null;
            FileStream fs = null;
            try
            {
                if (File.Exists(path))
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                    sw = new StreamWriter(fs, System.Text.Encoding.Default);

                    if(isBqProtocol == true)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("测试时间,");
                        foreach (var item in listBMS)
                        {
                            sb.Append(string.Format("{0}({1})",item.Description,item.Unit));
                            sb.Append(",");
                        }
                        sb.Append("电池包电压(mV)");
                        sb.Append(",");
                        sb.Append("实时电流(mA)");
                        sb.Append(",");
                        foreach (var item in listCell)
                        {
                            if(item.Description == "电池包电压" || item.Description == "实时电流")
                            {
                                continue;
                            }
                            sb.Append(string.Format("{0}({1})", item.Description, item.Unit));
                            sb.Append(",");
                        }
                        /*sb.Append("环境温度,");
                        sb.Append("电芯温度2,");
                        sb.Append("电芯温度3,");
                        sb.Append("满充容量,");
                        sb.Append("剩余电量,");
                        sb.Append("SOC,");
                        sb.Append("循环放电次数,");
                        sb.Append("Pack状态,");
                        sb.Append("电池状态,");
                        sb.Append("Pack配置,");
                        sb.Append("制造信息,");
                        sb.Append("电芯温度4,");
                        sb.Append("电芯温度5,");
                        sb.Append("电芯温度6,");
                        sb.Append("电芯温度7,");
                        sb.Append("湿度,");
                        sb.Append("功率温度,");
                        sb.Append("AFE状态,");
                        sb.Append("最高电压,");
                        sb.Append("最高电压单体号,");
                        sb.Append("最低电压,");
                        sb.Append("最低电压单体号,");
                        sb.Append("单体最大温度,");
                        sb.Append("单体最小温度,");
                        sb.Append("均衡状态,");
                        sb.Append("RTC通讯状态,");
                        sb.Append("EEPROM通讯状态,");
                        sb.Append("最大压差,");
                        sb.Append("电芯01电压,");
                        sb.Append("电芯02电压,");
                        sb.Append("电芯03电压,");
                        sb.Append("电芯04电压,");
                        sb.Append("电芯05电压,");
                        sb.Append("电芯06电压,");
                        sb.Append("电芯07电压,");
                        sb.Append("电芯08电压,");
                        sb.Append("电芯09电压,");
                        sb.Append("电芯10电压,");
                        sb.Append("电芯11电压,");
                        sb.Append("电芯12电压,");
                        sb.Append("电芯13电压,");
                        sb.Append("电芯14电压,");
                        sb.Append("电芯15电压,");
                        sb.Append("电芯16电压,");
                        sb.Append("电池包电压,");
                        sb.Append("实时电流,");*/
                        sw.WriteLine(sb.ToString());
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("测试时间,");
                        //描述中有逗号，需做特殊处理
                        //sb.Append("电池组健康百分比SOH,");
                        //sb.Append("BMS是否持续输出电压,");
                        //sb.Append("BMS输出延时");
                        //sb.Append("电池组内部温度/电池表面温度,");
                        //sb.Append("电池组总电压,");
                        //sb.Append("电池实时电流,");
                        //sb.Append("电池相对容量百分比RSOC,");
                        //sb.Append("电池绝对容量百分比ASOC,");
                        //sb.Append("电池剩余容量RSCAP,");
                        //sb.Append("电池满电容量,");
                        //sb.Append("电池循环次数,");
                        //sb.Append("保留");
                        //sb.Append("电池高16组每节电池电压,");
                        //sb.Append("电池低16组每节电池电压");
                        //sb.Append("电池当前充电间隔时间,");
                        //sb.Append("电池最大充电间隔时间,");
                        //sb.Append("电池允许最大放电电流,");
                        //sb.Append("电池组允许的最大充电电压,");
                        //sb.Append("电池允许最大充电电流,");
                        //sb.Append("一级过放保护电压阈值,");
                        //sb.Append("保留,");
                        //sb.Append("电池状态信息,");
                        //sb.Append("电芯型号,");
                        //sb.Append("电池组内部实时时钟RTC,");
                        //sb.Append("电池设计容量 (毫安时),");
                        //sb.Append("电池累计放电安时数 (毫安时),");
                        //sb.Append("电池组内部温度，第二组电芯温度探测点温度,");
                        //sb.Append("电池组内部温度，第三组电芯温度探测点温度,");
                        //sb.Append("电池组内部温度，第四组电芯温度探测点温度,");
                        //sb.Append("电池组内部温度，MOS管温度,");
                        //sb.Append("电池内部湿度百分比,");
                        //sb.Append("SOP，高16位电压值，低16位电流值,");
                        //sb.Append("FCC,");
                        //sb.Append("累计充电能量,");
                        //sb.Append("累计放电能量,");
                        //sb.Append("绝缘电阻,");
                        foreach (var item in listBMS)
                        {
                            sb.Append(string.Format("{0}({1})", item.Description, item.Unit));
                            sb.Append(",");
                        }
                        foreach (var item in listCell)
                        {
                            sb.Append(string.Format("{0}({1})", item.Description, item.Unit));
                            sb.Append(",");
                        }
                        //foreach (var item in listDevice)
                        //{
                        //    sb.Append(item.Description);
                        //    sb.Append(",");
                        //}
                        /*sb.Append("电池组健康百分比SOH,");
                        sb.Append("BMS是否持续输出电压,");
                        sb.Append("BMS输出延时");
                        sb.Append("电池组内部温度/电池表面温度,");
                        sb.Append("电池组总电压,");
                        sb.Append("电池实时电流,");
                        sb.Append("电池相对容量百分比RSOC,");
                        sb.Append("电池绝对容量百分比ASOC,");
                        sb.Append("电池剩余容量RSCAP,");
                        sb.Append("电池满电容量,");
                        sb.Append("电池循环次数,");
                        sb.Append("保留");
                        sb.Append("电池高16组每节电池电压,");
                        sb.Append("电池低16组每节电池电压");
                        sb.Append("电池当前充电间隔时间,");
                        sb.Append("电池最大充电间隔时间,");
                        sb.Append("电池允许最大放电电流,");
                        sb.Append("电池组允许的最大充电电压,");
                        sb.Append("电池允许最大充电电流,");
                        sb.Append("一级过放保护电压阈值,");
                        sb.Append("保留,");
                        sb.Append("电池状态信息,");
                        sb.Append("电芯型号,");
                        sb.Append("电池组内部实时时钟RTC,");
                        sb.Append("电池设计容量 (毫安时),");
                        sb.Append("电池累计放电安时数 (毫安时),");
                        sb.Append("电池组内部温度，第二组电芯温度探测点温度,");
                        sb.Append("电池组内部温度，第三组电芯温度探测点温度,");
                        sb.Append("电池组内部温度,第四组电芯温度探测点温度,");
                        sb.Append("电池组内部温度，MOS管温度,");
                        sb.Append("电池内部湿度百分比,");
                        sb.Append("SOP，高16位电压值，低16位电流值,");
                        sb.Append("FCC,");
                        sb.Append("累计充电能量,");
                        sb.Append("累计放电能量,");
                        sb.Append("绝缘电阻,");
                        sb.Append("高16组 电芯1电压,");
                        sb.Append("高16组 电芯2电压,");
                        sb.Append("高16组 电芯3电压,");
                        sb.Append("高16组 电芯4电压,");
                        sb.Append("高16组 电芯5电压,");
                        sb.Append("高16组 电芯6电压,");
                        sb.Append("高16组 电芯7电压,");
                        sb.Append("高16组 电芯8电压,");
                        sb.Append("高16组 电芯9电压,");
                        sb.Append("高16组 电芯10电压,");
                        sb.Append("高16组 电芯11电压,");
                        sb.Append("高16组 电芯12电压,");
                        sb.Append("高16组 电芯13电压,");
                        sb.Append("高16组 电芯14电压,");
                        sb.Append("高16组 电芯15电压,");
                        sb.Append("高16组 电芯16电压,");
                        sb.Append("设备类型,");
                        sb.Append("固件版本号,");
                        sb.Append("硬件版本号,");
                        sb.Append("制造厂信息,");
                        sb.Append("设备SN号,");
                        sb.Append("硬件型号编号.客户型号编号,");
                        sb.Append("固件版本号,");*/
                        sw.WriteLine(sb.ToString());
                    }
                }

            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }

        public static void SaveRecordDataCSV(System.Collections.ObjectModel.ObservableCollection<H5RecordInfo> listData, string path)
        {
            bool isCreate = false;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (!File.Exists(path))
                {
                    fs = File.Create(path);//创建该文件
                    isCreate = true;
                }
                else
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }

                sw = new StreamWriter(fs, System.Text.Encoding.Default);

                //写出列名称
                if (isCreate)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("序号,");
                    sb.Append("时间,");
                    sb.Append("记录类型,");
                    sb.Append("Pack状态,");
                    sb.Append("电池状态,");
                    sb.Append("满充容量(mAh),");
                    sb.Append("剩余容量(mAh),");
                    sb.Append("SOC(%),");
                    sb.Append("Cell1(mV),");
                    sb.Append("Cell2(mV),");
                    sb.Append("Cell3(mV),");
                    sb.Append("Cell4(mV),");
                    sb.Append("Cell5(mV),");
                    sb.Append("Cell6(mV),");
                    sb.Append("Cell7(mV),");
                    sb.Append("Cell8(mV),");
                    sb.Append("Cell9(mV),");
                    sb.Append("Cell10(mV),");
                    sb.Append("Cell11(mV),");
                    sb.Append("Cell12(mV),");
                    sb.Append("Cell13(mV),");
                    sb.Append("Cell14(mV),");
                    sb.Append("Cell15(mV),");
                    sb.Append("Cell16(mV),");
                    sb.Append("总压(V),");
                    sb.Append("电流(mA),");
                    sb.Append("环境温度(℃),");
                    sb.Append("温度保留1(℃),");
                    sb.Append("温度保留2(℃),");
                    sb.Append("电芯温度1(℃),");
                    sb.Append("电芯温度2(℃),");
                    sb.Append("电芯温度3(℃),");
                    sb.Append("电芯温度4(℃),");
                    sb.Append("湿度(%),");
                    sb.Append("功率温度(℃)");
                    sw.WriteLine(sb.ToString());
                }

                //写出各行数据
                if (null != listData)
                {
                    foreach(var item in listData)
                    {
                        string strLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34}",
                            item.Index, item.RecordTime, item.RecordType, item.PackStatus, item.BatteryStatus, item.FCC, item.RC, item.SOC, item.Cell1Voltage, item.Cell2Voltage,
                            item.Cell3Voltage, item.Cell4Voltage, item.Cell5Voltage, item.Cell6Voltage, item.Cell7Voltage, item.Cell8Voltage, item.Cell9Voltage, item.Cell10Voltage, 
                            item.Cell11Voltage, item.Cell12Voltage, item.Cell13Voltage, item.Cell14Voltage, item.Cell15Voltage, item.Cell16Voltage, item.TotalVoltage, item.Current, 
                            item.AmbientTemp, item.Cell1Temp, item.Cell2Temp,item.Cell3Temp, item.Cell4Temp,item.Cell5Temp,item.Cell6Temp,item.Cell7Temp,item.PowerTemp);
                        sw.WriteLine(strLine);
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }
        public static void SaveDdRecordDataCSV(System.Collections.ObjectModel.ObservableCollection<H5DidiRecordInfo> listData, string path, bool isReadAll, string uid, List<string> list)
        {
            bool isCreate = false;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (!File.Exists(path))
                {
                    fs = File.Create(path);//创建该文件
                    isCreate = true;
                }
                else
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }

                sw = new StreamWriter(fs, System.Text.Encoding.Default);

                sw.WriteLine(string.Format("UID：{0}", uid));
                sw.WriteLine(string.Format("项目名称：{0}", list[0]));
                sw.WriteLine(string.Format("硬件版本号：{0}", list[1]));
                sw.WriteLine(string.Format("BOOT版本号：{0}", list[2]));
                sw.WriteLine(string.Format("APP通用码：{0}", list[3]));
                sw.WriteLine(string.Format("生产厂商：{0}", list[4]));
                sw.WriteLine(string.Format("固件版本：{0}", list[5]));
                sw.WriteLine(string.Format("化学材料：{0}", list[6]));
                sw.WriteLine(string.Format("电芯型号：{0}", list[7]));
                sw.WriteLine(string.Format("程序所处阶段：{0}", list[8]));
                sw.WriteLine(string.Format("保护板生厂日期：{0}", list[9]));
                sw.WriteLine(string.Format("保护板序列号：{0}", list[10]));
                sw.WriteLine(string.Format("电池包生厂日期：{0}", list[11]));
                sw.WriteLine(string.Format("电池包序列号：{0}", list[12]));
                sw.WriteLine(string.Empty);
                //写出列名称
                if (isCreate)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("序号,");
                    sb.Append("时间,");
                    if (isReadAll)
                    {
                        sb.Append("记录类型(BQ),");
                        sb.Append("事件类型(BQ),");
                        //sb.Append("Pack状态(BQ),");
                        //sb.Append("电池状态(BQ),");
                    }
                    sb.Append("总压(mV),");
                    sb.Append("电流(mA),");
                    sb.Append("SOC(%),");
                    sb.Append("电池组状态,");
                    sb.Append("充电MOS状态,");
                    sb.Append("放电MOS状态,");
                    sb.Append("DET状态,");
                    sb.Append("放电使能状态,");
                    sb.Append("满充容量(mAh),");
                    sb.Append("循环次数,");
                    sb.Append("电池包状态,");
                    sb.Append("Mos状态,");
                    sb.Append("电压状态,");
                    sb.Append("电流状态,");
                    sb.Append("温度状态,");
                    sb.Append("湿度状态,");
                    sb.Append("配置参数状态,");
                    sb.Append("通讯状态,");
                    sb.Append("均衡,");
                    sb.Append("电芯温度1(℃),");
                    sb.Append("电芯温度2(℃),");
                    sb.Append("MOS温度(℃),");
                    sb.Append("电芯温度3(℃),");
                    sb.Append("电芯温度4(℃),");
                    sb.Append("Cell1(mV),");
                    sb.Append("Cell2(mV),");
                    sb.Append("Cell3(mV),");
                    sb.Append("Cell4(mV),");
                    sb.Append("Cell5(mV),");
                    sb.Append("Cell6(mV),");
                    sb.Append("Cell7(mV),");
                    sb.Append("Cell8(mV),");
                    sb.Append("Cell9(mV),");
                    sb.Append("Cell10(mV),");
                    sb.Append("Cell11(mV),");
                    sb.Append("Cell12(mV),");
                    sb.Append("Cell13(mV),");
                    sb.Append("Cell14(mV),");
                    sb.Append("Cell15(mV),");
                    sb.Append("Cell16(mV),");
                    sb.Append("湿度(%)");
                    sw.WriteLine(sb.ToString());
                }

                //写出各行数据
                if (null != listData)
                {
                    foreach (var item in listData)
                    {
                        string strLine = string.Empty;
                        if (isReadAll)
                        {
                            strLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44}",
                                        item.Index, item.RecordTime, item.RecordType, item.EventType, item.TotalVoltage, item.Current, item.SOC, item.BatteryStatus, item.ChargeMOSStatus, item.DischargeMOSStatus, item.DetStatus, item.DischargeEnableStatus,
                                        item.FCC, item.LoopNumber, item.PackStatus, item.MosStatus, item.VoltageStatus, item.CurrentStatus, item.TemperatureStatus, item.Humidity, item.ConfigStatus, item.CommunicationStatus,
                                        item.Balance, item.Cell1Temp, item.Cell2Temp, item.Cell3Temp, item.Cell4Temp, item.Cell5Temp, item.Cell1Voltage, item.Cell2Voltage, item.Cell3Voltage, item.Cell4Voltage,
                                        item.Cell5Voltage, item.Cell6Voltage, item.Cell7Voltage, item.Cell8Voltage, item.Cell9Voltage,
                                        item.Cell10Voltage, item.Cell11Voltage, item.Cell12Voltage, item.Cell13Voltage, item.Cell14Voltage, item.Cell15Voltage, item.Cell16Voltage,
                                        item.Humidity);
                        }
                        else
                        {
                            strLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33}",
                                        item.Index, item.RecordTime, item.TotalVoltage, item.Current, item.SOC, item.BatteryStatus, item.ChargeMOSStatus, item.DischargeMOSStatus, item.DetStatus, item.DischargeEnableStatus, item.LoopNumber, item.Balance,
                                        item.Cell1Temp, item.Cell2Temp, item.Cell3Temp, item.Cell4Temp, item.Cell5Temp, item.Cell1Voltage, item.Cell2Voltage, item.Cell3Voltage, item.Cell4Voltage, item.Cell5Voltage,
                                        item.Cell6Voltage, item.Cell7Voltage, item.Cell8Voltage, item.Cell9Voltage,
                                        item.Cell10Voltage, item.Cell11Voltage, item.Cell12Voltage, item.Cell13Voltage, item.Cell14Voltage, item.Cell15Voltage, item.Cell16Voltage,
                                        item.Humidity);
                        }
                        sw.WriteLine(strLine);
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }
        //static string logPath = AppDomain.CurrentDomain.BaseDirectory + "log" + "\\" + ;


        public static void SaveOperationCSV(List<OperationRecord> listData, string path)
        {
            bool isCreate = false;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (!File.Exists(path))
                {
                    fs = File.Create(path);//创建该文件
                    isCreate = true;
                }
                else
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }

                sw = new StreamWriter(fs, System.Text.Encoding.Default);

                //写出列名称
                if (isCreate)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("序号,");
                    sb.Append("UID,");
                    sb.Append("BMSID,");
                    sb.Append("操作,");
                    sb.Append("操作时间,");
                    sb.Append("操作结果,");
                    sb.Append("总压(mV),");
                    sb.Append("电流(mA),");
                    sb.Append("Cell1(mV),");
                    sb.Append("Cell2(mV),");
                    sb.Append("Cell3(mV),");
                    sb.Append("Cell4(mV),");
                    sb.Append("Cell5(mV),");
                    sb.Append("Cell6(mV),");
                    sb.Append("Cell7(mV),");
                    sb.Append("Cell8(mV),");
                    sb.Append("Cell9(mV),");
                    sb.Append("Cell10(mV),");
                    sb.Append("Cell11(mV),");
                    sb.Append("Cell12(mV),");
                    sb.Append("Cell13(mV),");
                    sb.Append("Cell14(mV),");
                    sb.Append("Cell15(mV),");
                    sb.Append("Cell16(mV),");
                    sb.Append("环境温度(℃),");
                    sb.Append("电芯温度1(℃),");
                    sb.Append("电芯温度2(℃),");
                    sb.Append("电芯温度3(℃),");
                    sb.Append("电芯温度4(℃),");
                    sb.Append("湿度(%),");
                    sb.Append("循环放电次数,");
                    sb.Append("RTC,");
                    sb.Append("MCU检查时间,");
                    sb.Append("操作人员,");
                    sb.Append("数据,");
                    sb.Append("备注,");
                    sw.WriteLine(sb.ToString());
                }

                //写出各行数据
                if (null != listData)
                {
                    foreach (var item in listData)
                    {
                        string[] voltages = item.CellVoltage.Split('$');
                        string[] temps = item.CellTemp.Split('$');
                        string strLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35}",
                        item.Index, item.UID, item.BID, item.OperationType, item.ModifiedTime, item.Result, item.TotalVoltage, item.Current, voltages[0], voltages[1], voltages[2], voltages[3], voltages[4], voltages[5],
                        voltages[6], voltages[7], voltages[8], voltages[9], voltages[10], voltages[11], voltages[12], voltages[13], voltages[14], voltages[15], item.Ambient, temps[0], temps[1], temps[2], temps[3],
                        item.Humidity, item.LoopNumber, item.RTC, item.MCUCheckTime,item.UserName, item.Data, item.Comments);

                        sw.WriteLine(strLine);
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }

        public static void SaveRepairRecordCSV(List<RepairRecord> listData, string path)
        {
            bool isCreate = false;
            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (!File.Exists(path))
                {
                    fs = File.Create(path);//创建该文件
                    isCreate = true;
                }
                else
                {
                    fs = new FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }

                sw = new StreamWriter(fs, System.Text.Encoding.Default);

                //写出列名称
                if (isCreate)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("序号,");
                    sb.Append("UID,");
                    sb.Append("BMSID,");
                    sb.Append("返修时间,");
                    sb.Append("返修结果,");
                    sb.Append("返修问题描述,");
                    sb.Append("返修情况描述,");
                    sb.Append("操作人员,");
                    sw.WriteLine(sb.ToString());
                }

                //写出各行数据
                if (null != listData)
                {
                    foreach (var item in listData)
                    {
                        string strLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",item.Index, item.UID, item.BMSID,item.ModifiedTime,item.Result,item.Description,item.Comments,item.UserName);
                        sw.WriteLine(strLine);
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (null != sw)
                    sw.Close();
                if (null != fs)
                    fs.Close();
            }

        }

        public static void WriteLogs(string fileName, string type, string content)
        {
            string logPath = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(logPath))
            {
                logPath = AppDomain.CurrentDomain.BaseDirectory + fileName;
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                //path = path + "\\" + DateTime.Now.ToString("yyyyMMdd");
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}
                logPath = logPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                if (!File.Exists(logPath))
                {
                    FileStream fs = File.Create(logPath);
                    fs.Close();
                }
                if (File.Exists(logPath))
                {
                    FileInfo info = new FileInfo(logPath);
                    if(info.Length > 3 * 1024 * 1024)
                    {
                        logPath = AppDomain.CurrentDomain.BaseDirectory + "log" + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                        if(!File.Exists(logPath))
                        {
                            FileStream fs = File.Create(logPath);
                            fs.Close();
                        }
                    }

                    StreamWriter sw = new StreamWriter(logPath, true, System.Text.Encoding.Default);
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "-->" + type + "-->" + content);
                    //  sw.WriteLine("----------------------------------------");
                    sw.Close();
                }
            }
        }

        public static string ToHexStrFromByte(byte[] byteDatas)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < byteDatas.Length; i++)
            {
                builder.Append(string.Format("{0:X2} ", byteDatas[i]));
            }
            return builder.ToString().Trim();
        }

        public static string ToHexStrFromByte(byte[] byteDatas, bool isSpacing)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < byteDatas.Length; i++)
            {
                if (isSpacing)
                {
                    builder.Append(string.Format("{0:X2}", byteDatas[i]));
                }
                else
                {
                    builder.Append(string.Format("{0:X2} ", byteDatas[i]));
                }
            }
            return builder.ToString().Trim();
        }
    }
}
