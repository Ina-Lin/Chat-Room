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
    public delegate void CheckFinish(bool topmost,string s);
    public delegate void CancelGame(bool isclick);  
    public partial class Game : Form
    {
        public event CheckFinish finish;
        public event CancelGame leave;
        int keyNum;
        int  Max;
        int  Min;
        string name;
        bool buttonisclick=false;

        public Game(string keyNum,string Min,string Max,string name )
        {
            InitializeComponent();
            
            this.keyNum = int.Parse(keyNum);
            this.Max = int.Parse(Max);
            this.Min = int.Parse(Min);
            this.name = name;
            textBox2.Text = Min;
            textBox4.Text = Max;
        }

        private void button1_Click(object sender, EventArgs e)
        {
              

            if (checkNum(textBox1.Text) && int.Parse(textBox1.Text) >=Min && int.Parse(textBox1.Text) <= Max)
            {
                buttonisclick = true;

                if(textBox1.Text==keyNum.ToString())
                {
                    MessageBox.Show("You dIe");
                    leave(buttonisclick);
                    
                    this.Close();
                }
                else
                {
                    finish(buttonisclick, textBox1.Text);//執行委託實例
                    this.Close();
                }

            }
            else
            {
                MessageBox.Show(name+"請輸入合法數字");
            }
        }

        private bool checkNum(string s)
        {
            Char[] x = s.ToCharArray();
            for (int j = 0; j < s.Length; j++)
            {
                if (x[j] < '0' || x[j] > '9')
                {
                    return false;
                }
            }
            return true;
        }

        private void Game_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!buttonisclick)
            {
                if (MessageBox.Show(name + "您確定要離開嗎?", "EXIT", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else
                {
                    leave(buttonisclick);
                }
            }
            buttonisclick = false;
        }
    }
}
