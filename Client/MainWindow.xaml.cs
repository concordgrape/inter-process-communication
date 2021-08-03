﻿/*
- FILE : MainWindow.xaml.cs
- PROJECT : PROG2121 - Assignment #05 - Client Side
- PROGRAMMER : Sky Roth
- FIRST VERSION : November 4, 2020
- LAST UPDATE   : November 9, 2020
- DESCRIPTION : This will act as the client side for the chat program, the user can add a username, IP address, and message.
-                   Once these are filled in, it will be sent to the server and other connected clients. 
-                   The client side will also perform validation for the server and inputs, all exceptions will be accounted for.
*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Client
{
    public partial class MainWindow : Window
    {
        readonly TcpClient client = new TcpClient();
        NetworkStream ns = null;
        private string username = "";
        private Thread thread = null;
        private IPAddress ip = null;
        IPEndPoint ipEndPoint = null;

        private bool canChangeIP = true;
        private readonly int kMAXINPUTBYTE = 256;
        private readonly Int32 kPORT = 5000;
        






        public MainWindow()
        {
            InitializeComponent();
        }






        private void SendBtn_Click(object e, RoutedEventArgs a)
        {
            //this is temporary, just to check the length of the input
            Byte[] check = Encoding.ASCII.GetBytes(msgBox.Text);
            if (username == "" || msgBox.Text == "" || ip == null)
            {
                MessageBox.Show("Error: Please submit all buttons before sending a message!");
            //checks to see if the value is too large --> XAML is set to only allow messages that are 256 characters or less, this is just in case that is somehow bypassed
            } else if (check.Length > kMAXINPUTBYTE)
            {
                MessageBox.Show("Error: This message is too large! Please input messages that are less than kMAXINPUTBYTE characters!");
            }
            else
            {
                if (ns != null)
                {
                    DateTime time = DateTime.Now;
                    // String timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
                    Byte[] buffer = Encoding.ASCII.GetBytes(time.ToString() + " - " + username + ": " + msgBox.Text);
                    string buff = System.Text.Encoding.UTF8.GetString(buffer);

                    try
                    {
                        ns.Write(buffer, 0, buffer.Length);
                    }
                    catch (Exception q)
                    {
                        if (msgBox.Text.Contains("details"))
                        {
                            txtBox.AppendText(q.ToString());
                        }
                        else
                        {
                            txtBox.AppendText(buff);
                        }
                    }

                }
            }
        }









        private void SetUser_Click(object e, RoutedEventArgs a)
        {
            if (usernameBox.Text == "")
            {
                PrintText("Error: Please enter a username!");
            }
            else
            {
                username = usernameBox.Text;
                PrintText("Set username to " + username);
            }
        }









        private void ReceiveData(TcpClient client)
        {
            Byte[] receivedBytes = new Byte[kMAXINPUTBYTE];
            int bytes;


            //try to connect to the network stream to read incoming messages
            try
            {
                NetworkStream ns = client.GetStream();
            }
            catch (SocketException d)
            {
                //write the exception to the text box
                txtBox.AppendText(d.ToString());
            }


            //place all reads from the server in a try block, allowing for any exceptions to be caught
            try
            {
                while ((bytes = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
                {
                    //this will allow us to update the UI
                    this.Dispatcher.Invoke(() =>
                    {
                        //parse the string so we just grab the username, we will use this to compare with the current username
                        //this will let us change the colour of the CURRENT client
                        String byteAsString = System.Text.Encoding.UTF8.GetString(receivedBytes);
                        int index = byteAsString.IndexOf("-");
                        String n = byteAsString.Substring(index + 2);
                        int col = n.IndexOf(":");
                        n = n.Substring(0, col);

                        Paragraph p = new Paragraph(new Run(Encoding.ASCII.GetString(receivedBytes, 0, bytes)));

                        if (string.Equals(username, n) && username != "")
                        {
                            p.Foreground = Brushes.Red;
                        }
                        else
                        {
                            p.Foreground = Brushes.Gray;
                        }

                        //add the new paragraph to the txtbox, this will include colouring
                        txtBox.Document.Blocks.Add(p);
                    });
                }
            }
            catch (SocketException e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    txtBox.AppendText(e.ToString());
                    PrintText("*Error: Socket Failed!*", "darkred");
                });
            }
            catch (Exception y)
            {
                this.Dispatcher.Invoke(() =>
                {
                    PrintText("*Error: Server was disconnected! You may close the program now, enter 'details' for more details*\n", "darkred");

                    if (msgBox.Text.Contains("details"))
                    {
                        txtBox.AppendText(y.ToString());
                    }
                });
            }
        }








        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //before the user clicks the "X" to close the window, we will close the client and any threads that may be running
            client.Client.Shutdown(SocketShutdown.Send);
            thread.Abort();
            ns.Close();
            client.Close();
        }








        //always scroll to the end of the document, so that when a lot of messages appear, the user is always presented with the newest message first
        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtBox.ScrollToEnd();
        }









        private void SetIpAdd_Click(object sender, RoutedEventArgs e)
        {
            //check if we can change the IP, if the IP has already been set then the user won't be able to
            if (canChangeIP)
            {
                if (ipAddressBox.Text == "")
                {
                    MessageBoxResult result = MessageBox.Show("Error: Please enter an IP Address");
                }
                //parse the entered IP to ensure it is valid
                else if (IPAddress.TryParse(ipAddressBox.Text, out ip))
                {
                    //try to connect to the IP address the user provided
                    try
                    {
                        //this end point will allow us to connect to the server so that we will be able to send a message
                        ipEndPoint = new IPEndPoint(ip, kPORT);

                        //connect the client
                        client.Connect(ipEndPoint);

                        //get the stream of the client
                        ns = client.GetStream();

                        //start a new thread that will be receiving data
                        thread = new Thread(obj => ReceiveData((TcpClient)obj));
                        thread.Start(client);

                        //tell the user that the connection was successful
                        PrintText("Connected to IP address: " + ip.ToString());

                        //set it so the user can't change the IP address after it's set
                        canChangeIP = false;
                    }
                    //if any problems arise, show the user the problem
                    catch (SocketException x)
                    {
                        PrintText("Error: Connecting to server failed, please ensure the server is running!\n\n" + x.ToString(), "darkred");
                    }
                    
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Error: This is not a valid IP");
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Error: Sorry silly goose, you can't change the IP address after you set it!");
            }

        }









        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (!client.Connected)
            {
                PrintText("Error: Client is not connected to a server!", "black");
                return;
            }

            String message = username + " is disconnecting from: " + ip.ToString();
            Byte[] buffer = Encoding.ASCII.GetBytes(message);

            try
            {
                //write the message to the other clients
                ns.Write(buffer, 0, buffer.Length);

                //shutdown the client and the thread
                client.Client.Shutdown(SocketShutdown.Send);
                thread.Abort();
                ns.Close();
                client.Close();
            }
            catch (Exception q)
            {
                txtBox.AppendText("Error when disconnecting from server! \n\n\n" + q.ToString());
            }
        }







        private void Close_Click(object sender, RoutedEventArgs e)
        {
            //check if the client is connected to a server before closing
            if (client.Connected)
            {
                MessageBox.Show("Error: You must disconnect the server before closing!");
            }
            else
            {
                thread.Abort();
                Environment.Exit(Environment.ExitCode);
            }
        }







        //since I'm using a "Rich Text Box", it will allow us to change colours of our inputs, this will replace constantly calling the same 3 lines
        private void PrintText(string text, string colour = "black")
        {
            Paragraph alert = new Paragraph(new Run(text));
            if (colour.Contains("darkred"))
            {
                alert.Foreground = Brushes.DarkRed;
            } else if (colour.Contains("red"))
            {
                alert.Foreground = Brushes.Red;
            } else
            {
                alert.Foreground = Brushes.Black;
            }
            txtBox.Document.Blocks.Add(alert);
        }

    }

}
