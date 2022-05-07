using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NorcusSetClient
{
    [Serializable]
    public class Song
    {
        public Song() { }

        public string FileName { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Tempo { get; set; }
        public Gender Singer { get; set; }
        public SongType Type { get; set; }
        public bool Old { get; set; }
        public bool PlayedByBrass { get; set; }
        public DateTime LastPlayed { get; set; }
        public int AverageDuration { get; set; }
        public int TimesPlayed { get; set; }

        public enum Gender
        {
            Male,
            Female,
            Both
        };
        public enum SongType
        {
            Slow,
            Fast,
            Brass
        }
    }
}
