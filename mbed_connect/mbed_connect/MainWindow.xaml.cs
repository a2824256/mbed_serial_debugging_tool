using System;
using System.Collections.Generic;
using System.IO.Ports;
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
        IList<customer> comList = new List<customer>();//可用串口集合  
        DispatcherTimer autoSendTick = new DispatcherTimer();//定时发送  
#if MULTITHREAD  
        private static bool Sending = false;//正在发送数据状态字  
        private static Thread _ComSend;//发送数据线程  
        Queue recQueue = new Queue();//接收数据过程中，接收数据线程与数据处理线程直接传递的队列，先进先出  
        private  SendSetStr SendSet = new SendSetStr();//发送数据线程传递参数的结构体  
        private  struct SendSetStr//发送数据线程传递参数的结构体格式  
        {  
            public string SendSetData;//发送的数据  
            public bool? SendSetMode;//发送模式  
        }  
#endif  
        public class customer//各下拉控件访问接口  
        {

            public string com { get; set; }//可用串口  
            public string com1 { get; set; }//可用串口  
            public string BaudRate { get; set; }//波特率  
            public string Parity { get; set; }//校验位  
            public string ParityValue { get; set; }//校验位对应值  
            public string Dbits { get; set; }//数据位  
            public string Sbits { get; set; }//停止位  


        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String ComName = "";
            ComName = Cb.SelectedValue.ToString();
            ContentBox.Text = ContentBox.Text + "已自动选择" + ComName + "...\n";
            ContentBox.Text = ContentBox.Text + "Waiting connect...\n";
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
                ContentBox.Text = ContentBox.Text + "无可用串口...\n";
            }
        }

        private void Button_Connect(object sender, RoutedEventArgs e)
        {

        }

        private void Clean_Content(object sender, RoutedEventArgs e)
        {
            ContentBox.Text = "";
        }
    }
}
