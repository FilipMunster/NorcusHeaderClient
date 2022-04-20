using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NorcusHeaderClientGui
{
    public class Client : INotifyPropertyChanged
    {
        private const string msgEmpty = "NENÍ VYBRÁNA ŽÁDNÁ PÍSNIČKA";
        private const string msgNoServer = "SERVER NEDOSTUPNÝ! VYHLEDÁVÁM SPOJENÍ...";
        private byte[] id = Encoding.ASCII.GetBytes("UNKNOWN");

        private const int buffsize = 1024;
        private byte[] buffer = new byte[buffsize];
        private byte[] dummy;

        private string messageLabel;
        public string MessageLabel
        {
            get => messageLabel;
            set
            {
                messageLabel = value;
                Console.WriteLine(value);
            }
        }
        public string SongSeparator { get; set; }

        private string hostIp;
        private int port;
        private Socket socket;
        private IPEndPoint endPoint;

        public event PropertyChangedEventHandler PropertyChanged;

        public Client(string hostIp, int port)
        {
            this.hostIp = hostIp;
            this.port = port;
            SongSeparator = " ";

            string dummyStr = "DUMMY";
            while (dummyStr.Length < buffsize)
            {
                dummyStr += "@";
            }
            this.dummy = Encoding.ASCII.GetBytes(dummyStr);
            Message(msgNoServer);
        }

        public async void ConnectServer()
        {
            IPHostEntry host = Dns.GetHostEntry(hostIp);
            IPAddress ipAddress = host.AddressList[0];
            endPoint = new IPEndPoint(ipAddress, port);
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        socket.Connect(endPoint);
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    socket.Send(id);
                    break;
                }
            });

        }
        public async void MainLoop()
        {
            Message(msgNoServer);
            await Task.Factory.StartNew(() => ConnectServer());
            Message(msgEmpty);
            int bytesRec;

            await Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    socket.Send(dummy);
                    try
                    {
                        bytesRec = socket.Receive(buffer);
                    }
                    catch
                    {
                        Message(msgNoServer);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        ConnectServer();
                        Message(msgEmpty);
                        continue;
                    }

                    Message(Encoding.UTF8.GetString(buffer, 0, bytesRec));

                    // Poslat serveru v odpovědi moje id
                    socket.Send(id);
                }
            });
               
        }
        private void Message(string inputText)
        {
            string text = inputText.Trim('@');
            text = text.Replace("'", "");
            // nahrazení mezer nedělitelnými
            text = text.Replace(" ", "\u00a0");
            text = text.Replace(",\u00a0", "," + SongSeparator);

            if (text.StartsWith("noty"))
                return;

            if (text.StartsWith("SADA"))
                text = text.Substring(4);

            MessageLabel = text;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageLabel)));
        }
    }
}
