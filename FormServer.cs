using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatSever
{
    public partial class FormServer : Form
    {
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.listBox2 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listBox1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(442, 282);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "狀態信息";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(7, 22);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(429, 244);
            this.listBox1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(97, 316);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "開始監看";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(290, 316);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "停止監看";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // listBox2
            // 
            this.listBox2.FormattingEnabled = true;
            this.listBox2.ItemHeight = 12;
            this.listBox2.Location = new System.Drawing.Point(479, 34);
            this.listBox2.Name = "listBox2";
            this.listBox2.Size = new System.Drawing.Size(95, 244);
            this.listBox2.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(477, 281);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 12);
            this.label1.TabIndex = 4;
            this.label1.Text = "GaMe WiLl bE";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(551, 281);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "wAiTInG";
            // 
            // FormServer
            // 
            this.ClientSize = new System.Drawing.Size(668, 363);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox1);
            this.Name = "FormServer";
            this.Text = "Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormServer_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        
        public FormServer()
        {
            InitializeComponent();
            listBox1.HorizontalScrollbar = true;
            button2.Enabled = false;
        }

        /// <summary>
        /// 保存連接的所有用戶
        /// </summary>
        private List<User> userList = new List<User>();
        /// <summary>
        /// 使用的本機IP位址
        /// </summary>
        IPAddress localAddress = IPAddress.Any;
        /// <summary>
        /// 監聽埠
        /// </summary>
        private const int port = 8889;
        private TcpListener myListener;
        /// <summary>
        /// 是否正常退出所有接收執行緒
        /// </summary>
        bool isExit = false;
        private delegate void myUICallBack(string myStr, Control ctl);
        private int person = 0;
        private int MIN = 0;
        private int MAX = 100;


        

        /// <summary>
        /// 監聽用戶端請求
        /// </summary>
        private void ListenClientConnect()
        {
            TcpClient newClient = null;
            while (true)
            {
                ListenClientDelegate d = new ListenClientDelegate(ListenClient);
                IAsyncResult result = d.BeginInvoke(out newClient, null, null);
                //使用輪詢方式來判斷非同步作業是否完成
                while (result.IsCompleted == false)
                {
                    if (isExit)
                        break;
                    Thread.Sleep(250);
                }
                //獲取Begin 方法的返回值和所有輸入/輸出參數
                d.EndInvoke(out newClient, result);
                if (newClient != null)
                {
                    //每接受一個用戶端連接，就創建一個對應的執行緒迴圈接收該用戶端發來的資訊
                    User user = new User(newClient);
                    Thread threadReceive = new Thread(ReceiveData);
                    threadReceive.Start(user);
                    userList.Add(user);
                    AddItemToListBox(string.Format("[{0}]進入", newClient.Client.RemoteEndPoint));
                    AddItemToListBox(string.Format("當前連接用戶數：{0}", userList.Count));
                }
                else
                {
                    break;
                }
            }
        }

        private void ReceiveData(object userState)
        {
            User user = (User)userState;
            TcpClient client = user.client;
            while (!isExit)
            {
                string receiveString = null;
                ReceiveMessageDelegate d = new ReceiveMessageDelegate(ReceiveMessage);
                IAsyncResult result = d.BeginInvoke(user, out receiveString, null, null);
                //使用輪詢方式來判斷非同步作業是否完成
                while (!result.IsCompleted)
                {
                    if (isExit)
                        break;
                    Thread.Sleep(250);
                }
                //獲取Begin方法的返回值和所有輸入/輸出參數
                d.EndInvoke(out receiveString, result);
                if (receiveString == null)
                {
                    if (!isExit)
                    {
                        AddItemToListBox(string.Format("與{0}失去聯繫，已終止接收該使用者資訊", client.Client.RemoteEndPoint));
                        RemoveUser(user);
                    }
                    break;
                }
                AddItemToListBox(string.Format("來自[{0}]:{1}", user.client.Client.RemoteEndPoint, receiveString));
                string[] splitString = receiveString.Split(',');
                switch (splitString[0])
                {
                    case "Login":
                        user.userName = splitString[1];
                        AsyncSendToAllClient(user, receiveString);
                        break;
                    case "Logout":
                        AsyncSendToAllClient(user, receiveString);
                        RemoveUser(user);
                        return;
                    case "Talk":    //格式=代號,收信,訊息
                        string talkString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                        AddItemToListBox(string.Format("{0}對{1}說：{2}", user.userName, splitString[1], talkString));
                        foreach (User target in userList)
                        {
                            if (target.userName == splitString[1])
                            {
                                AsyncSendToClient(target, "talk," + talkString);
                                break;
                            }
                        }
                        break;
                    case "TalkOne":
                        string talkOneString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                        foreach (User target in userList)
                        {
                            if (target.userName == splitString[1])
                            {
                                AsyncSendToClient(target, "talkone,"  + talkOneString);
                                break;
                            }
                        }
                        break;
                    case "FormatOne":
                        string formOneString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                        foreach (User target in userList)
                        {
                            if (target.userName == splitString[1])
                            {
                                AsyncSendToClient(target, "formatone," + formOneString);
                                break;
                            }
                        }
                        break;                    
                    case "TalkAll":   //格式= 代號,寄信,訊息
                        string talkAllString = receiveString.Substring(splitString[0].Length + 1);
                        AddItemToListBox(string.Format("{0}說：{1}", user.userName, talkAllString));
                        AsyncSendToAllClient(user, "talkall," + talkAllString);
                        break;
                    case "Format":   //格式=代號,收件,format
                        string formString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                        foreach (User target in userList)
                        {
                            if (target.userName == splitString[1])
                            {
                                AsyncSendToClient(target, "format," + formString);
                                break;
                            }
                        }
                        break;
                    case "FormatAll":  //格式=代號,format
                        string formatAllString = receiveString.Substring(splitString[0].Length + 1);
                        AsyncSendToAllClient(user, "formatall," + formatAllString);
                        break;
                    case "Start the game":
                        user.userName = splitString[1];
                        AddItemToListBox2(string.Format("{0}", user.userName));
                        StartTheGame();
                        break;
                    case "Leave the game":
                        user.userName = splitString[1];
                        RemoveItemToListBox2(user.userName);
                        AddItemToListBox(string.Format("{0} 退出遊戲", user.userName));
                        break;
                    case "Playing":
                        user.userName = splitString[1];
                        AddItemToListBox(string.Format("{0} 進行遊戲中...", user.userName));
                        break;
                    case "Cancel":
                        user.userName = splitString[1];
                        RemoveItemToListBox2(user.userName);
                        break;
                    case "turnto":
                        string  userinput = splitString[1];

                        if (listBox2.Items.Count<= 0) 
                        {
                            string message = "GameisOver,";
                            for (int i = 0; i < userList.Count; i++)
                            {
                                AsyncSendToClient(userList[i], message);
                                person = 0;
                            }
                            break;
                        }

                        else if (person < listBox2.Items.Count-1)
                        {
                            this.person = person + 1;
                        }
                        else
                        {
                            this.person = 0;
                        }
                        round(userinput);
                        break;
                    
                    case "Gameisover":
                        Form.CheckForIllegalCrossThreadCalls = false;
                        listBox2.Items.Clear();
                        string message2 = "GameisOver,";
                        for (int i = 0; i < userList.Count; i++)
                        {
                            AsyncSendToClient(userList[i], message2);
                        }
                        break;
                }
 
            }
        }

        /// <summary>
        /// 非同步發送資訊給所有客戶
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        private void AsyncSendToAllClient(User user, string message)
        { 
            string command = message.Split(',')[0].ToLower();
            if (command == "login")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    AsyncSendToClient(userList[i], message);
                    if (userList[i].userName != user.userName)
                        AsyncSendToClient(user, "login," + userList[i].userName);
                }
            }
            else if (command == "logout")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    if (userList[i].userName != user.userName)
                        AsyncSendToClient(userList[i], message);
                }
            }

            else if (command == "talkall")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    AsyncSendToClient(userList[i], message);
                }
            }

            else if (command == "formatall")
            {
                for (int i = 0; i < userList.Count; i++)
                {
                    AsyncSendToClient(userList[i], message);
                }
            } 
        }

        /// <summary>
        /// 非同步發送message給user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        private void AsyncSendToClient(User user, string message)
        {
            SendToClientDelegate d = new SendToClientDelegate(SendToClient);
            IAsyncResult result = d.BeginInvoke(user, message, null, null);
            while (result.IsCompleted == false)
            {
                if (isExit)
                    break;
                Thread.Sleep(250);
            }
            d.EndInvoke(result);
        }

        private delegate void SendToClientDelegate(User user, string message);
        /// <summary>
        /// 發送message給user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        private void SendToClient(User user, string message)
        {
            try
            {
                //將字串寫入網路流，此方法會自動附加字串長度首碼
                user.bw.Write(message);
                user.bw.Flush();
                AddItemToListBox(string.Format("向[{0}]發送：{1}", user.userName, message));
            }
            catch
            {
                AddItemToListBox(string.Format("向[{0}]發送資訊失敗", user.userName));
            }
        }

        /// <summary>
        /// 移除用戶
        /// </summary>
        /// <param name="user"></param>
        private void RemoveUser(User user)
        {
            userList.Remove(user);
            user.Close();
            AddItemToListBox(string.Format("當前連接用戶數：{0}", userList.Count));
        }

        delegate void ReceiveMessageDelegate(User user, out string receiveMessage);
        /// <summary>
        /// 接收用戶端發來的資訊
        /// </summary>
        /// <param name="user"></param>
        /// <param name="receiveMessage"></param>
        private void ReceiveMessage(User user, out string receiveMessage)
        {
            try
            {
                receiveMessage = user.br.ReadString();
            }
            catch (Exception ex)
            {
                AddItemToListBox(ex.Message);
                receiveMessage = null;
            }
        }

        private delegate void ListenClientDelegate(out TcpClient client);
        /// <summary>
        /// 接受掛起的用戶端連接請求
        /// </summary>
        /// <param name="newClient"></param>
        private void ListenClient(out TcpClient newClient)
        {
            try
            {
                newClient = myListener.AcceptTcpClient();
            }
            catch
            {
                newClient = null;
            }
        }

        delegate void AddItemToListBoxDelegate(string str);
        /// <summary>
        /// 在ListBox中追加狀態資訊
        /// </summary>
        /// <param name="str">要追加的信息</param>
        private void AddItemToListBox(string str)
        {
            if (listBox1.InvokeRequired)
            {
                AddItemToListBoxDelegate d = AddItemToListBox;
                this.listBox1.Invoke(d, str);
            }
            else
            {
                listBox1.Items.Add(str);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.ClearSelected();
            }
        }
        private void AddItemToListBox2(string str)
        {
            if (listBox2.InvokeRequired)
            {
                AddItemToListBoxDelegate d = AddItemToListBox2;
                listBox2.Invoke(d, str);
            }
            else
            {
                listBox2.Items.Add(str);
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
                listBox2.ClearSelected();
            }
        }
        private void RemoveItemToListBox2(string str)
        {
            if (listBox2.InvokeRequired)
            {
                AddItemToListBoxDelegate d = RemoveItemToListBox2;
                listBox2.Invoke(d, str);
            }
            else
            {
                listBox2.Items.Remove(str);
                listBox2.SelectedIndex = listBox2.Items.Count - 1;
                listBox2.ClearSelected();
            }
        }

        private void FormServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            button2.PerformClick();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myListener = new TcpListener(localAddress, port);
            myListener.Start();
            AddItemToListBox(string.Format("開始在{0}:{1}監聽看用戶端", localAddress, port));
            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AddItemToListBox("停止服務，將使用者依序退出！");
            isExit = true;
            for (int i = userList.Count - 1; i >= 0; i--)
            {
                
                RemoveItemToListBox2(userList[i].ToString());
                RemoveUser(userList[i]);
            }
            //通過停止監聽讓myListener.AcceptTcpClient()產生異常退出監聽執行緒
            myListener.Stop();
            button1.Enabled = true;
            button2.Enabled = false;
        }
      
        private void StartTheGame() 
        {
            if (userList.Count == listBox2.Items.Count)
            {
                myUI("終極密碼為:", label1);
                myUI(rnd(), label2);

                string message = "allready"+","+label2.Text;

                for (int i = 0; i < userList.Count; i++)
                {
                    AsyncSendToClient(userList[i], message);
                }
                MIN = 0;
                MAX = 100;
                message = "startgame" + "," + label2.Text + "," + MAX.ToString() + "," + MIN.ToString() + "," + "false";
                AsyncSendToClient(userList[person], message);    
            }
        }

        private String rnd() 
        {
            Random rnd = new Random();
            int MinValue = 1;
            int MaxValue = 101;
           return rnd.Next(MinValue, MaxValue).ToString();
        }   

        private void myUI(string myStr, Control ctl)
        {
            if (this.InvokeRequired)
            {
                myUICallBack myUpdate = new myUICallBack(myUI);
                this.Invoke(myUpdate, myStr, ctl);
            }
            else
            {
                ctl.Text = myStr;
            }
        }

        private void round(string inputnum) 
        {
            if (int.Parse(inputnum) > int.Parse(label2.Text)) 
            {
                MAX = int.Parse(inputnum);
            }
            else
            {
                MIN = int.Parse(inputnum);
            }
            Form.CheckForIllegalCrossThreadCalls = false;
            string message = "startgame" + "," + label2.Text+","+MAX+","+MIN+","+"true";
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i].userName.Equals(listBox2.Items[person].ToString())) 
                {
                    AsyncSendToClient(userList[i], message);  
                }
            } 
        }
    }
}
