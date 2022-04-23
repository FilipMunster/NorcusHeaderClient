using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        private const int buffsize = 1024;
        private byte[] buffer = new byte[buffsize];
        private readonly byte[] dummy;
        private Random random = new Random();

        private readonly byte[] id;
        private readonly string hostIp;
        private readonly int port;
        private Socket socket;

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

        public Logger Logger { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Provede připojení k serveru
        /// </summary>
        /// <returns></returns>
        private async Task ConnectServerAsync()
        {
            await Task.Run(() =>
            {
                WriteToConsole("ConnectServerAsync -> connecting to server");
                if (!IPAddress.TryParse(hostIp, out IPAddress ipAddress))
                {
                    ProcessMessage("Zadaná IP adresa " + hostIp + " je neplatná!");
                    return;
                }

                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        tryCount++;
                        socket.Connect(ipAddress, port);
                        WriteToConsole("ConnectServerAsync -> socket connected (try #" + tryCount + ")");
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole("ConnectServerAsync -> socket connect failed (try #" + tryCount + ")\n\t" +
                            "Exception: " + ex.Message);
                        Thread.Sleep(1000);
                        continue;
                    }
                    _ = socket.Send(id);
                    WriteToConsole("ConnectServerAsync -> SENT my id  (" + Encoding.ASCII.GetString(id) + ")");
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
                            WriteToConsole("RunClient -> SENT dummy");

                            bytesRec = socket.Receive(buffer);
                            WriteToConsole("RunClient -> RECEIVED " + bytesRec + " bytes");
                            WriteToConsole("\tMESSAGE: " + Encoding.UTF8.GetString(buffer, 0, bytesRec).Trim('@'));

                            _ = socket.Send(id); // Poslat serveru v odpovědi moje id
                            WriteToConsole("RunClient -> SENT my id (" + Encoding.ASCII.GetString(id) + ")");

                            if (bytesRec == 0)
                            {
                                int delay = 1000 + random.Next(1000);
                                WriteToConsole("Sleep " + delay + "ms before continuing.");
                                Thread.Sleep(delay);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteToConsole("RunClient -> Exception thrown" +
                                "\n\tException: " + ex.Message);
                            ProcessMessage(msgNoServer);
                            SocketClose();
                            await ConnectServerAsync();
                            ProcessMessage(msgEmpty);
                            continue;
                        }

                        ProcessMessage(Encoding.UTF8.GetString(buffer, 0, bytesRec));
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Shuts down and closes <see cref="socket"/> if is bound
        /// </summary>
        public void SocketClose()
        {
            try
            {
                if (socket.IsBound)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    WriteToConsole("SocketClose -> socket closed");
                }
                else
                {
                    WriteToConsole("SocketClose -> socket was already closed!");
                }
            }
            catch (Exception e)
            {
                WriteToConsole("SocketClose -> Exception thrown" +
                    "\n\tException: " + e.Message);
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
            if (text == "SADA" || text == "")
            {
                text = msgEmpty;
                FileName = "";
            }

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
                FileName = "";
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

        /// <summary>
        /// Vypíše zprávu do konzole a do logovacího souboru. Když je aplikace spuštěna z příkazové řádky, vypisuje i do ní.
        /// </summary>
        /// <param name="message"></param>
        public void WriteToConsole(string message)
        {
            AttachConsole(-1);
            Console.WriteLine(message);
            Logger?.Log(message);
        }
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
    }
}
