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
        public Database(string databaseFileName, Logger logger)
        {
            dbFile = databaseFileName;
            Logger = logger;
        }
        public List<Song> Songs { get; set; } = new List<Song>();
        public Logger Logger { get; set; }
        
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
            {
                Logger?.Log($"Database: Error loading database file {dbFile}.");
                return false;
            }

            Songs = new List<Song>(database.Songs);
            Logger?.Log($"Database: Success loading database file \"{dbFile}\". {Songs.Count} songs loaded.");
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
            Logger?.Log($"Database: Database saved to file \"{fileName}\".");
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
                Logger?.Log($"Database: Added new song to database: \"{fileName}\"");
            }
            return song;
        }

        public void UpdatePlayedSong(string fileName, int duration)
        {
            Song song = GetSongByFileName(fileName);

            // pokud se písnička nehrála celá
            if (duration < 60)
            {
                Logger?.Log($"Database: Song duration was not updated (too short): \"{fileName}\", duration {duration}s.");
                return;
            }

            // pokud byly noty zobrazené jen po dobu písničky, updatuji její průměrnou délku
            if (duration < 600 || duration < (song.AverageDuration * 1.5))
            {
                double newAverage = (song.TimesPlayed * song.AverageDuration + duration) / (song.TimesPlayed + 1);
                song.AverageDuration = Convert.ToInt32(Math.Round(newAverage));
                Logger?.Log($"Database: Song duration updated: \"{fileName}\", dur: {duration}s, avg: {newAverage}s, count: {song.TimesPlayed + 1}");
            }
            else
            {
                Logger?.Log($"Database: Song duration was not updated (too long): \"{fileName}\", duration {duration}s.");
            }

            song.TimesPlayed++;
            song.LastPlayed = DateTime.Now;
        }
    }
}
