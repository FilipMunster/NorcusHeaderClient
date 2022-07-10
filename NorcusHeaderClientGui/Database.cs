using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NorcusSetClient
{
    public class Database
    {
        private readonly string dbFile;
        public Database() { }
        public Database(string databaseFileName)
        {
            dbFile = databaseFileName;
        }
        public List<Song> Songs { get; set; } = new List<Song>();
        
        public bool Load(string fileName = "")
        {
            if (fileName == "")
                fileName = dbFile;

            System.Xml.Serialization.XmlSerializer serializer = 
                new System.Xml.Serialization.XmlSerializer(typeof(Database));
            
            if (!System.IO.File.Exists(fileName))
                return false;
            System.IO.FileStream file = System.IO.File.OpenRead(fileName);
            
            Database database;
            object deserialized = serializer.Deserialize(file);
            database = deserialized as Database;           
            file.Close();
            
            if (database is null)
                return false;

            Songs = new List<Song>(database.Songs);
            return true;
        }
        public void Save(string fileName = "")
        {
            if (fileName == "")
                fileName = dbFile;

            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(Database));

            System.IO.FileStream file = System.IO.File.Create(fileName);

            serializer.Serialize(file, this);
            file.Close();
        }

        public Song GetSongByTitle(string songTitle)
        {
            return Songs.FirstOrDefault(x => x.Title == songTitle);
        }
        public Song GetSongByFileName(string fileName)
        {
            Song song = Songs.FirstOrDefault(x => x.FileName == fileName);

            if (song is null)
            {
                Songs.Add(new Song() { FileName = fileName });
                song = Songs.Last();
            }
            return song;
        }

        public void UpdatePlayedSong(string fileName, int duration)
        {
            Song song = GetSongByFileName(fileName);

            // pokud se písnička nehrála celá
            if (duration < 60)
                return;

            // pokud byly noty zobrazené jen po dobu písničky, updatuji její průměrnou délku
            if (duration < 600)
            {
                double newAverage = (song.TimesPlayed * song.AverageDuration + duration) / (song.TimesPlayed + 1);
                song.AverageDuration = Convert.ToInt32(Math.Round(newAverage));
            }

            song.TimesPlayed++;
            song.LastPlayed = DateTime.Now;
        }
    }
}
