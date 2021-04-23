using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoqiangH5Entity
{

    public enum H5Protocol
    {
        BO_QIANG = 0,
        DI_DI,        
    }

    public enum OperationTypeEnum
    {
        默认值 = 0,
        RTC校准 = 1,
        SOC校准 = 2,
        零点校准 = 3,
        负10A校准 = 4,
        上电 = 5,
        下电 = 6,
        关机 = 7,
        休眠 = 8,
        Eeprom写入 = 9,
        MCU写入 = 10,
        一键出厂配置 = 11,
        充放电测试 = 12,
        一键出厂检验 = 13,
        BMS注册 = 14,
        故障擦除 = 15,
        UTC校准 = 16,
        电流校准 = 17,
        BMS绑定 = 18,
        总压校准 = 19,
        进水阻抗校准 = 20,
        保护参数写入 = 21
    }

    public enum OperationResultEnum
    {
        成功 = 1,
        失败 = 2,
        超时未响应 = 3,
        其他 = 4
    }

    public enum BoqiangH5Wnd
    {
        BMS_WND = 0,
        EEPROM_WND,
        MCU_WND,
        RECORD_WND,
        ADJUST_WND,
    }

    public class OneClickConfiguration
    {
        public int checkEepromState = -1;
        public DateTime? adjustRTCTime = null;
        public DateTime? adjustZeroTime = null;
        public DateTime? adjust10ATime = null;
        public DateTime? shallowSleepTime = null;
        public DateTime? deepSleepTime = null;
        public DateTime? writeMCUTime = null;
        public DateTime? writeEepromTime = null;
        public string maxVoltageDiff = string.Empty;
        public string maxTemperatureDiff = string.Empty;
        public string comments = string.Empty;

        public void Init()
        {
            checkEepromState = -1;
            adjustRTCTime = null;
            adjustZeroTime = null;
            adjust10ATime = null;
            shallowSleepTime = null;
            deepSleepTime = null;
            writeMCUTime = null;
            writeEepromTime = null;
            maxVoltageDiff = string.Empty;
            maxTemperatureDiff = string.Empty;
            comments = string.Empty;
        }
    }

    public class OneClickCheck
    {
        public sbyte checkEepromState = -1;
        public string loopNumber = string.Empty;
        public string maxVoltageDiff = string.Empty;
        public string maxTemperatureDiff = string.Empty;
        public string comments = string.Empty;
        public double rtcError = -1;
        public DateTime? checkMCUTime = null;
        public DateTime? checkRTCTime = null;
        public DateTime? adjustSOCTime = null;

        public void Init()
        {
            checkEepromState = -1;
            loopNumber = string.Empty;
            maxVoltageDiff = string.Empty;
            maxTemperatureDiff = string.Empty;
            comments = string.Empty;
            rtcError = -1;
            checkMCUTime = null;
            checkRTCTime = null;
            adjustSOCTime = null;
        }
    }
}
