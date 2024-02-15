using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading; //thread
using System.Net;
using System.IO;
using System.Text.RegularExpressions; //정규표현식
using System.Net.Sockets;

namespace ServerForm
{
    public partial class Form1 : Form
    {
        TcpListener tcpListener; //클라이언트 연결 수신대기
        TcpClient tcpClient1; //클라이언트 연결
        private bool isIPAddr = true;
        private bool isPortAddr = true;
        StreamReader streamReader; //데이타 읽기 위한 스트림리더
        StreamWriter streamWriter; //데이타 쓰기 위한 스트림라이터 

        public Form1()
        {
            InitializeComponent();
        }

        //폼이 로드되면
        private void Form1_Load(object sender, EventArgs e)
        {
            disconnectBtn.Enabled = false;
            sendBtn.Enabled = false;
        }

        //연결하기 버튼 클릭
        private void connectBtn_Click(object sender, EventArgs e) 
        {
            //ipTextBox와 portTextBox의 값이 null이거나 공백이 아니라면 Connect()
            if (!String.IsNullOrWhiteSpace(ipTextBox.Text) && !String.IsNullOrWhiteSpace(portTextBox.Text))
            {
                Task task = Task.Factory.StartNew(async () =>
                {
                    await Connect();
                });

                connectBtn.Enabled = false;
                disconnectBtn.Enabled = true;
                sendBtn.Enabled = true;
            }
            else
            {
                MessageBox.Show("ip번호와 port번호를 전부 다 입력하세요");
            }  
        }

        //연결하는 함수
        //async : 비동기 작업
        //비동기 : A작업 시작과 동시에 B작업 실행, 서버에서 요청 보냈을 때 응답 상태와 상관없이 다음 동작 수행 가능
        private async Task Connect() 
        {
            while(true)
            {
                //TcpListener 객체 생성
                tcpListener = new TcpListener(IPAddress.Parse(ipTextBox.Text), int.Parse(portTextBox.Text));

                //서버 시작
                tcpListener.Start();

                writeRichTextbox("서버 준비.. 클라이언트 기다리는 중..");//텍스트박스에 표시

                //클라이언트 접속 확인
                tcpClient1 = new TcpClient();
                tcpClient1 = await tcpListener.AcceptTcpClientAsync(); 

                writeRichTextbox("클라이언트 연결 성공"); //텍스트박스에 표시

                streamReader = new StreamReader(tcpClient1.GetStream()); //읽기 스트림 연결
                streamWriter = new StreamWriter(tcpClient1.GetStream()); //쓰기 스트림 연결
                streamWriter.AutoFlush = true; //쓰기 버퍼 자동처리

                // 클라이언트가 연결되어 있는 동안
                while (tcpClient1.Connected)  
                {
                    try
                    {
                        connectBtn.Enabled = false; //연결하기 버튼 비활성화

                        // 수신 데이타를 읽어서 receiveData1 변수에 저장
                        string receiveData1 = await streamReader.ReadLineAsync();

                        // 데이타를 수신창에 쓰기
                        if (receiveData1 != null)
                            writeRichTextbox(receiveData1);

                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    
                }
            }
        }

        //richTextbox1에 쓰기 함수
        private void writeRichTextbox(string str) 
        {
            //데이터를 수신창에 표시. 반드시 Invoke 사용해 충돌피함
            //*멀티스레드 환경에서 데이터 보호를 위해 Invoke를 써야함
            //하나의 Form을 다른 thread에서 접근하게 될 경우에 기존의 Form과 충돌이 날 수 있어, invoke 를 사용하여 실행하려고 하는 메소드의 대리자(delegate)를 실행시킴
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.AppendText(str + "\r\n"); });

            //스크롤을 제일 밑으로 가게 함
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.ScrollToCaret(); }); 
        }

        // '보내기' 버튼이 클릭되면
        private void sendBtn_Click(object sender, EventArgs e)  
        {
            try
            {
                string text;
                string time = DateTime.Now.ToString("HH:mm:ss");

                // sendTextBox의 내용을 sendData1 변수에 저장
                string sendData1 = sendTextBox.Text;
                text = $"[Server] [{time}] {sendData1}";

                // 스트림라이터를 통해 데이타를 전송
                streamWriter.WriteLine(text);

                writeRichTextbox(text);
            }
                
           catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // '연결끊기' 버튼이 클릭되면
        private void disconnectBtn_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("연결을 끊으시겠습니까?", "연결끊기", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                streamReader.Close();
                streamWriter.Close();
                tcpListener.Stop();
                tcpClient1.Close(); //연결 끊기

                connectBtn.Enabled = true; //연결하기 버튼 활성화
                writeRichTextbox("클라이언트와의 연결 끊김.. 다시 연결하려면 연결하기를 누르세요.");
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

            if (isPortAddr) //정규식을 이용한 유효성 검사를 하여 port번호가 참이라면
            {
                connectBtn.Enabled = true; //연결하기 버튼 활성화
            }

            else
            {
                MessageBox.Show("Port주소를 형식에 맞게 입력 바랍니다.");
                connectBtn.Enabled = false; //연결하기 버튼 비활성화
            }
        }

        //정규식을 이용한 port번호 유효성 검사
        public void IsValidPort(string port)
        {
            Regex regex = new Regex(@"^((6553[0-5])|(655[0-2][0-9])|(65[0-4][0-9]{2})|(6[0-4][0-9]{3})|([1-5][0-9]{4})|([0-5]{0,5})|([0-9]{1,4}))$");
            isPortAddr = regex.IsMatch(port);
        }

    }
}
