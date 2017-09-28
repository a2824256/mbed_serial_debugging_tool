using System;
using System.Collections;
using System.IO;
//串口库
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace mbed_connect
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort ComPort = new SerialPort();//声明一个串口        
        private string[] ports;//可用串口数组  
        //private bool recStaus = true;//接收状态字
        private String PortName = "";
        private bool PortSwitch = false;
        private static string LogFilePath = @".\log.txt";
        private static StreamWriter ErrorWriter;
        private static string WriterInstancePath = "";
        private static Queue queue = new Queue(13);

        //创建窗口
        public MainWindow()
        {
            InitializeComponent();
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            queue.Enqueue(35.0);
            //检查日志文件
            CreateDiagram();
            MakeFile();
        }

        //public static StreamWriter GetWriterInstance(string path, bool IsWarning = true)
        //{
        //    if (IsWarning)
        //    {
        //        if(ErrorWriter == null)
        //        {
        //            ErrorWriter = new StreamWriter(LogFilePath);
        //        }
        //        return ErrorWriter;
        //    }
        //    else
        //    {
        //        if(writer == null || WriterInstancePath != path)
        //        {
        //            writer = new StreamWriter(path);
        //        }
        //        return writer;
        //    }
        //}

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
                    //检测缓冲区输入Handler
                    this.ComPort.DataReceived += new SerialDataReceivedEventHandler(RecData);
                    ConnectButton.DataContext = "1";
                    //修改按键名称
                    ConnectButton.Content = "Disconnect";
                    ContentBox.Text = ContentBox.Text + this.PortName + "已连接...\n";
                    ErrorWriter = new StreamWriter(LogFilePath);
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
                    ContentBox.Text += "串口打开失败\n";
                }
            }
            else if (PortSwitch)
            {
                try
                {
                    this.ComPort.Close();
                    ConnectButton.DataContext = "0";
                    ConnectButton.Content = "Connect";
                    this.PortSwitch = false;
                    ContentBox.Text += "串口已断开...\n";
                    ErrorWriter.Close();
                    if(ErrorWriter == null)
                    {
                        ContentBox.Text += "文件IO对象已关闭...\n";
                    }
                }
                catch (Exception ex)
                {
                    this.PrintError(ex);
                    ConnectButton.Content = "Connect";
                    this.PortSwitch = false;
                    ContentBox.Text = ContentBox.Text + "串口关闭失败\n";
                    ErrorWriter.Close();
                }
            }
        }

        //子线程执行接收数据任务
        private void RecData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                String content = this.ComPort.ReadLine();
                ContentBox.Dispatcher.BeginInvoke(new Action(() => ContentBox.Text = ContentBox.Text + content + "\n"));
                Thread thread = new Thread(() => PrintLog(content));
                thread.Start();
            }
            catch (Exception ex)
            {
                ContentBox.Dispatcher.BeginInvoke(new Action(() => ContentBox.Text += "-----------发生异常----------\n" + ex + "\n\n"
                + "\n" + "------------------------------\n"));
            }


        }

        //清空文本框
        private void Clean_Content(object sender, RoutedEventArgs e)
        {
            ContentBox.Text = "";
        }

        //随机温度
        private void RandTemp(object sender, RoutedEventArgs e)
        {
            Random ran = new Random();
            //出队
            queue.Dequeue();
            //入队
            queue.Enqueue(ran.Next(35,45));
            layout.Children.RemoveRange(0, 20);
            CreateDiagram();
        }

        //private void CreateD(object sender, RoutedEventArgs e)
        //{
        //    CreateDiagram();
        //}

        //输出错误
        private void PrintError(Exception ex)
        {
            Console.WriteLine(ex);
            string content = "-----------发生异常----------\n" + ex + "\n\n" + "------------------------------\n";
            Thread thread = new Thread(() => PrintLog(content));
            thread.Start();
            ContentBox.Text += content;
        }

        //检查日志文件是否存在
        private void MakeFile()
        {
            if (File.Exists(LogFilePath))
            {
                string FullLogFilePath = System.IO.Path.GetFullPath(LogFilePath);
                ContentBox.Text = ContentBox.Text + "日志文件:" + FullLogFilePath + "\n";
            }
            else
            {
                ContentBox.Text = ContentBox.Text + "日志文件-log.txt不存在\n";
                FileStream LogFile = File.Create(LogFilePath);
            }
        }

        //打印日志
        private async void PrintLog(string content)
        {
            ErrorWriter.WriteLine(content);
            ErrorWriter.Flush();
        }

        //创建图表
        private void CreateDiagram()
        {
            Random ran = new Random();
            double x = 20;
            //PrintValues(queue);
            foreach(Object obj in queue)
            {
                DrawRectangle(Convert.ToDouble(obj), x);
                x += 80;
            }
        }

        //画柱子
        private void DrawRectangle(double value,double x)
        {
            int DiagramHeight = 226;
            double temp = (value-35) / 10;
            double RecHeight = temp * 200;
            RecHeight = RecHeight > 0 ? RecHeight : 2;
            RecHeight = Math.Round(RecHeight);
            Rectangle r = new Rectangle();
            r.Fill = new SolidColorBrush(Colors.Blue);
            r.Width = 40;
            r.Height = RecHeight;
            double MarginTop = 226 - RecHeight;
            r.SetValue(Canvas.LeftProperty, x);
            r.SetValue(Canvas.TopProperty, MarginTop);
            layout.Children.Add(r);
            Label label = new Label();
            label.Content = (int)value;
            label.Width = 40;
            label.Foreground = new SolidColorBrush(Colors.White);
            label.SetValue(Canvas.LeftProperty, x);
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.SetValue(Canvas.TopProperty, MarginTop-20);
            layout.Children.Add(label);
        }
        
        //清空图表
        private void CleanDiagram()
        {
            layout.Children.RemoveRange(0, 26);
        }

        //打印队列
        public static void PrintValues(IEnumerable myCollection)
        {
            foreach (Object obj in myCollection)
                Console.Write("    {0}", obj);
            Console.WriteLine();
        }
    }
}
