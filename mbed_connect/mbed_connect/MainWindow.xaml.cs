﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace mbed_connect
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort ComPort = new SerialPort();//声明一个串口        
        private string[] ports;//可用串口数组  
        private bool recStaus = true;//接收状态字  
        private bool ComPortIsOpen = false;//COM口开启状态字，在打开/关闭串口中使用，这里没有使用自带的ComPort.IsOpen，因为在串口突然丢失的时候，ComPort.IsOpen会自动false，逻辑混乱  
        //private bool Listening = false;//用于检测是否没有执行完invoke相关操作，仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离  
        //private bool WaitClose = false;//invoke里判断是否正在关闭串口是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke ,解决关闭串口时，程序假死，具体参见http://news.ccidnet.com/art/32859/20100524/2067861_4.html 仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离  
        //DispatcherTimer autoSendTick = new DispatcherTimer();//定时发送  
        private String PortName = "";
        private bool PortSwitch = false;
        DispatcherTimer timer = new DispatcherTimer();


        //创建窗口
        public MainWindow()
        {
            InitializeComponent();
        }

        //下拉框选择事件
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.PortName = Cb.SelectedValue.ToString();
                ContentBox.Text = ContentBox.Text + "已自动选择" + this.PortName + "...\n";
                ContentBox.Text = ContentBox.Text + "Waiting connect...\n";
            }
            catch (Exception ex)
            {
                this.PrintError(ex);
                Cb.Items.Clear();
            }
        }

        //点击扫描按钮触发事件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }


        //更新列表
        private void UpdateList()
        {
            ports = SerialPort.GetPortNames();//获取可用串口
            if (ports.Length > 0)//ports.Length > 0说明有串口可用  
            {
                ContentBox.Text = ContentBox.Text + "有可用串口" + ports.Length + "个...\n";
                for (int i = 0; i < ports.Length; i++)
                {
                    Cb.Items.Add(ports[i]);
                    Cb.SelectedIndex = i;
                }
            }
            else//未检测到串口  
            {
                Cb.Items.Clear();
                this.PortName = "";
                ContentBox.Text = ContentBox.Text + "无可用串口...\n";
            }
        }

        //连接串口
        private void Button_Connect(object sender, RoutedEventArgs e)
        {
            if (!PortSwitch)
            {
                try
                {
                    //串口名称
                    this.ComPort.PortName = this.PortName;
                    //波特率
                    this.ComPort.BaudRate = 9600;
                    //停止位
                    this.ComPort.StopBits = StopBits.One;
                    this.ComPort.Parity = Parity.None;
                    this.ComPort.ReceivedBytesThreshold = 1;
                    //打开串口
                    this.ComPort.Open();
                    this.PortSwitch = true;
                    this.ComPort.DataReceived += new SerialDataReceivedEventHandler(RecData);
                    ConnectButton.DataContext = "1";
                    //修改按键名称
                    ConnectButton.Content = "Disconnect";
                    ContentBox.Text = ContentBox.Text + this.PortName + "已连接...\n";
                    //try
                    //{
                    //    // String content = this.ComPort.ReadLine();
                    //    // ContentBox.Text = ContentBox.Text + content + "\n";
                    //    // ReceiveData(ComPort);
                    //    //SerialDataReceivedEventHandler
                    //    //timer.Interval = new TimeSpan(0, 0, 1);
                    //    //创建事件处理
                    //    //timer.Tick += new EventHandler(RecData);
                    //    //开始计时
                    //    //timer.Start();
                    //}
                    //catch (Exception ex)
                    //{
                    //    this.PrintError(ex);
                    //    ContentBox.Text = ContentBox.Text + "超时," + ex.Message + "\n";
                    //}
                }
                catch (Exception ex)
                {
                    this.PortSwitch = false;
                    this.PrintError(ex);
                    ContentBox.Text = ContentBox.Text + "串口打开失败\n";
                }
            }else if(PortSwitch)
            {
                try
                {
                    timer.Stop();
                    this.ComPort.Close();
                    ConnectButton.DataContext = "0";
                    ConnectButton.Content = "Connect";
                    this.PortSwitch = false;
                    ContentBox.Text = ContentBox.Text + "串口已断开...\n";
                }
                catch (Exception ex)
                {
                    this.PrintError(ex);
                    ConnectButton.Content = "Connect";
                    this.PortSwitch = false;
                    ContentBox.Text = ContentBox.Text + "串口关闭失败\n";
                }
            }
        }

        //子线程执行任务
        private void RecData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                String content = this.ComPort.ReadLine();
                ContentBox.Dispatcher.BeginInvoke(new Action(() => ContentBox.Text = ContentBox.Text + content + "\n"));
            }catch (Exception ex)
            {
                ContentBox.Dispatcher.BeginInvoke(new Action(() =>ContentBox.Text = ContentBox.Text + "-----------发生异常----------\n" + ex + "\n\n"
                + "请等待作者修复\n" + "------------------------------\n"));
            }


        }

        private void Clean_Content(object sender, RoutedEventArgs e)
        {
            ContentBox.Text = "";
        }

        private void PrintError(Exception ex)
        {
            Console.WriteLine(ex);
            ContentBox.Text = ContentBox.Text + "-----------发生异常----------\n";
            ContentBox.Text = ContentBox.Text + ex + "\n\n";
            ContentBox.Text = ContentBox.Text + "请等待作者修复\n";
            ContentBox.Text = ContentBox.Text + "------------------------------\n";
        }
    }
}
