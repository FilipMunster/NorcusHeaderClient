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
    public class NorcusClient : INotifyPropertyChanged
    {
        private const string msgEmpty = "NENÍ VYBRÁNA ŽÁDNÁ PÍSNIČKA";
        private const string msgNoServer = "SERVER NEDOSTUPNÝ!\nVYHLEDÁVÁM SPOJENÍ...";
        private byte[] id = Encoding.ASCII.GetBytes(Properties.Settings.Default.id);

        private const int buffsize = 1024;
        private byte[] buffer = new byte[buffsize];
        private byte[] dummy;

        private string messageLabel;
        public string MessageLabel
        {
            get
            {
                string msg = messageLabel;
                // Pokud zobrazuji na řádku, nahradím mezery nedělitelnými, ať není žádná položka přes 2 řádky.
                if (!Properties.Settings.Default.vertical)
                    msg = msg.Replace(" ", "\u00a0");

                return msg.Replace("@", SongSeparator);
            }
            set
            {
                messageLabel = value;
                Console.WriteLine(value);
            }
        }
        public string SongSeparator => Properties.Settings.Default.vertical ? "\n" : ", ";

        private string hostIp;
        private int port;
        private Socket socket;
        private IPEndPoint endPoint;

        public event PropertyChangedEventHandler PropertyChanged;

        public NorcusClient(string hostIp, int port)
        {
            this.hostIp = hostIp;
            this.port = port;

            string dummyStr = "DUMMY";
            while (dummyStr.Length < buffsize)
            {
                dummyStr += "@";
            }
            this.dummy = Encoding.ASCII.GetBytes(dummyStr);
            ProcessMessage(msgNoServer);
        }

        public async Task ConnectServerAsync()
        {
            await Task.Run(() => {
                if (!IPAddress.TryParse(hostIp, out IPAddress ipAddress))
                {
                    ProcessMessage("Zadaná IP adresa " + hostIp + " je neplatná!");
                    return;
                }

                endPoint = new IPEndPoint(ipAddress, port);
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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
        public async void RunClient()
        {
            ProcessMessage(msgNoServer);
            await ConnectServerAsync();

            // Nastane, pokud byla zadaná neplatná IP adresa:
            if (socket is null)
                return;
            
            ProcessMessage(msgEmpty);
            int bytesRec;

            await Task.Run(async () =>
            {
                while (true)
                {                    
                    try
                    {
                        socket.Send(dummy);
                        bytesRec = socket.Receive(buffer);
                    }
                    catch
                    {
                        ProcessMessage(msgNoServer);
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                        }
                        catch { }
                        await ConnectServerAsync();
                        ProcessMessage(msgEmpty);
                        continue;
                    }

                    ProcessMessage(Encoding.UTF8.GetString(buffer, 0, bytesRec));

                    // Poslat serveru v odpovědi moje id
                    socket.Send(id);
                }
            });
               
        }
        /// <summary>
        /// Zpracování přijaté zprávy
        /// </summary>
        /// <param name="inputText">Přijatá zpráva</param>
        private void ProcessMessage(string inputText)
        {
            string text = inputText.Trim('@');

            // Pokud vrátil prázdnou sadu, nic nedělej.
            if (text == "SADA")
                return;
                        
            // Pokud jsou poslány noty:
            if (text.StartsWith("noty"))
                return;

            // Pokud je poslána sada:
            if (text.StartsWith("SADA"))
            {
                // Nastavit oddělovač mezi položkami v sadě znakem @
                text = text.Replace("', '", "@");

                text = text.Replace("'", "");
                text = text.Substring(4);
            }                

            MessageLabel = text;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageLabel)));
        }
    }
}
