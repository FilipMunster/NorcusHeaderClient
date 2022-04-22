using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NorcusSetClient
{
    public class NorcusClient : INotifyPropertyChanged
    {
        public const string msgEmpty = "NENÍ VYBRÁNA ŽÁDNÁ PÍSNIČKA";
        public const string msgNoServer = "SERVER NEDOSTUPNÝ! VYHLEDÁVÁM SPOJENÍ...";
        public const string msgErrorServer = "CHYBA V KOMUNIKACI SE SERVEREM! RESTARTUJI KLIENTA...";

        private const int buffsize = 1024;
        private byte[] buffer = new byte[buffsize];
        private readonly byte[] dummy;

        private readonly byte[] id;
        private readonly string hostIp;
        private readonly int port;
        private Socket socket;
        private IPEndPoint endPoint;

        /// <summary>
        /// Create new NorcusClient instance
        /// </summary>
        /// <param name="hostIp">Server IP address</param>
        /// <param name="port">Communication port</param>
        /// <param name="id">Client ID</param>
        public NorcusClient(string hostIp, int port, string id)
        {
            this.hostIp = hostIp;
            this.port = port;
            this.id = Encoding.ASCII.GetBytes(id);

            string dummyStr = "DUMMY";
            while (dummyStr.Length < buffsize)
            {
                dummyStr += "@";
            }
            this.dummy = Encoding.ASCII.GetBytes(dummyStr);
            ProcessMessage(msgNoServer);
        }

        /// <summary>
        /// Seznam písniček v sadě
        /// </summary>
        public string[] SetList { get; private set; }

        /// <summary>
        /// Název aktuálně zobrazeného souboru vč. přípony, bez cesty
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Zobrazený text v okně
        /// </summary>
        public string Message
        {
            get
            {
                string msg = String.Join("{@}", SetList);

                // Pokud zobrazuji na řádku, nahradím mezery mezi slovy nedělitelnými mezerami,
                // ať není žádná položka přes 2 řádky.
                if (!SongSeparator.Contains("\n"))
                    msg = msg.Replace(" ", "\u00a0");

                return msg.Replace("{@}", SongSeparator);
            }
        }

        /// <summary>
        /// Aktuálně zobrazená písnička
        /// </summary>
        public string CurrentSong
        {
            get
            {
                string currentSong = GetCurrentSong();
                // Pokud zobrazuji na řádku, nahradím mezery mezi slovy nedělitelnými mezerami,
                // ať není žádná položka přes 2 řádky.
                if (!SongSeparator.Contains("\n"))
                    currentSong = currentSong.Replace(" ", "\u00a0");
                return currentSong;
            }
        }

        /// <summary>
        /// Pořadí vybrané písničky v sadě
        /// </summary>
        public int CurrentSongIndex { get; private set; }

        /// <summary>
        /// Oddělovač mezi položkami v sadě
        /// </summary>
        public string SongSeparator { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Provede připojení k serveru
        /// </summary>
        /// <returns></returns>
        private async Task ConnectServerAsync()
        {
            await Task.Run(() =>
            {
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

        /// <summary>
        /// Spuštění klienta.
        /// </summary>
        public async void RunClient()
        {
            ProcessMessage(msgNoServer);
            await ConnectServerAsync();

            // Nastane, pokud byla zadaná neplatná IP adresa:
            if (socket is null)
                return;

            ProcessMessage(msgEmpty);
            int bytesRec;

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            socket.Send(dummy);
                            Console.WriteLine("poslán dummy");
                            bytesRec = socket.Receive(buffer);
                            Console.WriteLine("přijata odpověď");
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
            catch
            {
                ProcessMessage(msgErrorServer);
                SocketClose();
                await Task.Run(() => Thread.Sleep(1000));
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        public void SocketClose()
        {
            if (socket.IsBound)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.WriteLine("Socket closed");
                MessageBox.Show("Socket closed");
            }
        }

        /// <summary>
        /// Zpracování přijaté zprávy
        /// </summary>
        /// <param name="inputText">Přijatá zpráva</param>
        private void ProcessMessage(string inputText)
        {
            string text = inputText.Trim('@');

            /// Pokud vrátil prázdnou sadu, vypíšu <see cref="msgEmpty"/>.
            if (text == "SADA")
                text = msgEmpty;

            // Pokud jsou poslány noty:
            if (text.StartsWith("noty/"))
            {
                FileName = text.Substring(5);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong)));
                return;
            }

            // Pokud je poslána sada:
            if (text.StartsWith("SADA"))
            {
                text = text.Substring(5, text.Length - 6); // Odstranění textu SADA a prvního a posledního apostrofu
            }

            SetList = text.Split(new string[] { "', '" }, StringSplitOptions.RemoveEmptyEntries);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
        }

        /// <summary>
        /// Vrátí zobrazený název aktuální písničky na základě <see cref="FileName"/>
        /// </summary>
        /// <returns>Název písničky zobrazený v <see cref="Message"/></returns>
        private string GetCurrentSong()
        {
            if (FileName is null || FileName == "" ||
                SetList is null || SetList[0] == msgEmpty || SetList[0] == msgNoServer)
                return "";

            // Převedení seznam písniček do podoby, která odpovídá názvům souborů v databázi.
            string[] setListMod = new string[SetList.Length];
            for (int i = 0; i < SetList.Length; i++)
            {
                // Text malými písmeny a normalizování na FormD, tj. diakritika se převede na NonSpacingMark (například "něco" se převede na "neˇco")
                setListMod[i] = SetList[i].ToLower().Normalize(NormalizationForm.FormD);
                // Odstranění diakritiky
                setListMod[i] = new string(setListMod[i].Where(
                    (ch) => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    .ToArray());

                setListMod[i] = setListMod[i].Replace(" ", "_");
                setListMod[i] = setListMod[i].Replace("-", "_");
                setListMod[i] = setListMod[i].Replace("+", "_");
                setListMod[i] = setListMod[i].Replace("/", "_");
                setListMod[i] = setListMod[i].Replace("'", "");
                setListMod[i] = setListMod[i].Replace(",", "");
            }

            // Izolace názvu písničky z názvu souboru:
            int separatorIndex = FileName.IndexOfAny(new char[] { '-', '.' }); // hledám i tečku pro případy, kdy není v názvu zadán intepret
            string currentSong = FileName.Substring(0, separatorIndex).ToLower();

            // Nalezení indexu zobrazené písničky:
            int currentSongIndex = -1;
            for (int i = 0; i < setListMod.Length; i++)
            {
                if (setListMod[i] == currentSong)
                {
                    currentSongIndex = i;
                    break;
                }
            }

            // Pokud jsem písničku nenašel, zkusím ještě najít první slovo z názvu písničky v celém názvu souboru
            if (currentSongIndex < 0)
            {
                for (int i = 0; i < SetList.Length; i++)
                {
                    string firstWord = setListMod[i].Split('_')[0];
                    if (FileName.ToLower().Contains(firstWord))
                    {
                        currentSongIndex = i;
                        break;
                    }
                }
            }

            CurrentSongIndex = currentSongIndex;

            if (currentSongIndex < 0)
                return "";

            return SetList[currentSongIndex];
        }
    }
}
