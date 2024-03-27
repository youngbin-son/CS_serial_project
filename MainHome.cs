using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using Tses01.Properties;

namespace Tses01
{
    public partial class MainHome : Form
    {
        SqlConnection sqlConn = new SqlConnection();
        SqlCommand sqlCommand = new SqlCommand();

        public MainHome()
        {
            InitializeComponent();
        }

        delegate void AddTextCB(string text, int n);

        void AddText(string text, int op = 0)
        {
            if (tbTerminal.InvokeRequired)
            {
                AddTextCB cb = new AddTextCB(AddText);
                object[] obj = { text, op };
                Invoke(cb, obj);
            }
            else
            {
                tbTerminal.Text += text;
                if (op == 0)
                    tbTerminal.Text += "\r\n";
            }
        }

        delegate void AddChartCB(string text, int n); // 직렬 통신을 통해 들어온 데이터를 차트에 표시 하기 위한 대리자

        void AddChart(string _name, int _val)
        {
            if(chart1.InvokeRequired)
            {
                AddChartCB cb = new AddChartCB(AddChart);
                object[] objects = { _name, _val };
                Invoke(cb, objects);
            }
            else
            {
                chart1.Series[_name].Points.Add(_val); // 데이터를 차트에 표시
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void mnuSearch_Click(object sender, EventArgs e) // 검색 버튼 누름
        {
            frmSearch frm = new frmSearch();
            frm.ShowDialog();
            string s1 = frm.tbFind.Text;
            tbTerminal.Select(tbTerminal.Text.IndexOf(s1), s1.Length);
        }

        private void mnuChange_Click(object sender, EventArgs e) // 찾기 버튼 누름
        {
            try
            {
                Change frm = new Change();
                frm.ShowDialog();
                string s1 = frm.tbW1.Text;
                string s2 = frm.tbW2.Text;
                tbTerminal.Text = tbTerminal.Text.Replace(s1, s2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void mnuConnet_Click(object sender, EventArgs e)
        {
            Config frm = new Config();

            if(frm.ShowDialog() == DialogResult.OK)
            {
                serialPort2.Parity = (Parity)frm.cbParity.SelectedIndex;
                serialPort2.DataBits = int.Parse(frm.cbData.Text);
                serialPort2.StopBits = (StopBits)frm.cbStop.SelectedIndex;
                serialPort2.BaudRate = int.Parse(frm.cbBaud.Text);
                serialPort2.PortName = frm.cbCom.Text;

                serialPort2.Open();

                if(serialPort2.IsOpen)
                {
                    string strComm = $"{frm.cbCom.Text}:{frm.cbBaud.Text}{frm.cbParity.Text[0]}";
                    strComm += $"{frm.cbData.Text}{frm.cbStop.SelectedIndex}";
                    AddText($"Communication String {strComm}");
                }
                else 
                    AddText($"can not\r\n");
            }
            

        }

        int cnt = 0;

        string[] names = new string[3] {  "Hum", "Temp", "CDS" }; // 차트 항목의 배열

        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e) // 직렬 통신을 통해 데이터가 들어온 경우 실행
        {
            string str = serialPort2.ReadLine(); // str.Split()[0], [1] 습도, [2] 온도 [3] 조도
            
            cnt++;

            if(cnt >= 4)
            {
                AddText(str);

                Console.WriteLine($"INSERT INTO Ditto VALUES ({str.Split()[1]},{str.Split()[2]},{str.Split()[3]})");

                sqlCommand.CommandText = $"INSERT INTO Ditto VALUES ({str.Split()[1]},{str.Split()[2]},{str.Split()[3]})";

                sqlCommand.ExecuteNonQuery();

                for (int i = 0; i < names.Length; i++)
                {
                    if(i != 2)
                    {
                        AddChart(names[i], int.Parse(str.Split()[i + 1]));
                    }
                    else
                    {
                        AddChart(names[i], int.Parse(str.Split()[i + 1])/100);
                    }
                }      

                cnt = 0;
            }

            
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tbTerminal.Clear();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About frm = new About();
            frm.ShowDialog();
        }

        private void tbTerminal_TextChanged(object sender, EventArgs e)
        {

        }

        private void MainHome_Load(object sender, EventArgs e) // 첫 화면이 로드되는 순간
        {
            if(sqlConn.State == ConnectionState.Closed) // 데이터 베이스와 연결이 끈어진 경우에 재 연결
            {
                sqlConn.ConnectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Harman.DESKTOP-VQQPT2C\\Desktop\\BakDo\\Tses01\\Resources\\Ditto_Database.mdf;Integrated Security=True;Connect Timeout=30";
                sqlConn.Open();
                sqlCommand.Connection = sqlConn;
            }
        }

        private void 종료ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            frmExit bye = new frmExit();

            bye.ShowDialog();
        }
    }
}
