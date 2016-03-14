using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Data.SQLite.Linq;
using System.Data.Entity;
using System.Windows;
using System.Data.Linq.Mapping;
using System.Drawing;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Net.Sockets;

namespace ServerData
{
    [Serializable, XmlRoot("User")]
    [Table(Name = "users")]
    public class User
    {
        [Column(Name = "ID", IsPrimaryKey = true)]
        public int ID { get; set; }

        [Column(Name = "Username")]
        public String Username { get; set; }

        [Column(Name = "Password")]
        public String Password { get; set; }

        [Column(Name = "Email")]
        public String Email { get; set; }

        [Column(Name = "Points")]
        public int Points { get; set; }

        [Column(Name = "Wins")]
        public int Wins { get; set; }

        [Column(Name = "Losses")]
        public int Losses { get; set; }

        [Column(Name = "Draws")]
        public int Draws { get; set; }

        [Column(Name = "Streak")]
        public int Streak { get; set; }

        [Column(Name = "Rank")]
        public int Rank { get; set; }

        [Column(Name = "Div")]
        public int Div { get; set; }

        [Column(Name = "Banned")]
        public int Banned { get; set; }

        public int Vote{ get; set; }
        
        public string Stringify ()
        {
            return ID + " " +
                   Username + " " +
                   Password + " " +
                   Email + " " +
                   Points + " " +
                   Wins + " " +
                   Losses + " " +
                   Draws + " " +
                   Streak + " " +
                   Rank + " " +
                   Div + " " +
                   Banned + " " +
                   Vote;
        }

        public static User Unstringify(string user)
        {
            string[] temp = user.Split(null);
            User auser = new User();
            auser.ID = int.Parse(temp[0]);
            auser.Username = temp[1];
            auser.Password = temp[2];
            auser.Email = temp[3];
            auser.Points = int.Parse(temp[4]);
            auser.Wins = int.Parse(temp[5]);
            auser.Losses = int.Parse(temp[6]);
            auser.Draws = int.Parse(temp[7]);
            auser.Streak = int.Parse(temp[8]);
            auser.Rank = int.Parse(temp[9]);
            auser.Div = int.Parse(temp[10]);
            auser.Banned = int.Parse(temp[11]);
            auser.Vote = int.Parse(temp[12]);
            return auser;
        }

        public Bitmap Icon
        {
            get
            {
                switch (Rank)
                {
                    case 0:
                        return Properties.Resources.Member;
                    case 1:
                        return Properties.Resources.Top;
                    case 2:
                        return Properties.Resources.Admin;
                    case 3:
                        return Properties.Resources.Owner;
                    default:
                        return Properties.Resources.Member;
                 }
            }
        }
        /*
        public byte[] ToBytes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);
            byte[] bytes = ms.ToArray();
            ms.Close();

            return bytes;
        }
        public static User ToUser(byte[] userBytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(userBytes);

            return (User)bf.Deserialize(ms);
        }

        public override string ToString()
        {
            return this.Username;
        }*/
    }
    [Serializable]
    public class Game
    {
        public Game()
        {
            playerlist = new List<string>();
            Dire = new List<string>();
            Radiant = new List<string>();
        }
        public string ID { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Creator { get; set; }

        public int Count { get; set; }

        public int MinMMR { get; set; }

        public bool isChallenge { get; set; }

        public List<string> playerlist { get; set; }

        public List<string> Dire { get; set; }

        public List<string> Radiant { get; set; }

        public List<int> Votes { get; set; }

        public string Stringify()
        {
            string temp = ID + " " +
                    Name + " " +
                    Type + " " +
                    Creator + " " +
                    Count + " " +
                    MinMMR + " " +
                    isChallenge + " " +
                    "-";
            foreach (string s in playerlist)
            {
                temp += s + " ";
            }
                temp = temp.Substring(0,temp.Length - 1) + "-";
            foreach (string s in Dire)
            {
                temp += s + " ";
            }
            temp = temp.Substring(0, temp.Length - 1) + "-";
            foreach (string s in Radiant)
            {
                temp += s + " ";
            }

            return temp.Substring(0, temp.Length - 1);
        }

        public static Game Unstringify(string game)
        {
            Game temp = new Game();
            string[] splita = game.Split('-');
            string[] GamePrime = splita[0].Split(null);
            string[] GamePList = splita[1].Split(null);
            List<string> GameDire = new List<string>();
            List<string> GameRadiant = new List<string>();
            if (splita.Count() == 3)
            GameDire = splita[2].Split(null).ToList();
            if (splita.Count() == 4)
            {
                GameDire = splita[2].Split(null).ToList();
                GameRadiant = splita[3].Split(null).ToList();
            }
            temp.ID = GamePrime[0];
            temp.Name = GamePrime[1];
            temp.Type = GamePrime[2];
            temp.Creator = GamePrime[3];
            temp.Count = int.Parse(GamePrime[4]);
            temp.MinMMR = int.Parse(GamePrime[5]);
            temp.isChallenge = bool.Parse(GamePrime[6]);
            foreach (string s in GamePList)
            {
                temp.playerlist.Add(s);
            }
            foreach (string s in GameDire)
            {
                temp.Dire.Add(s);
            }
            foreach (string s in GameRadiant)
            {
                temp.Radiant.Add(s);
            }
            return temp;
        }

        public string NameID
        {
            get
            {
                if (Name == null || Name.Equals(""))
                {
                    return "ED2L" + ID;
                }
                else
                    return Name;
            }
        }

        public string Challenge
        {
            get
            {
                if (isChallenge)
                {
                    return "Y";
                }
                else
                    return "N";
            }
        }
    }
    [Serializable, XmlRoot("Sequence")]
    [Table(Name = "sqlite_sequence")]
    public class Sequence
    {
        [Column(Name = "name")]
        public String Name { get; set; }
        [Column(Name = "seq")]
        public String Seq { get; set; }
    }
    /*
    [Serializable]
    public class EncapsulatedData
    {
        public EncapsulatedData() { }
        public string Message { get; set; }
        public ISerializable Object { get; set; }
        
    }*/

