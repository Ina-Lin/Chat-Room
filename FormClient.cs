using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;   ///KillMessageBox



namespace ChatClient
{
    public partial class FormClient : Form
    {

        ChattingRom CR;
        private int min=0;
        private int max =100;
        private int B = 0;
        private int I = 0;
        private int U = 0;
        private bool isfinish = false;
        private bool isExit = false; //是否正常退出
        private bool isSignIn = false; //是否登入
        private TcpClient client;
        public BinaryReader br;
        public BinaryWriter bw;
        BackgroundWorker connectWork = new BackgroundWorker();
        private string serverIP = "127.0.0.1";
        private bool gameisOpen=false;

        ///
        ///關閉MessageBox引數
        ///
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        public const int WM_CLOSE = 0x10; 
        
        public FormClient()
        {
            InitializeComponent();
            this.AddOwnedForm(CR);
            this.StartPosition = FormStartPosition.CenterScreen;
            Random r = new Random((int)DateTime.Now.Ticks);
            textBox1.Text = "user" + r.Next(100, 999);
            lst_OnlineUser.HorizontalScrollbar = true;
            connectWork.DoWork += new DoWorkEventHandler(connectWork_DoWork);
            connectWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(connectWork_RunWorkerCompleted);
           }
    
        /// <summary>
        /// 非同步方式與伺服器進行連接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void connectWork_DoWork(object sender, DoWorkEventArgs e)
        {
            client = new TcpClient();
            IAsyncResult result = client.BeginConnect(serverIP, 8889, null, null);
            while (!result.IsCompleted)
            {
                Thread.Sleep(100);
                AddStatus(".");
            }
            try
            {
                client.EndConnect(result);
                e.Result = "success";
            }
            catch (Exception ex)
            {
                e.Result = ex.Message;
                return;
            }
        }

