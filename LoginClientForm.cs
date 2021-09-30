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

        /// <summary>
        /// This method sets up the program when it first loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginClientForm_Load(object sender, EventArgs e)
        {
            CreateNewPipeClient();
            UpdateFormButtons();
            LoginBtn.Enabled = false;
        }

        /// <summary>
        /// Create a new pipeClient and set all the handlers for the delegates.
        /// </summary>
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

        /// <summary>
        /// Methods to allow the pipeClient to use the methods on the form.
        /// </summary>
        #region HandlerMethods

        private void pipeClient_ConnectedToServer()
        {
            Invoke(new PipeClient.ConnectedToServerHandler(UpdateFormButtons));
        }

        private void pipeClient_MessageRecieved(byte[] message)
        {
            Invoke(new PipeClient.MessageRecievedHandler(DisplayRecievedMessage),
                new object[] { message });
        }

        private void pipeClient_ServerDisconnected()
        {
            Invoke(new PipeClient.ServerDisconnectedHandler(UpdateFormButtons));
        }
        #endregion

        /// <summary>
        /// Add the message onto a newline of the message log.
        /// </summary>
        /// <param name="message"></param>
        private void DisplayRecievedMessage(byte[] message)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string str = encoder.GetString(message, 0, message.Length);

            if (str == "close")
            {
                MessageLogTB.Text += str + "\r\n";
                pipeClient.Disconnect();
                CreateNewPipeClient();
            }
            MessageLogTB.Text += str + "\r\n";
        }

        /// <summary>
        /// Update the enabled property of the buttons on the form.
        /// Then resets the focus.
        /// </summary>
        private void UpdateFormButtons()
        {
            bool connected = pipeClient.IsConnected();
            bool loggedIn = pipeClient.IsLoggedIn();

            ConnectBtn.Enabled = !connected;
            DisconnectBtn.Enabled = connected;

            LoginBtn.Enabled = !loggedIn;
            SendBtn.Enabled = loggedIn;
            SendMessageTB.Enabled = loggedIn;
            ClearBtn.Enabled = loggedIn;

            if (!connected)
            {
                ConnectBtn.Focus();
            }
            else if (!loggedIn)
            {
                AdminUsernameTB.Focus();
            }
            else
            {
                SendMessageTB.Focus();
            }
        }

        
        #region ButtonClickMethods

        /// <summary>
        /// Attempts to connect to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            pipeClient.Connect(PipeNameTB.Text);

            UpdateFormButtons();
        }

        /// <summary>
        /// Sends the user text to the server and clears the send message box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBtn_Click(object sender, EventArgs e)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();

            if (!String.IsNullOrWhiteSpace(SendMessageTB.Text))
            {
                pipeClient.SendMessage(encoder.GetBytes(SendMessageTB.Text));
                SendMessageTB.Clear();
            }
        }

        /// <summary>
        /// When the disconnect button is clicked the pipeclient is dicconected, 
        /// the form is updated and a new pipeclient is created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            pipeClient.Disconnect();
            UpdateFormButtons();
            CreateNewPipeClient();
        }

        /// <summary>
        /// Clears the Message log and focuses onto the send box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearBtn_Click(object sender, EventArgs e)
        {
            MessageLogTB.Clear();
            SendMessageTB.Focus();
        }

        /// <summary>
        /// The method called when the Login button is called.
        /// If the username and password text boxes are filled out they are sent to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginBtn_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrWhiteSpace(AdminUsernameTB.Text)
                && String.IsNullOrWhiteSpace(AdminPasswordTB.Text)))
            {
                //pipeClient.Connect(PipeNameTB.Text);
                //ValidateAdminDetails(AdminUsernameTB.Text, AdminPasswordTB.Text);

                string adminDetails = AdminUsernameTB.Text + ',' + AdminPasswordTB.Text;
                ASCIIEncoding encoder = new ASCIIEncoding();

                pipeClient.SendMessage(encoder.GetBytes(adminDetails));
            }
            else
            {
                MessageBox.Show("Connection requires the admin username and password.", "Login Error");
            }
        }

        #endregion
    }
}