    [Serializable]
    public class Packet : ISerializable
    {
        public List<string> data;
       // public int packetInt;
       // public bool packetBool;
        public string senderID;
        public PacketType packetType;

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
       {
           info.AddValue("data", data);
           info.AddValue("senderID", senderID);
           info.AddValue("packetType", packetType);

       }
       public Packet()
       { }

       public Packet(SerializationInfo info, StreamingContext ctxt)
       {
           data = (List<string>)info.GetValue("data", typeof(List<string>));
           senderID = (string)info.GetValue("senderID", typeof(string));
           packetType = (PacketType)info.GetValue("packetType", typeof(PacketType));

       }

        public Packet(PacketType type, string senderID)
        {
            data = new List<string>();
            this.senderID = senderID;
            this.packetType = type;
        }
        
        public Packet(byte[] packetBytes)
        {
            /*
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(packetBytes);
            */
            Packet p = ByteArrayToPacket(packetBytes);//(Packet)bf.Deserialize(ms);
            // ms.Close();
            if (p != null)
            {
                this.senderID = p.senderID;
                this.packetType = p.packetType;
                this.data = p.data;
            }
            //this.packetInt = p.packetInt;
            //this.packetBool = p.packetBool;
            
        }


        public byte[] ToBytes()
        {/*
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);
            byte[] bytes = ms.ToArray();
            ms.Close();

            return bytes;*/

            return PacketToByteArray(this);
        }

        private Packet ByteArrayToPacket(byte[] temp)
    {
        try
        {
            Stream objectStream = new MemoryStream(temp);

            IFormatter BinaryFormatter
                        = new BinaryFormatter();

            objectStream.Position =0;

            return (Packet)BinaryFormatter.Deserialize(objectStream); //    <--- get exception
        }
        catch (Exception ex)
        {
            ;
        }
        return null;


    }
        private byte[] PacketToByteArray(Packet p)
        {
            try
            {
                 MemoryStream objectStream = new MemoryStream();

                IFormatter BinaryFormatter
                             = new BinaryFormatter();

                BinaryFormatter.Serialize(objectStream, p);
                objectStream.Seek(0, SeekOrigin.Begin);
                return objectStream.ToArray();
            }
            catch (Exception ex)
            {
                ;
            }

            return null;
        }


        public static string GetIP4Address()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

            return (from ip in ips
                    where ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    select ip)
                   .FirstOrDefault()
                   .ToString();
        }
    }
}
