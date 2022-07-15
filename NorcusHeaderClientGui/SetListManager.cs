using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NorcusSetClient
{
    class SetListManager
    {
        private Database _database;

        private string _currentSongFileName;
        private DateTime _currentSongStartTime = default;
        /// <summary>
        /// Čas, kdy začala přestávka
        /// </summary>
        private DateTime _pauseStartTime = DateTime.Now;
        /// <summary>
        /// Čas do konce sady (s) (součet délek písniček od aktuální zobrazené do konce sady)
        /// </summary>
        private int _setListDuration = 0;

        /// <summary>
        /// Čas do konce sady v aktuální čas
        /// </summary>
        public TimeSpan SetListDuration
        {
            get
            {
                if (IsPause || _currentSongStartTime == default)
                    return TimeSpan.FromSeconds(_setListDuration);
                else
                {
                    TimeSpan duration = TimeSpan.FromSeconds(_setListDuration) - (DateTime.Now - _currentSongStartTime);
                    return duration.TotalSeconds > 0 ? duration : new TimeSpan(0, 0, 0);
                }
            }
        }
        /// <summary>
        /// Délka přestávky
        /// </summary>
        public TimeSpan PauseDuration => IsPause ? DateTime.Now - _pauseStartTime : new TimeSpan(0, 0, 0);
        /// <summary>
        /// Čas, kdy skončí sada
        /// </summary>
        public DateTime SetEndTime => DateTime.Now + SetListDuration;
        public bool IsPause { get; private set; } = true;

        public SetListManager(Database database)
        {
            _database = database;
        }
        public void SelectionChanged(object sender, NorcusClient.SelectionChangedEventArgs e)
        {
            switch (e.NorcusAction)
            {
                case NorcusClient.NorcusAction.SetEmpied:
                    SetEmptied();
                    break;
                case NorcusClient.NorcusAction.SetStarted:
                    SetStarted();
                    break;
                case NorcusClient.NorcusAction.SetChanged:
                    SetChanged(e);
                    break;
                case NorcusClient.NorcusAction.SongChanged:
                    SongChanged(e);
                    break;
                default:
                    break;
            }
        }
        private void SetEmptied()
        {
            if (!(_currentSongFileName is null || _currentSongStartTime == default))
            {
                int duration = (int)(DateTime.Now - _currentSongStartTime).TotalSeconds;
                _database.UpdatePlayedSong(_currentSongFileName, duration);
            }
            _pauseStartTime = DateTime.Now;
            _setListDuration = 0;
            _currentSongStartTime = default;
            IsPause = true;
        }
        private void SetStarted()
        {
            IsPause = false;
        }
        private void SetChanged(NorcusClient.SelectionChangedEventArgs e)
        {
            UpdateSetDuration(e);
        }
        private void SongChanged(NorcusClient.SelectionChangedEventArgs e)
        {
            if (!(_currentSongFileName is null || _currentSongStartTime == default(DateTime)))
            {
                int duration = (int)(DateTime.Now - _currentSongStartTime).TotalSeconds;
                _database.UpdatePlayedSong(_currentSongFileName, duration);
            }

            _currentSongFileName = e.CurrentFileName;
            _currentSongStartTime = DateTime.Now;

            UpdateSetDuration(e);
        }
        private void UpdateSetDuration(NorcusClient.SelectionChangedEventArgs e)
        {
            if (e.SetList is null || e.SetList.Length == 0)
            {
                _setListDuration = 0;
                return;
            }

            int songIndex = e.CurrentSongIndex < 0 ? 0 : e.CurrentSongIndex;
            int remainingTime = 0;

            for (int i = songIndex; i < e.SetList.Length; i++)
            {
                int? avgDuration = _database.GetSongByTitle(e.SetList[i])?.AverageDuration;
                remainingTime += avgDuration > 0 ? (int)avgDuration : 240;
            }
            _setListDuration = remainingTime;
        }
    }
}
