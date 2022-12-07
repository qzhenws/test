

#define LIN_MASTER

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using USB2XXX;

namespace USB2XXX_LIN_Test
{

    public partial class Form1 : Form
    {
        bool MSGStringFlag = true;
        String MSGString;
        Int32[] DevHandles = new Int32[20];
        Int32 DevHandle = 0;
        Byte LINIndex = 0;
        bool state;
        bool Initstate = false;
        Int32 DevNum, ret;
        static Byte tickCnt = 0;
        byte[] RcvDataBuffer = new byte[8];
        public Form1()
        {
            try
            {
                InitializeComponent();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {


                usb_device.DEVICE_INFO DevInfo = new usb_device.DEVICE_INFO();
                //扫描查找设备
                DevNum = usb_device.USB_ScanDevice(DevHandles);
                if (DevNum <= 0)
                {
                    Console.WriteLine("No device connected!");
                    return;
                }
                else
                {
                    Console.WriteLine("Have {0} device connected!", DevNum);
                }
                DevHandle = DevHandles[0];
                //打开设备
                state = usb_device.USB_OpenDevice(DevHandle);
                if (!state)
                {
                    Console.WriteLine("Open device error!");
                    textBox1.Text = "Open Fail...";
                    textBox1.BackColor = Color.Red;
                    USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 0);
                    return;
                }
                else
                {
                    Console.WriteLine("Open device success!");
                    textBox1.Text = "Opend!!!";
                    textBox1.BackColor = Color.Green;
                    USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 1); //12v
                }
                //获取固件信息
                StringBuilder FuncStr = new StringBuilder(256);
                state = usb_device.DEV_GetDeviceInfo(DevHandle, ref DevInfo, FuncStr);
                if (!state)
                {
                    Console.WriteLine("Get device infomation error!");
                    return;
                }
                else
                {
                    Console.WriteLine("Firmware Info:");
                    Console.WriteLine("    Name:" + Encoding.Default.GetString(DevInfo.FirmwareName));
                    Console.WriteLine("    Build Date:" + Encoding.Default.GetString(DevInfo.BuildDate));
                    Console.WriteLine("    Firmware Version:v{0}.{1}.{2}", (DevInfo.FirmwareVersion >> 24) & 0xFF, (DevInfo.FirmwareVersion >> 16) & 0xFF, DevInfo.FirmwareVersion & 0xFFFF);
                    Console.WriteLine("    Hardware Version:v{0}.{1}.{2}", (DevInfo.HardwareVersion >> 24) & 0xFF, (DevInfo.HardwareVersion >> 16) & 0xFF, DevInfo.HardwareVersion & 0xFFFF);
                    Console.WriteLine("    Functions:" + DevInfo.Functions.ToString("X8"));
                    Console.WriteLine("    Functions String:" + FuncStr);
                }
                //初始化配置LIN
                USB2LIN.LIN_CONFIG LINConfig = new USB2LIN.LIN_CONFIG();
                LINConfig.BaudRate = 19200;
                LINConfig.BreakBits = USB2LIN.LIN_BREAK_BITS_11;
                LINConfig.CheckMode = USB2LIN.LIN_CHECK_MODE_EXT;
#if LIN_MASTER
                LINConfig.MasterMode = USB2LIN.LIN_MASTER;
#else
            LINConfig.MasterMode = USB2LIN.LIN_SLAVE;
#endif
                ret = USB2LIN.LIN_Init(DevHandle, LINIndex, ref LINConfig);
                if (ret != USB2LIN.LIN_SUCCESS)
                {
                    Console.WriteLine("Config LIN failed!");
                    textBox1.Text = "Open LIN failed!";
                    return;
                }
                else
                {
                    Console.WriteLine("Config LIN Success!");
                    textBox1.Text = "Open LIN Success!";
                }
#if LIN_MASTER
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (true == Initstate)
                {
                    Initstate = false;
                    button_start.Text = "开始";
                    button_start.ForeColor = System.Drawing.SystemColors.ControlText;
                    USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 0);//设置输出电压（0=0V,1=12V,2=5V）

                }
                else if (false == Initstate)
                {
                    Initstate = true;
                    button_start.Text = "停止";
                    button_start.ForeColor = Color.DarkRed;
                    USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 1);//设置输出电压（0=0V,1=12V,2=5V）
                    timer1.Stop();
                    timer1.Start();
                    tickCnt = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {

                int[] bytArray = new int[8];
                tickCnt++;
                if (true == Initstate)
                {

                    if (0u == tickCnt % 2)
                    {
                        if (20 > tickCnt)
                        {
                            Send_data(new Byte[8] { 0xff, 0xff, 0xff, 0x7F, 0x00, 0x00, 0x14, 0x00 });
                        }
                        else 
                        {
                            Send_data(new Byte[8] { 0xff, 0xff, 0xff, 0x7F, 0x00, 0x00, 0x18, 0x00 });
                        }
                    }
                    else
                    {
                        RcvDataBuffer = Receive_data();

                        if (10 < tickCnt)
                        {
                            if (0x63 == RcvDataBuffer[1])
                            {
                                Initstate = false;
                                button_start.Text = "开始";
                                button_start.ForeColor = System.Drawing.SystemColors.ControlText;
                                MessageBox.Show("LIN message reset happenend!!!!");
                            }
                        }
                    }
                    if (40 == tickCnt)
                    {
                        USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 0);//设置输出电压（0=0V,1=12V,2=5V）
                    }
                    if (44 <= tickCnt)
                    {
                        tickCnt = 0;
                        USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 1);//设置输出电压（0=0V,1=12V,2=5V）
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        //若出现USB断开连接，使用该函数进行重连设备
        public bool LIN_ReConnect()
        {
            try
            {
                DevNum = usb_device.USB_ScanDevice(DevHandles);// 扫描查找设备
                if (DevNum <= 0)
                {
                    textBox1.Text = "没有连接驱动器！";//
                    return false;
                }
                DevHandle = DevHandles[0];
                bool state = usb_device.USB_OpenDevice(DevHandle);//打开设备
                if (state)
                {
                    int ret;
                    ret = USB2LIN_EX.LIN_EX_Init(DevHandle, LINIndex, 19200, 1);//初始化为主机
                    if (ret == USB2LIN_EX.LIN_EX_SUCCESS)
                    {
                        textBox1.Text = "驱动器连接成功！";//
                        USB2LIN_EX.LIN_EX_CtrlPowerOut(DevHandle, LINIndex, 0);//设置输出电压（0=0V,1=12V,2=5V）
                        return true;
                    }
                }
                textBox1.Text = "驱动器连接失败！";//

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return false;
        }

        public void Send_data(byte[] byteArray)//发送数据
        {
            try
            {
                ret = USB2LIN_EX.LIN_EX_MasterWrite(DevHandle, LINIndex, (byte)0x2C, byteArray, (byte)byteArray.Length, USB2LIN_EX.LIN_EX_CHECK_EXT);//发送耗时10ms
                if (ret != USB2LIN_EX.LIN_EX_SUCCESS)
                {
                    LIN_ReConnect();
                    MSGString += DateTime.Now.ToString("HH:mm:ss.fff") + " " + "ID[3D] " + "LIN_ReConnect!" + "\r\n";
                }
                if (MSGStringFlag == true)
                {
                    MSGString += DateTime.Now.ToString("mm:ss.fff") + " " + "[2C] " +
                                        (byteArray[0]).ToString("X2") + " " + (byteArray[1]).ToString("X2") + " " +
                                        (byteArray[2]).ToString("X2") + " " + (byteArray[3]).ToString("X2") + " " +
                                        (byteArray[4]).ToString("X2") + " " + (byteArray[5]).ToString("X2") + " " +
                                        (byteArray[6]).ToString("X2") + " " + (byteArray[7]).ToString("X2") + "\r\n";

                    textBox2.AppendText(DateTime.Now.ToString("mm:ss.fff") + " " + "[2C] " +
                                        (byteArray[0]).ToString("X2") + " " + (byteArray[1]).ToString("X2") + " " +
                                        (byteArray[2]).ToString("X2") + " " + (byteArray[3]).ToString("X2") + " " +
                                        (byteArray[4]).ToString("X2") + " " + (byteArray[5]).ToString("X2") + " " +
                                        (byteArray[6]).ToString("X2") + " " + (byteArray[7]).ToString("X2") + "\r\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        public byte[] Receive_data()//接收数据
        {
            
            byte[] DataBuffer = new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            try
            { 
                ret = USB2LIN_EX.LIN_EX_MasterRead(DevHandle, LINIndex, (byte)0x08, DataBuffer);
                if (ret <= USB2LIN_EX.LIN_EX_SUCCESS)//判断读取结果
                {
                    if (ret < 0)//LIN通讯错误
                    {
                        LIN_ReConnect();//重新连接
                    }
                    else
                    {

                        textBox2.AppendText(DateTime.Now.ToString("mm:ss.fff") + " " + "[08] " + "\r\n");
                  


                    }
                }
                else
                {//主机发送数据成功后，也会接收到发送出去的数据，通过接收回来的数据跟发送出去的数据对比，可以判断发送数据的时候，数据是否被冲突
                    if (MSGStringFlag == true)
                    {
                        MSGString += DateTime.Now.ToString("mm:ss.fff") + " " + "ID[3D] " +
                                        (DataBuffer[0]).ToString("X2") + " " + (DataBuffer[1]).ToString("X2") + " " +
                                        (DataBuffer[2]).ToString("X2") + " " + (DataBuffer[3]).ToString("X2") + " " +
                                        (DataBuffer[4]).ToString("X2") + " " + (DataBuffer[5]).ToString("X2") + " " +
                                        (DataBuffer[6]).ToString("X2") + " " + (DataBuffer[7]).ToString("X2") + "\r\n";

                        textBox2.AppendText(DateTime.Now.ToString("mm:ss.fff") + " " + "[08] " +
                                        (DataBuffer[0]).ToString("X2") + " " + (DataBuffer[1]).ToString("X2") + " " +
                                        (DataBuffer[2]).ToString("X2") + " " + (DataBuffer[3]).ToString("X2") + " " +
                                        (DataBuffer[4]).ToString("X2") + " " + (DataBuffer[5]).ToString("X2") + " " +
                                        (DataBuffer[6]).ToString("X2") + " " + (DataBuffer[7]).ToString("X2") + "\r\n");
                    }
                }
                return DataBuffer;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return DataBuffer;
            }
        }
    }
}
    
