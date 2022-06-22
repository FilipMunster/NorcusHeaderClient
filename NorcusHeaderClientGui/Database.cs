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
            try
            {
                object deserialized = serializer.Deserialize(file);
                database = (Database)deserialized;
            }
            catch
            {
                file.Close();
                return false;
            }

            file.Close();
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
            return Songs.FirstOrDefault(x => x.FileName == fileName);
        }
    }
}
