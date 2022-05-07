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
    public class NorcusClient
    {
        public const string msgEmpty = "NENÍ VYBRÁNA ŽÁDNÁ PÍSNIČKA";
        public const string msgNoServer = "SERVER NEDOSTUPNÝ! VYHLEDÁVÁM SPOJENÍ...";

        private const int buffsize = 1024;
        private readonly byte[] buffer = new byte[buffsize];
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
        public string CurrentFileName { get; private set; } = String.Empty;

        /// <summary>
        /// Zobrazený text v okně
        /// </summary>
        public string Message { get; private set; } = String.Empty;

        /// <summary>
        /// Aktuálně zobrazená písnička
        /// </summary>
        public string CurrentSongTitle => GetCurrentSong();

        /// <summary>
        /// Pořadí vybrané písničky v sadě
        /// </summary>
        public int CurrentSongIndex
        {
            get
            {
                if (SetList is null)
                    return -1;
                return SetList.ToList().IndexOf(CurrentSongTitle);
            }
        }

        public Logger Logger { get; set; }

        public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);
        public event SelectionChangedEventHandler SelectionChanged;

        public enum NorcusAction
        {
            SongChanged,
            SetChanged,
            SetEmpied,
            SetStarted,
            StatusMessage
        }
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
                                int delay = 1000 + random.Next(2000);
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
        /// Shuts down and closes <see cref="socket"/> if it's bound
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
                Message = msgEmpty;

                NorcusAction action = NorcusAction.SetChanged;
                if (CurrentFileName != String.Empty)
                    action = NorcusAction.SetEmpied;
                
                CurrentFileName = String.Empty;
                SetList = null;
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, action));
                return;
            }

            // Pokud jsou poslány noty:
            if (text.StartsWith("noty/"))
            {
                Message = String.Empty;

                NorcusAction action = NorcusAction.SongChanged;
                if (CurrentFileName == String.Empty)
                    action = NorcusAction.SetStarted;

                CurrentFileName = text.Substring(5);
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, action));
                return;
            }

            // Pokud je poslána sada:
            if (text.StartsWith("SADA"))
            {
                Message = String.Empty;
                text = text.Substring(5, text.Length - 6); // Odstranění textu SADA a prvního a posledního apostrofu
                SetList = text.Split(new string[] { "', '" }, StringSplitOptions.RemoveEmptyEntries);
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, NorcusAction.SetChanged));
                return;
            }

            Message = text;
            CurrentFileName = String.Empty;
            SetList = null;
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, NorcusAction.StatusMessage));
        }

        /// <summary>
        /// Vrátí zobrazený název aktuální písničky na základě <see cref="CurrentFileName"/>
        /// </summary>
        /// <returns>Název zobrazené písničky</returns>
        private string GetCurrentSong()
        {
            if (CurrentFileName == "" || SetList is null)
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
            int separatorIndex = CurrentFileName.IndexOfAny(new char[] { '-', '.' }); // hledám i tečku pro případy, kdy není v názvu zadán intepret
            string currentSong = CurrentFileName.Substring(0, separatorIndex).ToLower();

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
                    if (CurrentFileName.ToLower().Contains(firstWord))
                    {
                        currentSongIndex = i;
                        break;
                    }
                }
            }

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

        public class SelectionChangedEventArgs
        {
            public SelectionChangedEventArgs(NorcusClient client, NorcusAction action)
            {
                this.SetList = client.SetList;
                this.CurrentFileName = client.CurrentFileName;
                this.Message = client.Message;
                this.CurrentSongIndex = client.CurrentSongIndex;
                this.CurrentSongTitle = client.CurrentSongTitle;
                this.NorcusAction = action;
            }
            public string[] SetList { get; }
            public string CurrentFileName { get; }
            public string Message { get; }
            public int CurrentSongIndex { get; }
            public string CurrentSongTitle { get; }
            public NorcusAction NorcusAction { get; }
        }

    }
}
