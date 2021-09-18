using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoginClient
{
    public partial class LoginClientForm : Form
    {
        private PipeClient pipeClient;

        public LoginClientForm()
        {
            InitializeComponent();
        }

        private void LoginClientForm_Load(object sender, EventArgs e)
        {
            CreateNewPipeClient();
            NeedToConnect();
        }

        private void CreateNewPipeClient()
        {
            if (pipeClient != null)
            {
                pipeClient.MessageRecieved -= pipeClient_MessageRecieved;
                pipeClient.ServerDisconnected -= pipeClient_ServerDisconnected;
                pipeClient.ConnectedToServer -= pipeClient_ConnectedToServer;
            }

            pipeClient = new PipeClient();
            pipeClient.MessageRecieved += pipeClient_MessageRecieved;
            pipeClient.ServerDisconnected += pipeClient_ServerDisconnected;
            pipeClient.ConnectedToServer += pipeClient_ConnectedToServer;
        }

        private void pipeClient_ConnectedToServer()
        {
            Invoke(new PipeClient.ConnectedToServerHandler(ConnectedToServer));
        }

        private void pipeClient_MessageRecieved(byte[] message)
        {
            Invoke(new PipeClient.MessageRecievedHandler(DisplayRecievedMessage),
                new object[] { message });
        }

        private void DisplayRecievedMessage(byte[] message)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string str = encoder.GetString(message, 0, message.Length);

            if (str == "close") //|| str == "Username or password incorrect.")
            {
                MessageLogTB.Text += str + "\r\n";

                pipeClient.Disconnect();

                CreateNewPipeClient();
                //pipeClient.Connect(PipeNameTB.Text);
            }

            MessageLogTB.Text += str + "\r\n";
        }

        private void pipeClient_ServerDisconnected()
        {
            Invoke(new PipeClient.ServerDisconnectedHandler(NeedToConnect));
        }

         private void NeedToConnect()
        {
            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;

            SendBtn.Enabled = false;
            SendMessageTB.Enabled = false;
            ClearBtn.Enabled = false;

            AdminUsernameTB.Focus();
        }

        private void ConnectedToServer()
        {
            ConnectBtn.Enabled = false;
            DisconnectBtn.Enabled = true;

            SendBtn.Enabled = true;
            SendMessageTB.Enabled = true;
            ClearBtn.Enabled = true;

            SendMessageTB.Focus();
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrWhiteSpace(AdminUsernameTB.Text)
                && String.IsNullOrWhiteSpace(AdminPasswordTB.Text)))
            {
                pipeClient.Connect(PipeNameTB.Text);

                if (pipeClient.IsConnected())
                {
                    ValidateAdminDetails(AdminUsernameTB.Text, AdminPasswordTB.Text);
                }

            } else
            {
                MessageBox.Show("Connection requires the admin username and password.", "Login Error");
            }
            
        }

        private void ValidateAdminDetails(string adminUsername, string adminPassword)
        {
            string adminDetails = adminUsername + ',' + adminPassword;
            ASCIIEncoding encoder = new ASCIIEncoding();

            pipeClient.SendMessage(encoder.GetBytes(adminDetails));


        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            pipeClient.SendMessage(encoder.GetBytes(SendMessageTB.Text));
            SendMessageTB.Clear();
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            pipeClient.Disconnect();
            NeedToConnect();
            CreateNewPipeClient();
        }

        private void ClearBtn_Click(object sender, EventArgs e)
        {
            MessageLogTB.Clear();
            SendMessageTB.Focus();
        }
    }
}
