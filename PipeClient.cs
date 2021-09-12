﻿using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace LoginClient
{
    /// <summary>
    /// Allows communication between the client and server pipes.
    /// </summary>
    class PipeClient
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeFileHandle CreateFile(
           String pipeName,
           uint dwDesiredAccess,
           uint dwShareMode,
           IntPtr lpSecurityAttributes,
           uint dwCreationDisposition,
           uint dwFlagsAndAttributes,
           IntPtr hTemplate);

        /// <summary>
        /// Handles the messages recieved from a server pipe.
        /// </summary>
        /// <param name="message">The byte message recieved</param>
        public delegate void MessageRecievedHandler(byte[] message);

        /// <summary>
        /// The event that is called whenever a message is recieved from the server pipe.
        /// </summary>
        public event MessageRecievedHandler MessageRecieved;

        /// <summary>
        /// Handles the server disconnected message.
        /// </summary>
        public delegate void ServerDisconnectedHandler();

        /// <summary>
        /// The Event that is called when the server pipe is severed.
        /// </summary>
        public event ServerDisconnectedHandler ServerDisconnected;

        const int BUFFER_SIZE = 4096;

        FileStream stream;
        SafeFileHandle handle;
        Thread readThread;

        /// <summary>
        /// Is the client connected to a server pipe.
        /// </summary>
        private bool connected;

        /// <summary>
        /// The name of the pipe connected to the server.
        /// </summary>
        private string pipeName;

        /// <summary>
        /// Is the client logged in as the admin.
        /// </summary>
        private bool isAdmin;

        #region Accessors

        private bool IsAdmin()
        {
            return isAdmin;
        }

        private void IsAdmin(bool isAdminOrNot)
        {
            isAdmin = isAdminOrNot;
        }

        /// <summary>
        /// Returns true if client is connected to a server.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsConnected()
        {
            return connected;
        }

        /// <summary>
        /// Set the value of the connected boolean.
        /// </summary>
        /// <param name="b">The new value of connected.</param>
        private void IsConnected(bool b)
        {
            connected = b;
        }

        /// <summary>
        /// Return the name of the pipe used to connect to the server.
        /// </summary>
        /// <returns>string</returns>
        public string GetPipeName()
        {
            return pipeName;
        }

        /// <summary>
        /// Set the name of the pipe that connects to the server.
        /// </summary>
        /// <param name="newPipeName">The new pipe name.</param>
        private void SetPipeName(string newPipeName)
        {
            pipeName = newPipeName;
        }

        #endregion

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public void Disconnect()
        {
            if(!IsConnected())
            {
                return;
            }

            // We're no longer connected to the server.
            IsConnected(false);
            SetPipeName(null);
            IsAdmin(false);

            // Clean up the resources
            if (stream != null)
            {
                stream.Close();
            }
            handle.Close();

            stream = null;
            handle = null;
        }

        /// <summary>
        /// Connects client to the server with the pipename.
        /// </summary>
        /// <param name="pipename">The name of the pipe to connect to.</param>
        public void Connect(string pipename)
        {
            if (IsConnected())
            {
                throw new Exception("Already connected to server.");
            }

            SetPipeName(pipename);

            handle = CreateFile(
                GetPipeName(),
                0xC0000000, // GENERIC_READ | GENERIC_WRITE = 0x80000000 | 0x40000000
                0,
                IntPtr.Zero,
                3,  // OPEN_EXISTING
                0x40000000, // FILE_FLAG_OVERLAPPED
                IntPtr.Zero);

            // Couldn't create a handle.
            // The server is probably not running.
            if (handle.IsInvalid)
            {
                MessageBox.Show(
                    "Unable to connect.\nTheServer may not be running.",
                    "Server connection Error.");
                return;
            }



            IsConnected(true);

            // Start listening for messages.
            readThread = new Thread(Read)
            {
                IsBackground = true
            };
            readThread.Start();

            

        }

        private void ValidationResult(byte[] result)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string str = encoder.GetString(result, 0, result.Length);

            if (str.Equals("You are now logged in."))
            {
                IsAdmin(true);
            }
            else
            {
                Disconnect();
                ServerDisconnected();
                MessageBox.Show("Username or password incorrect.\n" +
                    "Please check your spelling and try again.", 
                    "Login Failure");
            }
        }

        /// <summary>
        /// Read the message from the server.
        /// </summary>
        public void Read()
        {
            stream = new FileStream(handle, FileAccess.ReadWrite, BUFFER_SIZE, true);
            byte[] readBuffer = new byte[BUFFER_SIZE];

            while (true)
            {
                int bytesRead = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        // Read the total stream length.
                        int totalSize = stream.Read(readBuffer, 0, 4);

                        // client had disconnected.
                        if (totalSize == 0)
                        {
                            break;
                        }

                        totalSize = BitConverter.ToInt32(readBuffer, 0);

                        do
                        {
                            int numBytes = stream.Read(readBuffer, 0, Math.Min(totalSize - bytesRead, BUFFER_SIZE));

                            ms.Write(readBuffer, 0, numBytes);

                            bytesRead += numBytes;

                        } while (bytesRead < totalSize);

                    }
                    catch
                    {
                        break;
                    }

                    // Client has disconnected.
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Call message recieved event.
                    if(MessageRecieved != null)
                    {
                        MessageRecieved(ms.ToArray());

                        if (!IsAdmin())
                        {
                            ValidationResult(ms.ToArray());
                        }

                    }
                }
            }

            // If disconnected then the disconnection was caused by the server
            // being terminated.
            if (IsConnected())
            {

                // Clean up the resources.
                stream.Close();
                handle.Close();

                stream = null;
                handle = null;

                // Client no longer conected to the server.
                IsConnected(false);
                SetPipeName(null);

                if (ServerDisconnected != null)
                {
                    ServerDisconnected();
                }
            }

        }


        /// <summary>
        /// Sends the message to the server.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SendMessage(byte[] message)
        {
            try
            {
                // Write the entire stream length.
                stream.Write(BitConverter.GetBytes(message.Length), 0, 4);

                stream.Write(message, 0, message.Length);
                stream.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }



    }
}
