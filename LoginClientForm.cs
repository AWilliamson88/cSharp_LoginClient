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
        }

        private void CreateNewPipeClient()
        {
            if (pipeClient != null)
            {
                pipeClient.MessageRecieved -= pipeClient_MessageRecieved;
                pipeClient.ServerDisconnected -= pipeClient_ServerDisconnected;
            }

            pipeClient = new PipeClient();
            pipeClient.MessageRecieved += pipeClient_MessageRecieved;
            pipeClient.ServerDisconnected += pipeClient_ServerDisconnected;
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

            if (str == "close")
            {
                pipeClient.Disconnect();

                CreateNewPipeClient();
                pipeClient.Connect(PipeNameTB.Text);
            }

            MessageLogTB.Text += str + "\r\n";
        }

        private void pipeClient_ServerDisconnected()
        {
            Invoke(new PipeClient.ServerDisconnectedHandler(EnableStartButton));
        }
         private void EnableStartButton()
        {
            ConnectBtn.Enabled = true;
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (AdminUsernameTB.Text != "" && AdminPasswordTB.Text != "")
            {
                pipeClient.Connect(PipeNameTB.Text);

                if (pipeClient.IsConnected())
                {
                    ConnectBtn.Enabled = false;
                    pipeClient.ValidateAdminDetails(AdminUsernameTB.Text, AdminPasswordTB.Text);
                    
                }

            } else
            {
                MessageBox.Show("Connection requires the admin username and password.", "Login Error");
            }
            
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            pipeClient.SendMessage(encoder.GetBytes(SendMessageTB.Text));
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            pipeClient.Disconnect();
            EnableStartButton();
        }
    }
}
