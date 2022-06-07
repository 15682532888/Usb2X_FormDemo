using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography;

namespace Usb2X_FormDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static List<byte[]> listCmd = new List<byte[]>();

        private void Form1_Load(object sender, EventArgs e)
        {
            listCmd.Add(cmd.cmd_SetCanBps);
            listCmd.Add(cmd.cmd_TransmitStdFrame2Can);
            listCmd.Add(cmd.cmd_SetCanRxFilter01);
            listCmd.Add(cmd.cmd_StartCan);
            listCmd.Add(cmd.cmd_StopCan);
            listCmd.Add(cmd.cmd_Test);
            listCmd.Add(cmd.cmd_KLine25msInitAndStart);
            comboBox2.Items.Add("cmd_SetCanBps");
            comboBox2.Items.Add("cmd_TransmitStdFrame2Can");
            comboBox2.Items.Add("cmd_SetCanRxFilter01");
            comboBox2.Items.Add("cmd_StartCan");
            comboBox2.Items.Add("cmd_StopCan");
            comboBox2.Items.Add("cmd_Test");
            comboBox2.Items.Add("cmd_KLine25msInitAndStart");
            comboBox2.SelectedIndex = 0;

            updateComboBox();

            Thread th1 = new Thread(Thread_RxHandle);
            th1.Start();
        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
            updateComboBox();
        }

        private void updateComboBox()
        {
            comboBox1.Items.Clear();
            foreach (string com in System.IO.Ports.SerialPort.GetPortNames())
            {
                if (!comboBox1.Items.Contains(com))
                {
                    comboBox1.Items.Add(com);
                }
            }

            if (0 < comboBox1.Items.Count)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        static Queue<byte[]> queue = new Queue<byte[]>();

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
                serialPort1.PortName = comboBox1.Text;
                serialPort1.Open();
                serialPort1.Write(cmd.cmd_Test, 0, cmd.cmd_Test.Length);
            }
            catch (Exception er)
            {
                MessageBox.Show("端口打开失败！" + er.Message, "提示");
            }
        }

        private void Thread_RxHandle()
        {
            while (true)
            {
                if (0 < queue.Count)
                {
                    for (int i = 0; i < queue.Count; i++)
                    {
                        usbFramesProcess(queue.Dequeue());
                    }
                }

                Thread.Sleep(5);
            }
        }

        private void usbFramesProcess(byte[] frames)
        {
            if (null != frames)
            {
                usbFrameLength += frames.Length;

                int usbFrameCount = frames.Length / 64;
                for (int i = 0; i < usbFrameCount; i++)
                {
                    byte[] frame = frames.Skip(i * 64).Take(64).ToArray();
                    ListChanged(frame);
                }
            }
        }

        private delegate void SetText(byte[] data);
        private void ListChanged(byte[] data)
        {
            if (this.textBox1.InvokeRequired)
            {
                SetText setText = ListChanged;
                this.Invoke(setText, new object[] { data });
            }
            else
            {
                string s = "";
                for (int i = 0; i < data.Length; i++)
                {
                    s += data[i].ToString("X") + " ";
                }
                this.textBox1.Text = s;
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = serialPort1.BytesToRead;
            byte[] data = new byte[bytes];
            serialPort1.Read(data, 0, bytes);
            if (0 < bytes)
            {
                queue.Enqueue(data);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            serialPort1.Write(cmd.cmd_StartCan, 0, cmd.cmd_StartCan.Length);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(cmd.cmd_StopCan, 0, cmd.cmd_StopCan.Length);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(listCmd[comboBox2.SelectedIndex], 0, listCmd[comboBox2.SelectedIndex].Length);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                string s = textBox2.Text.Replace(" ", "");
                byte[] cmdData = new byte[s.Length / 2];
                for (int i = 0; i < s.Length; i += 2)
                    cmdData[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
                serialPort1.Write(cmdData, 0, cmdData.Length);
            }
        }

        static int usbFrameLength = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            label5.Text = (usbFrameLength / 1024).ToString();
            usbFrameLength = 0;
        }
    }

    class cmd
    {
        public static byte[] cmd_SetCanBps = new byte[7] { 0xa5, 0x5a, 0x00, 0x03, 0x01, 0x00, 0x7d };
        public static byte[] cmd_TransmitStdFrame2Can = new byte[15] { 0xa5, 0x5a, 0x00, 0x0b, 0x00, 0x07, 0xfb, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11 };
        public static byte[] cmd_SetCanRxFilter01 = new byte[13] { 0xa5, 0x5a, 0x00, 0x09, 0x02, 0x07, 0x72, 0x07, 0xfe, 0x07, 0xfd, 0x07, 0xfc };
        public static byte[] cmd_StartCan = new byte[5] { 0xa5, 0x5a, 0x00, 0x01, 0x04 };
        public static byte[] cmd_StopCan = new byte[5] { 0xa5, 0x5a, 0x00, 0x01, 0x05 };
        public static byte[] cmd_Test = new byte[5] { 0xa5, 0x5a, 0x00, 0x01, 0x06 };
        public static byte[] cmd_KLine25msInitAndStart = new byte[9] { 0xa5, 0x5a, 0x00, 0x05, 0x08, 0x81, 0x10, 0xF1, 0x81 };
    }
}
