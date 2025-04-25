using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EtherChat_Client
{
    public partial class Form1 : Form
    {
        private Socket socketSend;
        private bool isConnected = false;
        private string username;

        public Form1()
        {
            InitializeComponent();
            webBrowser1.DocumentText = "<html><body style='font-family: Segoe UI; background-color: #f5f6fa;'></body></html>";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Settings1.Default.SERVERIP;
            textBox2.Text = Settings1.Default.PORT;
            textBox3.Text = Settings1.Default.USERNAME;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (isConnected) return;
            if (string.IsNullOrWhiteSpace(textBox3.Text))
            {
                MessageBox.Show("请输入用户名！", "EtherChat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            username = textBox3.Text;

            socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            try
            {
                socketSend.Connect(endPoint);
                byte[] buffer = Encoding.UTF8.GetBytes("ServerHello");
                socketSend.Send(buffer);
                isConnected = true;
                textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = button1.Enabled = false;
                Thread thread = new Thread(Receive);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接失败：" + ex.Message, "EtherChat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Disconnect();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!isConnected) return;
            if (string.IsNullOrWhiteSpace(textBox4.Text)) return;

            try
            {
                string message = $"{username}|{textBox4.Text}";
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                socketSend.Send(buffer);
                textBox4.Clear();
            }
            catch (Exception ex)
            {
                UpdateWebBrowser("EtherChat", $"发送失败：{ex.Message}", false);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
            Settings1.Default.SERVERIP = textBox1.Text;
            Settings1.Default.PORT = textBox2.Text;
            Settings1.Default.USERNAME = textBox3.Text;
            Settings1.Default.Save();
        }

        void Receive()
        {
            try
            {
                while (isConnected)
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    int r = socketSend.Receive(buffer);
                    if (r == 0) break;
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    if (str == "ClientHello")
                    {
                        UpdateWebBrowser("EtherChat", "已成功连接服务器！", false);
                    }
                    else
                    {
                        string[] parts = str.Split(new[] { '|' }, 2);
                        if (parts.Length == 2)
                        {
                            bool isSelf = parts[0] == username;
                            UpdateWebBrowser(parts[0], parts[1], isSelf);
                        }
                    }
                }
            }
            catch
            {
                UpdateWebBrowser("EtherChat", "连接已断开！", false);
            }
            finally
            {
                Disconnect();
            }
        }

        private void UpdateWebBrowser(string username, string content, bool isSelf)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string userColor = isSelf ? "#2980b9" : "#c0392b";
            string htmlContent = WebUtility.HtmlEncode(content).Replace("\n", "<br>");

            string html = $@"
            <div style='margin: 10px 0; border-bottom: 1px solid #ecf0f1; padding-bottom: 5px;'>
                <div style='display: flex; align-items: center;'>
                    <strong style='color: {userColor}; font-size: 13px;'>{username}</strong>
                    <span style='color: #7f8c8d; font-size: 11px; margin-left: 8px;'>{time}</span>
                </div>
                <div style='margin: 5px 0 0 20px; color: #2c3e50; font-size: 14px;'>
                    {htmlContent}
                </div>
            </div>";

            if (webBrowser1.InvokeRequired)
            {
                webBrowser1.Invoke(new Action<string, string, bool>(UpdateWebBrowser), username, content, isSelf);
            }
            else
            {
                webBrowser1.Document?.Write(html);
                webBrowser1.ScrollToEnd();
            }
        }

        private void Disconnect()
        {
            if (socketSend != null && socketSend.Connected)
            {
                socketSend.Shutdown(SocketShutdown.Both);
                socketSend.Close();
            }
            isConnected = false;
            textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = button1.Enabled = true;
        }
    }

    public static class WebBrowserExtensions
    {
        public static void ScrollToEnd(this WebBrowser webBrowser)
        {
            if (webBrowser.Document?.Body != null)
            {
                webBrowser.Document.Window.ScrollTo(0, webBrowser.Document.Body.ScrollRectangle.Height);
            }
        }
    }
}