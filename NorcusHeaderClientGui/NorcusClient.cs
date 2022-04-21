using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NorcusSetClient
{
    public class NorcusClient : INotifyPropertyChanged
    {
        private const string msgEmpty = "NENÍ VYBRÁNA ŽÁDNÁ PÍSNIČKA";
        private const string msgNoServer = "SERVER NEDOSTUPNÝ!\nVYHLEDÁVÁM SPOJENÍ...";

        private const int buffsize = 1024;
        private byte[] buffer = new byte[buffsize];
        private byte[] dummy;

        private byte[] id = Encoding.ASCII.GetBytes(Properties.Settings.Default.id);
        private string hostIp;
        private int port;
        private Socket socket;
        private IPEndPoint endPoint;

        /// <summary>
        /// Seznam písniček v sadě
        /// </summary>
        private string[] setList = { "" };

        /// <summary>
        /// Název aktuálně zobrazenéh souboru vč. přípony, bez cesty
        /// </summary>
        private string fileName = "";

        /// <summary>
        /// Zobrazený text v okně
        /// </summary>
        public string Message
        {
            get
            {
                string msg = String.Join("@", setList);

                // Pokud zobrazuji na řádku, nahradím mezery mezi slovy nedělitelnými mezerami, ať není žádná položka přes 2 řádky.
                if (!Properties.Settings.Default.vertical)
                    msg = msg.Replace(" ", "\u00a0");

                return msg.Replace("@", SongSeparator);
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
                if (!Properties.Settings.Default.vertical)
                    currentSong = currentSong.Replace(" ", "\u00a0");
                return currentSong;
            }
        }

        /// <summary>
        /// Oddělovač mezi položkami v sadě
        /// </summary>
        private string SongSeparator => Properties.Settings.Default.vertical ? "\n" : ", ";

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
            if (text.StartsWith("noty/"))
            {
                fileName = text.Substring(5);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong)));
                return;
            }

            // Pokud je poslána sada:
            if (text.StartsWith("SADA"))
            {
                text = text.Substring(5, text.Length - 6); // Odstranění textu SADA a prvního a posledního apostrofu
            }

            setList = text.Split(new string[] { "', '" }, StringSplitOptions.RemoveEmptyEntries);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
        }

        /// <summary>
        /// Vrátí zobrazený název aktuální písničky na základě <see cref="fileName"/>
        /// </summary>
        /// <returns>Název písničky zobrazený v <see cref="Message"/></returns>
        private string GetCurrentSong()
        {
            if (fileName == "" || setList[0] == msgEmpty || setList[0] == msgNoServer)
                return "";

            // Převedení seznam písniček do podoby, která odpovídá názvům souborů v databázi.
            string[] setListMod = new string[setList.Length];
            for (int i = 0; i < setList.Length; i++)
            {
                // Text malými písmeny a normalizování na FormD, tj. diakritika se převede na NonSpacingMark (například "něco" se převede na "neˇco")
                setListMod[i] = setList[i].ToLower().Normalize(NormalizationForm.FormD);
                // Odstranění diakritiky
                setListMod[i] = new string(setListMod[i].Where(
                    (ch) => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    .ToArray());

                setListMod[i] = setListMod[i].Replace(" ", "_");
                setListMod[i] = setListMod[i].Replace("-", "_");
                setListMod[i] = setListMod[i].Replace("+", "_");
                setListMod[i] = setListMod[i].Replace("'", "");
                setListMod[i] = setListMod[i].Replace(",", "");
            }

            // Izolace názvu písničky z názvu souboru:
            int separatorIndex = fileName.IndexOfAny(new char[] { '-', '.' }); // hladeám i tečku pro případy, kdy není v názvu zadán intepret
            string currentSong = fileName.Substring(0, separatorIndex).ToLower();

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
                for (int i = 0; i < setList.Length; i++)
                {
                    string firstWord = setListMod[i].Split('_')[0];
                    if (fileName.ToLower().Contains(firstWord))
                    {
                        currentSongIndex = i;
                        break;
                    }
                }
            }

            if (currentSongIndex < 0)
                return "";

            return setList[currentSongIndex];
        }
    }
}
