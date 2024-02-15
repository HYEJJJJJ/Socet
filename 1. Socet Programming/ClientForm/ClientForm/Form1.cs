using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions; //정규표현식

namespace ClientForm
{
    public partial class Form1 : Form
    {
        TcpClient tcpClient1;  //클라이언트 연결
        private bool isIPAddr = true;
        private bool isPortAddr = true;
        StreamReader streamReader;  // 데이타 읽기 위한 스트림리더
        StreamWriter streamWriter;  // 데이타 쓰기 위한 스트림라이터 


        public Form1()
        {
            InitializeComponent();
        }

        //폼이 로드되면
        private void Form1_Load(object sender, EventArgs e)
        {
            //tcpClient1 = new TcpClient();  // TcpClient 객체 생성

            disconnectBtn.Enabled = false;
            sendBtn.Enabled = false;
        }

        // '연결하기' 버튼이 클릭되면
        private void connectBtn_Click(object sender, EventArgs e) 
        {
            //ipTextBox와 portTextBox의 값이 null이거나 공백이 아니라면 Connect()
            if (!String.IsNullOrWhiteSpace(ipTextBox.Text) && !String.IsNullOrWhiteSpace(portTextBox.Text))
            {
                Task task = Task.Factory.StartNew(async () =>
                {
                    await connect();
                });

                disconnectBtn.Enabled = true;
                sendBtn.Enabled = true;
            }
            else
            {
                MessageBox.Show("ip번호와 port번호를 전부 다 입력하세요");
            }
            //Thread thread1 = new Thread(connect);  // Thread 객체 생성, Form과는 별도 쓰레드에서 connect 함수가 실행됨.
            //thread1.IsBackground = true;  // Form이 종료되면 thread1도 종료.
            //thread1.Start();  // thread1 시작.
        }

        //연결하는 함수
        private async Task connect()  
        {
            //TcpClient 객체 생성
            tcpClient1 = new TcpClient();

            // IP주소와 Port번호를 할당
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse(ipTextBox.Text), int.Parse(portTextBox.Text));  
            Console.WriteLine(ipEnd);

           
            // 서버에 연결 요청
            tcpClient1.Connect(ipEnd); 

            writeRichTextbox("서버 연결됨...");

            streamReader = new StreamReader(tcpClient1.GetStream());  // 읽기 스트림 연결
            streamWriter = new StreamWriter(tcpClient1.GetStream());  // 쓰기 스트림 연결
            streamWriter.AutoFlush = true;  // 쓰기 버퍼 자동으로  처리

            // 클라이언트가 연결되어 있는 동안
            while (true)  
            {
                try
                {                    
                    //connectBtn.Enabled = false; //연결하기 버튼 비활성화
                    //string receiveData1 = streamReader.ReadLine();
                    // 수신 데이타를 읽어서 receiveData1 변수에 저장
                    string receiveData1 = await streamReader.ReadLineAsync();

                    //richTextBox1.Invoke((MethodInvoker)delegate () {
                    //    richTextBox1.AppendText(Environment.NewLine + "msg");
                    //});

                    writeRichTextbox(receiveData1);  // 데이타를 수신창에 쓰기
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    tcpClient1.Close();
                    break;
                }
            }
 
            
        }

        // richTextbox1 에 쓰기 함수
        private void writeRichTextbox(string data)  
        {
            //  데이타를 수신창에 표시, 반드시 invoke 사용. 충돌피함.
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.AppendText(data + "\r\n"); });

            // 스크롤을 젤 밑으로.
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.ScrollToCaret(); });  
        }

        // '보내기' 버튼이 클릭되면
        private void sendBtn_Click(object sender, EventArgs e)  
        {
            //*streamWriter가 null이 아니라면
            if (streamWriter != null)
            {
                string text;
                string time = DateTime.Now.ToString("HH:mm:ss");

                // sendTextBox의 내용을 sendData1 변수에 저장
                string sendData1 = sendTextBox.Text; 
                text = $"[Client] [{time}] {sendData1}";

                // 스트림라이터를 통해 데이타를 전송
                streamWriter.WriteLine(text);

                writeRichTextbox(text);
            }
            else
            {
                MessageBox.Show("현재 연결된 서버가 없습니다.");
            }

        }

        // '연결끊기' 버튼이 클릭되면
        private void disconnectBtn_Click(object sender, EventArgs e) 
        {
            //*streamWriter이 없을 때 (null처리 또는 조건)
            if (streamWriter != null)
            {
                if (MessageBox.Show("연결을 끊으시겠습니까?", "연결끊기", MessageBoxButtons.YesNo) == DialogResult.Yes && tcpClient1 != null)
                {
                    connectBtn.Enabled = true; //연결하기 버튼 활성화
                    disconnectBtn.Enabled = false;
                    sendBtn.Enabled = false;

                    streamReader.Close();
                    streamWriter.Close();
                    tcpClient1.Close(); //연결 끊기


                    writeRichTextbox("서버와의 연결 끊김.. 다시 연결하려면 연결하기를 누르세요.");
                }

            }

            else
            {
                MessageBox.Show("서버와 연결되어 있지 않습니다.");
            }

        }

        //ipTextBox에 입력하고 벗어난다면
        private void ipTextBox_Leave(object sender, EventArgs e) 
        {
            IsValidIp(ipTextBox.Text.Replace(" ", "")); 

            if (isIPAddr) //정규식을 이용한 IP 유효성 검사를 하여 IP주소가 참이라면
            {
                connectBtn.Enabled = true; //연결하기 버튼 활성화
            }

            else
            {
                MessageBox.Show("IP주소를 형식에 맞게 입력 바랍니다.");
                connectBtn.Enabled = false; //연결하기 버튼 비활성화
            }
        }


        //정규식을 이용한 IP 유효성 검사
        public void IsValidIp(string ip) 
        {
            Regex regex = new Regex(@"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$"); //정규식
            isIPAddr = regex.IsMatch(ip);
        }

        //portTextBox에 입력하고 벗어난다면
        private void portTextBox_Leave(object sender, EventArgs e)
        {
            IsValidPort(portTextBox.Text.Replace(" ", ""));

            if (isPortAddr) //정규식을 이용한 유효성 검사를 하여 Port번호가 참이라면
            {
                connectBtn.Enabled = true; //연결하기 버튼 활성화
            }

            else
            {
                MessageBox.Show("Port주소를 형식에 맞게 입력 바랍니다.");
                connectBtn.Enabled = false; //연결하기 버튼 비활성화
            }
        }

        //정규식을 이용한 Port번호 유효성 검사
        public void IsValidPort(string port) 
        {
            Regex regex = new Regex(@"^((6553[0-5])|(655[0-2][0-9])|(65[0-4][0-9]{2})|(6[0-4][0-9]{3})|([1-5][0-9]{4})|([0-5]{0,5})|([0-9]{1,4}))$");
            isPortAddr = regex.IsMatch(port);
        }

    }
}
