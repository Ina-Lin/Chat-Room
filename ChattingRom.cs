using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public delegate void Chatting(string s);
    public partial class ChattingRom : Form
    {
        public event Chatting chatting;
        public string str = "";
        public string talker = "";
        public string user = "";
        public ChattingRom()
        {
            InitializeComponent();
        }

        private void btn_SendeMessage_Click(object sender, EventArgs e)
        {
            str = toolStripComboBox1.Text + "," + toolStripComboBox2.Text + "," + toolStripComboBox3.Text + "," + toolStripButton1.Checked + "," + toolStripButton2.Checked + "," + toolStripButton3.Checked;
            rtf_MessageInfo.AppendText("你說：\r\n");
            SetFont(str);
            rtf_MessageInfo.AppendText(rtf_SendMessage.Text + "\r\n");
            chatting("TalkOne," + talker  + user + ",說：\r\n");
            chatting("FormatOne," + talker  + str);
            chatting("TalkOne," + talker + rtf_SendMessage.Text + ",\r\n");
            rtf_SendMessage.Clear();
        }

        public delegate void SetTextDelegate(string name);
        /// <summary>
        /// 向 rtf 中添加聊天記錄
        /// </summary>
        /// <param name="message"></param>
        public void SetText(string name)
        {
            if (this.rtf_MessageInfo.InvokeRequired)
            {
                SetTextDelegate d = new SetTextDelegate(SetText);
                this.Invoke(d, new object[] { name });
            }
            else
            {
                this.Text=name;
            }
        }

        public delegate void AddTalkMessageDelegate(string message);
        /// <summary>
        /// 向 rtf 中添加聊天記錄
        /// </summary>
        /// <param name="message"></param>
        public void AddTalkMessage(string message)
        {
            if (this.rtf_MessageInfo.InvokeRequired)
            {
                AddTalkMessageDelegate d = new AddTalkMessageDelegate(AddTalkMessage);
                this.rtf_MessageInfo.Invoke(d, new object[] { message });
            }
            else
            {
                rtf_MessageInfo.AppendText(message);
                rtf_MessageInfo.ScrollToCaret();
            }
        }


        private delegate void SetFontDelegate(string text);
        /// <summary>
        /// 設定對話框文字顏色
        /// </summary>
        /// <param name="text"></param>
        public void SetFont(string text)
        {
            if (rtf_MessageInfo.InvokeRequired)
            {
                SetFontDelegate d = new SetFontDelegate(SetFont);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                string[] splitString = text.ToLower().Split(',');
                if (splitString[3] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Bold);
                }
                if (splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Italic);
                }
                if (splitString[5] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Underline);
                }
                if (splitString[5] == "true" && splitString[4] == "true" && splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Underline | FontStyle.Bold | FontStyle.Italic);
                }
                if (splitString[3] == "true" && splitString[4] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Bold | FontStyle.Italic);
                }
                if (splitString[5] == "true" && splitString[3] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Underline | FontStyle.Bold);
                }
                if (splitString[5] == "true" && splitString[3] == "true")
                {
                    rtf_MessageInfo.SelectionFont = new Font(splitString[0], float.Parse(splitString[1]), FontStyle.Underline | FontStyle.Italic);
                }
                string color = splitString[2];
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
    }
}