        /// <summary>
        /// 非同步方式與伺服器完成連接操作後的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void connectWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result.ToString() == "success")
            {
                isSignIn = true;
                AddStatus("連接成功");
                //獲取網路流
                NetworkStream networkStream = client.GetStream();
                //將網路流作為二進位讀寫物件
                br = new BinaryReader(networkStream);
                bw = new BinaryWriter(networkStream);
                AsyncSendMessage("Login," + textBox1.Text);
                Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
                threadReceive.IsBackground = true;
                threadReceive.Start();

                button2.Enabled = true;
                button3.Enabled = true;
            }
            else
            {
                AddStatus("連接失敗:" + e.Result);
                button1.Enabled = true;
                button2.Enabled = false;
            }
        }

        
        /// <summary>
        /// 處理接收的伺服器收據
        /// </summary>
        private void ReceiveData()
        {
            string receiveString = null;
            while (!isExit)
            {
                ReceiveMessageDelegate d = new ReceiveMessageDelegate(receiveMessage);
                IAsyncResult result = d.BeginInvoke(out receiveString, null, null);
                //使用輪詢方式來盤點非同步作業是否完成
                while (!result.IsCompleted)
                {
                    if (isExit)
                        break;
                    Thread.Sleep(250);
                }
                //獲取Begin方法的返回值所有輸入/輸出參數
                d.EndInvoke(out receiveString, result);
                if(receiveString == null)
                {
                    if(!isExit)
                        MessageBox.Show("與伺服器失去聯繫");
                    break;
                }
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                
                switch (command)
                {
                    case "login":   //格式： login,用戶名
                        AddOnline(splitString[1]);
                        break;
             
                    case "logout":  //格式： logout,用戶名
                        RemoveUserName(splitString[1]);
                        break;

                    case "talk":    //格式： talk,用戶名,對話資訊
                        AddTalkMessage(receiveString.Substring(splitString[0].Length + 1));
                        break;

                    case "talkone":
                        
                        if (CR == null)
                        {
                            CR = new ChattingRom();
                            CR.AddTalkMessage(splitString[1] + splitString[2]);
                            CR.SetText(splitString[1]);
                            CR.talker = splitString[1];
                            CR.chatting += new Chatting(chatting);
                            Application.Run(CR);   
                        }
                        else
                        {
                            CR.rtf_MessageInfo.AppendText(splitString[1] + splitString[2]);
                            CR.AddTalkMessage(splitString[1] + splitString[2]);
                            CR.chatting += new Chatting(chatting);
                         //   Application.Run(CR);
                        }
                //        
                        break;

                    case "talkall":    //格式： talkAll,計件者,資訊
                        AddTalkMessage(receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        break;
                   
                    case "formatone":
                        CR.SetFont(receiveString);
                        break;
                    
                    case "format":
                    case "formatall":
                        SetFont(receiveString);
                        break;

                    case "allready":    
                        gameisOpen = true;
                        button3.Enabled = false;
                        killMessageBox();
                        break;

                    case "startgame":
               
                       if (splitString[4] == "true")
                        {
                            max = int.Parse(splitString[2]);                       
                            min = int.Parse(splitString[3]);
                        }
                       else
                       {
                           max = 100;
                           min = 0;
                       }
                        Game a = new Game(splitString[1], min.ToString(), max.ToString(), textBox1.Text);
                        a.finish += new CheckFinish(checkfinish);
                        a.leave += new CancelGame(cancelGame);
                        Application.Run(a);
                        break;

                    case "gameisover":
                        Form.CheckForIllegalCrossThreadCalls = false;
                        button2.Enabled = true;        
                        button3.Enabled = true;          
                        button4.Enabled = false;
                        break;
                }
            }
            if (isExit == true)
            {
                MessageBox.Show("登出成功");
            }
            Application.Exit();
        }

        /// <summary>
        /// 設定對話框字形顏色
        /// </summary>
        /// <param name="text"></param>
        private delegate void SetFontDelegate(string text);

        private void SetFont(string text)
        {
            if (rtf_MessageInfo.InvokeRequired)
            {
                SetFontDelegate d = new SetFontDelegate(SetFont);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                string[] splitString = text.ToLower().Split(',');
                if (splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Bold);
                }
                if (splitString[5] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Italic);
                }
                if (splitString[6] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Underline);
                }
                if (splitString[6] == "true" && splitString[5] == "true" && splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Underline | FontStyle.Bold | FontStyle.Italic);
                }
                if (splitString[4] == "true" && splitString[5] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Bold | FontStyle.Italic);
                }
                if (splitString[6] == "true" && splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Underline | FontStyle.Bold);
                }
                if (splitString[6] == "true" && splitString[5] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[1], float.Parse(splitString[2]), FontStyle.Underline | FontStyle.Italic);
                }
                string color = splitString[3];
                switch (color)
                {
                    case "黑色":
                        rtf_MessageInfo.SelectionColor = Color.Black;
                        break;
                    case "紅色":
                        rtf_MessageInfo.SelectionColor = Color.Red;
                        break;
                    case "橘色":
                        rtf_MessageInfo.SelectionColor = Color.Orange;
                        break;
                    case "黃色":
                        rtf_MessageInfo.SelectionColor = Color.Yellow;
                        break;
                    case "綠色":
                        rtf_MessageInfo.SelectionColor = Color.Green;
                        break;
                    case "藍色":
                        rtf_MessageInfo.SelectionColor = Color.Blue;
                        break;
                    case "紫色":
                        rtf_MessageInfo.SelectionColor = Color.Purple;
                        break;
                }
            }
        }
       
        
        void cancelGame(bool isclick) 
        {
            if (!isclick)
            {
                AsyncSendMessage("Leave the game," + textBox1.Text);
                AsyncSendMessage("turnto," + max);
                isfinish = false;
            }
            else 
            {
                AsyncSendMessage("Gameisover,");
            }    
                          
        }

        void checkfinish(bool isfinish,string input)
        {
            this.isfinish = isfinish;
            AsyncSendMessage("turnto," +input); ///input 輸入值
        }

        public void chatting(string message)
        {
            AsyncSendMessage( message); ///input 輸入值
        }


        /// <summary>
        /// 發送資訊狀態的資料結構
        /// </summary>
        private struct SendMessageStates
        {
            public SendMessageDelegate d;
            public IAsyncResult result;
        }

        /// <summary>
        /// 非同步向伺服器發送資料
        /// </summary>
        /// <param name="message"></param>
        public void AsyncSendMessage(string message)
        {
            SendMessageDelegate d = new SendMessageDelegate(SendMessage);
            IAsyncResult result = d.BeginInvoke(message, null, null);
            while (!result.IsCompleted)
            {
                if (isExit)
                    return;
                Thread.Sleep(50);
            }
            SendMessageStates states = new SendMessageStates();
            states.d = d;
            states.result = result;
            Thread t = new Thread(FinishAsyncSendMessage);
            t.IsBackground = true;
            t.Start(states);
        }

        /// <summary>
        /// 處理接收的服務端資料
        /// </summary>
        /// <param name="obj"></param>
        private void FinishAsyncSendMessage(object obj)
        {
            SendMessageStates states = (SendMessageStates)obj;
            states.d.EndInvoke(states.result);
        }

        private delegate void SendMessageDelegate(string message);
        /// <summary>
        /// 向服務端發送資料
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(string message)
        {
            try
            {
                bw.Write(message);
                bw.Flush();
            }
            catch
            {
                AddStatus("發送失敗");
            }
        }

        delegate void ConnectServerDelegate();
        /// <summary>
        /// 連接伺服器
        /// </summary>
        private void ConnectServer()
        {
            client = new TcpClient(serverIP, 8889);
        }

        delegate void ReceiveMessageDelegate(out string receiveMessage);
        /// <summary>
        /// 讀取伺服器發過來的資訊
        /// </summary>
        /// <param name="receiveMessage"></param>
        private void receiveMessage(out string receiveMessage)
        {
            receiveMessage = null;
            try
            {
                receiveMessage = br.ReadString();
            }
            catch (Exception ex)
            {
                AddStatus(ex.Message);
            }
        }

        private delegate void AddTalkMessageDelegate(string message);
        /// <summary>
        /// 向 rtf 中添加聊天記錄
        /// </summary>
        /// <param name="message"></param>
        private void AddTalkMessage(string message)
        {
            if (rtf_MessageInfo.InvokeRequired)
            {
                AddTalkMessageDelegate d = new AddTalkMessageDelegate(AddTalkMessage);
                rtf_MessageInfo.Invoke(d, new object[] { message });
            }
            else
            {
                rtf_MessageInfo.AppendText(message);
                rtf_MessageInfo.ScrollToCaret();
            }
        }

        private delegate void AddStatusDelegate(string message);
        /// <summary>
        /// 向 rtf 中添加狀態資訊
        /// </summary>
        /// <param name="message"></param>
        private void AddStatus(string message)
        {
            if (rtf_StatusInfo.InvokeRequired)
            {
                AddStatusDelegate d = new AddStatusDelegate(AddStatus);
                rtf_StatusInfo.Invoke(d, new object[] { message });
            }
            else
            {
                try
                {
                    rtf_StatusInfo.AppendText(message);
                }
                catch (Exception ex){
                       MessageBox.Show(ex.Message);
                }
            }
        }

        private delegate void AddOnlineDelegate(string message);
        /// <summary>
        /// 在 lst_OnlineUser 添加線上用戶
        /// </summary>
        /// <param name="message"></param>
        private void AddOnline(string message)
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                AddOnlineDelegate d = new AddOnlineDelegate(AddOnline);
                lst_OnlineUser.Invoke(d, new object[] { message });
            }
            else
            {
                lst_OnlineUser.Items.Add(message);
                lst_OnlineUser.SelectedIndex = lst_OnlineUser.Items.Count - 1;
                lst_OnlineUser.ClearSelected();
            }
        }

        private delegate void RemoveUserNameDelegate(string userName);
        /// <summary>
        /// 從 listBoxOnline 刪除離線用戶
        /// </summary>
        /// <param name="userName"></param>
        private void RemoveUserName(string userName)
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                RemoveUserNameDelegate d = RemoveUserName;
                lst_OnlineUser.Invoke(d, userName);
            }
            else
            {
                lst_OnlineUser.Items.Remove(userName);
                lst_OnlineUser.SelectedIndex = lst_OnlineUser.Items.Count - 1;
                lst_OnlineUser.ClearSelected();
            }
        }

        private void FormClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null)
            {
                AsyncSendMessage("Logout," + textBox1.Text);
                AsyncSendMessage("Leave the game," + textBox1.Text);
                isExit = true;
                br.Close();
                bw.Close();
                client.Close();
            }
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            AddStatus("開始連接.");
            connectWork.RunWorkerAsync();
        }

        
        private void btn_SendeMessage_Click_1(object sender, EventArgs e)
        {
            if (isSignIn == true)
            {
                string format = toolStripComboBox1.Text + "," + toolStripComboBox2.Text + "," + toolStripComboBox3.Text + "," + toolStripButton1.Checked + "," + toolStripButton2.Checked + "," + toolStripButton3.Checked;

                if ((lst_OnlineUser.SelectedIndex > 0) && (lst_OnlineUser.Text != textBox1.Text))
                {
                    AddTalkMessage("［悄悄話］你對" + lst_OnlineUser.SelectedItem + "說：\r\n");
                    SetFont("Format," + format);
                    AddTalkMessage(rtf_SendMessage.Text + "\r\n");
                    AsyncSendMessage("Talk," + lst_OnlineUser.SelectedItem + ",［悄悄話］" + textBox1.Text + "對你說：\r\n");
                    AsyncSendMessage("Format," + lst_OnlineUser.SelectedItem + "," + format);
                    AsyncSendMessage("Talk," + lst_OnlineUser.SelectedItem + "," + rtf_SendMessage.Text + "\r\n");
                    rtf_SendMessage.Clear();
                }
                else
                {
                    AsyncSendMessage("TalkAll," + textBox1.Text + "," + textBox1.Text + "說：\r\n");
                    AsyncSendMessage("FormatAll," + format);
                    AsyncSendMessage("TalkAll," + textBox1.Text + "," + rtf_SendMessage.Text + "\r\n");
                    rtf_SendMessage.Clear();
                }
            }
            else
            {
                MessageBox.Show("請先登入");

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (client != null)
            {
                AsyncSendMessage("Logout," + textBox1.Text);
                isExit = true;
                isSignIn = false;
                br.Close();
                bw.Close();
                client.Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {       
            AsyncSendMessage("Start the game," + textBox1.Text);
            button2.Enabled = false;
            button3.Enabled = false;     

            while (true)      
            {   
                if (MessageBox.Show("即將開始終極密碼...", "Waiting", MessageBoxButtons.RetryCancel) == DialogResult.Cancel)       
                {
                    if (gameisOpen) 
                    {
                        AsyncSendMessage("Playing," + textBox1.Text);
                        button3.Enabled = false;
                        button4.Enabled = true;
                        break;
                    }
                }
                   
                else                  
                {
                    AsyncSendMessage("Cancel," + textBox1.Text);               
                    button2.Enabled = true;            
                    button3.Enabled = true;              
                    break;            
                }
                    
                    
            }
       }
        /// <summary>
        /// 除掉MessageBox
        /// </summary>
        private void killMessageBox()
        {
            IntPtr ptr = FindWindow(null, "Waiting");
            if (ptr != IntPtr.Zero)
            {
                PostMessage(ptr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AsyncSendMessage("Leave the game," + textBox1.Text);
            gameisOpen = false;
            isfinish = false;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = false;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            B++;
            if (B % 2 == 1)
            {
                toolStripButton1.Checked = true;
            }
            else
            {
                toolStripButton1.Checked = false;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            I++;
            if (I % 2 == 1)
            {
                toolStripButton2.Checked = true;
            }
            else
            {
                toolStripButton2.Checked = false;
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            U++;
            if (U % 2 == 1)
            {
                toolStripButton3.Checked = true;
            }
            else
            {
                toolStripButton3.Checked = false;
            }
        }

        private void lst_OnlineUser_DoubleClick(object sender, EventArgs e)
        {
            if (lst_OnlineUser.Text != textBox1.Text && lst_OnlineUser.SelectedIndex > 0)
            {
                
                if (CR == null)
                {
                    CR = new ChattingRom();
                    CR.Text = lst_OnlineUser.SelectedItem.ToString();
                    CR.talker = "" + lst_OnlineUser.SelectedItem + ",";
                    CR.user = textBox1.Text;
                    CR.chatting += new Chatting(chatting);
                    CR.Show();
                }
                else
                {
                    CR.Text = lst_OnlineUser.SelectedItem.ToString();
                    CR.talker = "" + lst_OnlineUser.SelectedItem + ",";
                    CR.user = textBox1.Text;
             //       CR.chatting += new Chatting(chatting);
                }
            }
        }

    }
}
