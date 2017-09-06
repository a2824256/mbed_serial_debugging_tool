using System;
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
        private bool Listening = false;//用于检测是否没有执行完invoke相关操作，仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离  
        private bool WaitClose = false;//invoke里判断是否正在关闭串口是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke ,解决关闭串口时，程序假死，具体参见http://news.ccidnet.com/art/32859/20100524/2067861_4.html 仅在单线程收发使用，但是在公共代码区有相关设置，所以未用#define隔离  
        DispatcherTimer autoSendTick = new DispatcherTimer();//定时发送  
        private String PortName = "";
        private bool PortSwitch = false;
        DispatcherTimer timer = new DispatcherTimer();



        public MainWindow()
        {
            InitializeComponent();
        }

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

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

        private void Button_Connect(object sender, RoutedEventArgs e)
        {
            if (!PortSwitch)
            {
                try
                {
                    this.ComPort.PortName = this.PortName;
                    this.ComPort.BaudRate = 9600;
                    this.ComPort.StopBits = StopBits.One;
                    this.ComPort.Parity = Parity.None;

                    this.ComPort.Open();
                    this.PortSwitch = true;
                    ConnectButton.DataContext = "1";
                    ConnectButton.Content = "Disconnect";
                    ContentBox.Text = ContentBox.Text + this.PortName + "已连接...\n";
                    try
                    {
                        // String content = this.ComPort.ReadLine();
                        // ContentBox.Text = ContentBox.Text + content + "\n";
                        // ReceiveData(ComPort);
                        timer.Interval = new TimeSpan(0, 0, 1);
                        //创建事件处理
                        timer.Tick += new EventHandler(RecData);
                        //开始计时
                        timer.Start();
                    }
                    catch (Exception ex)
                    {
                        this.PrintError(ex);
                        ContentBox.Text = ContentBox.Text + "超时," + ex.Message + "\n";
                    }
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

        private void Button_Receive(object sender, RoutedEventArgs e)
        {
            if (PortSwitch)
            {
                try
                {
                    // String content = this.ComPort.ReadLine();
                    // ContentBox.Text = ContentBox.Text + content + "\n";
                    // ReceiveData(ComPort);
                    timer.Interval = new TimeSpan(0, 0, 1);
                    //创建事件处理
                    timer.Tick += new EventHandler(RecData);
                    //开始计时
                    timer.Start();
                }
                catch (Exception ex)
                {
                    this.PrintError(ex);
                    ContentBox.Text = ContentBox.Text + "超时," + ex.Message + "\n";
                } 

            }
            else
            {
                ContentBox.Text = ContentBox.Text + "请打开串口\n";
            }
        }

        private void RecData(object sender, EventArgs e)
        {
            try
            {
                String content = this.ComPort.ReadLine();
                ContentBox.Text = ContentBox.Text + content + "\n";
            }catch (Exception ex)
            {
                this.PrintError(ex);
                timer.Stop();
            }


        }

        private void ReceiveData(SerialPort serialPort)
        {
            //同步阻塞接收数据线程
            // Thread threadReceive = new Thread(new ParameterizedThreadStart(SynReceiveData));
            // threadReceive.Start(serialPort);

            //也可用异步接收数据线程
            Thread threadReceiveSub = new Thread(new ParameterizedThreadStart(AsyReceiveData));
            threadReceiveSub.Start(serialPort);

        }

        private void SynReceiveData(object serialPortobj)
        {
            SerialPort serialPort = (SerialPort)serialPortobj;
            System.Threading.Thread.Sleep(0);
            serialPort.ReadTimeout = 1000;
            try
            {
                //阻塞到读取数据或超时(这里为2秒)
                ContentBox.Text = ContentBox.Text + this.ComPort.ReadLine(); 
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                //处理超时错误
            }

            serialPort.Close();

        }

        //异步读取
        private void AsyReceiveData(object serialPortobj)
        {
            SerialPort serialPort = (SerialPort)serialPortobj;
            System.Threading.Thread.Sleep(500);
            try
            {
                ContentBox.Text = ContentBox.Text + this.ComPort.ReadLine();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                //处理错误
            }
            serialPort.Close();
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
