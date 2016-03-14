using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerData;

using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using System.Data.SQLite;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
namespace Server
{
    static class Methods
    {
        public static IEnumerable<T[]> Combinations<T>(this IList<T> argList, int argSetSize)
        {
            if (argList == null) throw new ArgumentNullException("argList");
            if (argSetSize <= 0) throw new ArgumentException("argSetSize Must be greater than 0", "argSetSize");
            return combinationsImpl(argList, 0, argSetSize - 1);
        }

        private static IEnumerable<T[]> combinationsImpl<T>(IList<T> argList, int argStart, int argIteration, List<int> argIndicies = null)
        {
            argIndicies = argIndicies ?? new List<int>();
            for (int i = argStart; i < argList.Count; i++)
            {
                argIndicies.Add(i);
                if (argIteration > 0)
                {
                    foreach (var array in combinationsImpl(argList, i + 1, argIteration - 1, argIndicies))
                    {
                        yield return array;
                    }
                }
                else
                {
                    var array = new T[argIndicies.Count];
                    for (int j = 0; j < argIndicies.Count; j++)
                    {
                        array[j] = argList[argIndicies[j]];
                    }

                    yield return array;
                }
                argIndicies.RemoveAt(argIndicies.Count - 1);
            }
        }
    }

    class Server
    {
        static System.Data.Linq.DataContext cont;
        static SQLiteConnection con;
        static Socket listenerSocket;
        public static List<ClientData> _clients;
        static System.Data.Linq.Table<User> Users;
        static List<User> onlineUsers;
        static int ID;
        static List<Game> startedGames;
        static List<Game> pendingGames;
        static List<ClientData> genList;
        static List<ClientData> divsList;
        static List<ClientData> divaList;
        static List<ClientData> divbList;
        static Dictionary<string, ClientData> clientmap;
        static Dictionary<string, User> usermap;
        public static Dictionary<string, string> chmap;
        static string MotD = "Welcome to the exclusive ED2L server!";
        static List<int> IDG;
        private static readonly object myLock = new object();
        private static readonly object myLock2 = new object();
        private static readonly object myLock3 = new object();
        private static readonly object myLock4 = new object();
        private static readonly object myLock5 = new object();
        private static readonly object myLock6 = new object();
        public static List<string> Blacklist;

        /// <summary>
        /// start server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server...");
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new List<ClientData>();
            startedGames= new List<Game>();
            pendingGames = new List<Game>();
            onlineUsers = new List<User>();
            genList = new List<ClientData>();
            divaList = new List<ClientData>();
            divbList = new List<ClientData>();
            divsList = new List<ClientData>();
            
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.GetIP4Address()), 4242);
            listenerSocket.Bind(ip);
            Console.WriteLine("Success... Listening IP: " + Packet.GetIP4Address() + ":4242");
            
            if (File.Exists("ed2l.sqlite"))
            {
                con = new SQLiteConnection(
            @"Data Source=" + System.Environment.CurrentDirectory + "\\ed2l.sqlite");
                cont = new System.Data.Linq.DataContext(con);

            Users = cont.GetTable<User>();
            ID = Users.ToList().Last().ID;
            
            }
            else
            {
                con = new SQLiteConnection(
            @"Data Source=" + System.Environment.CurrentDirectory + "\\ed2l.sqlite");
                cont = new System.Data.Linq.DataContext(con);

                con.Open();

                SQLiteCommand sqlite_cmd = con.CreateCommand();

                sqlite_cmd.CommandText = "CREATE TABLE `users` (`ID`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,`Username`	TEXT NOT NULL UNIQUE,`Password`	TEXT NOT NULL,`Email`	TEXT NOT NULL,`Points`	INTEGER DEFAULT 1000,`Wins`	INTEGER DEFAULT 0,`Losses`	INTEGER DEFAULT 0,`Draws`	INTEGER DEFAULT 0,`Streak`	INTEGER DEFAULT 0,`Rank`	INTEGER NOT NULL DEFAULT 0,`Div`	INTEGER NOT NULL DEFAULT 0,`Banned`	INTEGER DEFAULT 0);";

                sqlite_cmd.ExecuteNonQuery();

                cont = new System.Data.Linq.DataContext(con);
                
                Users = cont.GetTable<User>();
                User s = new User();
                s.Username = "root"; 
                s.Email = "elite_l33t@hotmail.com";
                s.Password = BCrypt.HashPassword("root1337", BCrypt.GenerateSalt(12));
                s.Div = 5;
                s.Rank = 2;
                Users = cont.GetTable<User>();
                Users.InsertOnSubmit(s);
                cont.SubmitChanges();
                ID = 0;
            }
            IDG = new List<int>();
            Blacklist = new List<string>();
            clientmap = new Dictionary<string, ClientData>();
            chmap = new Dictionary<string, string>();
            usermap = new Dictionary<string, User>();
            if(!File.Exists("Blacklist.txt"))
            {
                File.Create("Blacklist.txt");
            }
            else
            {
                string temp = File.ReadAllText("Blacklist.txt");
                Blacklist = Regex.Split(temp, "\r\n").ToList();
            }

            if (!File.Exists("Gamelist.txt"))
            {
                File.Create("Gamelist.txt");
            }

            if (!File.Exists("Error.txt"))
            {
                File.Create("Error.txt");
            }

            Thread listenThread = new Thread(ListenThread);
            listenThread.Start();
        }

        static void ListenThread()
        {
            while(true)
            {
                listenerSocket.Listen(0);
                _clients.Add(new ClientData(listenerSocket.Accept()));
            }
        }

         public static void Data_IN(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;

            byte[] Buffer;
            int readBytes;

            while(true)
            {
                try
                {
                    Buffer = new byte[clientSocket.SendBufferSize];
                    readBytes = clientSocket.Receive(Buffer);

                    if (readBytes > 0)
                    {
                        Packet p = new Packet(Buffer);
                        DataManager(p);
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Client Disconnected.");
                    ClearClient(clientSocket);
                    break;
                }
            }
        }

        public static void ClearClient (Socket c)
        {
            ClientData sad = null;
            try
            {
            for (int i = 0; i < _clients.Count; i++)
            {
                if (_clients[i].clientSocket == c)
                {
                    foreach (var cd in clientmap)
                        if (cd.Value.id.Equals(_clients[i].id))
                        {
                            sad = cd.Value;
                            Console.WriteLine("Before Delete packet.");
                            Packet p = new Packet(PacketType.CloseConnection, cd.Value.id);
                            p.data.Add(cd.Key);
                            p.data.Add(usermap[cd.Key].Div + "");

                            Console.WriteLine("Before usermap Remove.");
                            usermap.Remove(cd.Key);

                            
                           // SendDeletePacket(p);
                            SendServerPacket(p);
                            Console.WriteLine("Before Div Remove");
                            for (int j = 0; j < onlineUsers.Count; j++)
                                if (onlineUsers[j].Username == cd.Key)
                                    onlineUsers.RemoveAt(j);
                            if (!divaList.Remove(cd.Value))
                                if (!divbList.Remove(cd.Value))
                                    divsList.Remove(cd.Value);
                           // _clients.RemoveAt(i);
                            Console.WriteLine("Before remove client from list.");
                            RemoveClientFromList(cd.Value);
                            Console.WriteLine("Before delete game.");
                            DeleteGames(cd.Key);
                            Console.WriteLine("Before closing connection.");
                            CloseClientConnection(cd.Value);
                            Console.WriteLine("Before clientmap remove.");
                            clientmap.Remove(cd.Key);
                            Console.WriteLine("Before Abort.");
                            AbortClientThread(sad);
                            break;
                        }
                    break;
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something wrong happened while removing client (socket).");
            }
            
        }
        public static void DeleteGames (string name)
        {
            Packet p;
            for (int i = 0; i < pendingGames.Count; i++)
            {
                if (pendingGames[i].Creator == name)
                {
                    if (pendingGames[i].isChallenge) chmap.Remove(pendingGames[i].Creator);
                    p = new Packet(PacketType.DeleteGame, "server");
                    p.data.Add(pendingGames[i].ID);
                    pendingGames.RemoveAt(i);
                    SendServerPacket(p);
                }
            }

            for (int i = 0; i < startedGames.Count; i++)
            {
                if (startedGames[i].Creator == name)
                {
                    if (startedGames[i].isChallenge) chmap.Remove(startedGames[i].Creator);
                    p = new Packet(PacketType.DeleteGame, "server");
                    p.data.Add(startedGames[i].ID);

                    startedGames.RemoveAt(i);
                    SendServerPacket(p);
                }
            }
        }

        public static void ClearClient(Packet p)
        {
            var exitClient = GetClientByID(p);
            try
            {
                usermap.Remove(p.data[0]);
                CloseClientConnection(exitClient);
                RemoveClientFromList(exitClient);
                if (!p.senderID.Equals("none"))
                {
                    clientmap.Remove(p.data[0]);
                    int index = onlineUsers.FindIndex(ByName(p.data[0]));
                    onlineUsers.RemoveAt(index);
                    RemoveClientFromDivList(exitClient, p.data[1]);
                    SendServerPacket(p);
                }
                DeleteGames(p.data[0]);
                Console.WriteLine("Client removed.");
                AbortClientThread(exitClient);
            }catch
            {
                Console.WriteLine("Something went wrong while removing client.");
            }
            
        }
        private static void RemoveClientFromDivList(ClientData CD, string Div)
        {
            switch(Div)
            { 
                case "0":
                    divbList.Remove(CD);
                    break;
                case "1":
                    divaList.Remove(CD);
                    break;
                case "2":
                    divsList.Remove(CD);
                    break;
                default:
                    break;
            }
        }

        public static void DataManager(Packet p)
        {
            Console.WriteLine("Processing packet...");
            switch (p.packetType)
            {
                case PacketType.Chat:
                    Chat(p);
                    break;
                case PacketType.Login:
                    CheckUser(p);
                    break;
                case PacketType.Registration:
                    NewUser(p);
                    break;
                case PacketType.RequestPending:
                    RequestPending(p);
                    break;
                case PacketType.RequestStarted:
                    RequestStarted(p);
                    break;
                case PacketType.NewGame:
                    NewGame(p);
                    break;
                case PacketType.JoinGame:
                    JoinGame(p);
                    break;
                case PacketType.UnsignGame:
                    UnsignGame(p);
                    break;
                case PacketType.DeleteGame:
                    DeleteGame(p);
                    break;
                case PacketType.SetMotD:
                    setMotD(p);
                    break;
                case PacketType.Warning:
                    Warn(p);
                    break;
                case PacketType.Vote:
                    Vote(p);
                    break;
                case PacketType.Ban:
                    Ban(p);
                    break;
                case PacketType.Mute:
                    Mute(p);
                    break;
                case PacketType.Unmute:
                    Unmute(p);
                    break;
                case PacketType.Kick:
                    Kick(p);
                    break;
                case PacketType.Promote:
                    Promote(p);
                    break;
                case PacketType.Demote:
                    Demote(p);
                    break;
                case PacketType.PM:
                    PMChat(p);
                    break;
                case PacketType.CloseConnection:
                    ClearClient(p);
                    break;
                case PacketType.Challenge:
                    Challenge(p);
                    break;
                case PacketType.ChallengePick:
                    ChallengePick(p);
                    break;
                case PacketType.StartVotes:
                    CanVote(p);
                    break;
                case PacketType.AssignDivision:
                    AssignDivision(p);
                    break;
                case PacketType.UpdateUserList:
                    UpdateUserList(p);
                    break;
                case PacketType.ForceResult:
                    ForceResult(p);
                    break;
                case PacketType.ForceAbort:
                    ForceAbort(p);
                    break;
                    
            }
        }

        public static void ForceAbort(Packet p)
        {
            if (pendingGames.Exists(ByID(p.data[0])) || startedGames.Exists(ByID(p.data[0])))
            {
                p.packetType = PacketType.DeleteGame;
                SendServerPacket(p);
            }
        }

        public static void ForceResult(Packet p)
        {
            try{
                 if (startedGames.Exists(ByID(p.data[0])))
                 {
                     switch (p.data[1])
                     {
                         case "0":
                         GameDireWin(startedGames.Find(ByID(p.data[0])));
                         break;
                         case "1":
                         GameRadiantWin(startedGames.Find(ByID(p.data[0])));
                         break;
                         case "2":
                         GameDraw(startedGames.Find(ByID(p.data[0])));
                         break;
                         default:
                         return;
                     }
                 }
            }
            catch( Exception ex)
            {
                Console.WriteLine("Something has went wrong in ForceResult.");
            }
        }

        public static void UpdateUserList(Packet p)
        {
            lock (myLock2)
            {
                //var list = Users.ToList();
                foreach (var user in Users)
                {
                    p.data.Add(user.Stringify());
                }
                p.data.Add("end");
                //list = onlineUsers;
                foreach (var user in onlineUsers)
                {
                    p.data.Add(user.Stringify());
                }

                GetClientByID(p).clientSocket.Send(p.ToBytes());
            }
        }

        public static void AssignDivision(Packet p)
        {
            lock (myLock6)
            {
                User s = (from user in Users where user.Username == p.data[0] select user).SingleOrDefault();
                switch (p.data[1])
                {
                    case "divb":
                        s.Div = 0;
                        break;
                    case "diva":
                        s.Div = 1;
                        break;
                    case "divs":
                        s.Div = 2;
                        break;
                    default:
                        break;
                }
                cont.SubmitChanges();
                SendServerPacket(p);
            }
        }
        
        public static void CanVote (Packet p)
        {
            Game m = startedGames.Find(ByID(p.data[0]));
            foreach (string s in m.playerlist)
            {
               clientmap[s].clientSocket.Send(p.ToBytes());
            }
        }

        public static Predicate<string> BySName(string Name)
        {
            return delegate(string oname)
            {
                return oname.Equals(Name);
            };
        }

        public static void ChallengePick (Packet p)
        {
            try
            {
                int index = -1;
                if (pendingGames.Exists(ByID(p.data[0])))
                {
                    
                    index = pendingGames.FindIndex(ByID(p.data[0]));
                    if (pendingGames.ElementAt(index).Creator.Equals(p.data[1]))
                        pendingGames.ElementAt(index).Dire.Add(pendingGames.ElementAt(index).playerlist.Find(BySName(p.data[2])));
                    else
                        pendingGames.ElementAt(index).Radiant.Add(pendingGames.ElementAt(index).playerlist.Find(BySName(p.data[2])));
                    Game g = pendingGames.ElementAt(index);
                    if ((g.Dire.Count + g.Radiant.Count) == 10)
                    {
                        g.Votes = new List<int>();
                        g.playerlist.Clear();
                        foreach (string s in g.Radiant)
                            g.playerlist.Add(s);
                        foreach (string s in g.Dire)
                            g.playerlist.Add(s);
                        Packet p2 = new Packet(PacketType.CreateGame, "server");
                        p2.data.Add(g.Stringify());
                        p2.data.Add("ED2L" + new Random().Next(100, 1000));
                        SendServerPacket(p2);
                        startedGames.Add(g);
                        pendingGames.RemoveAt(index);
                        using (var file = File.AppendText("Gamelist.txt"))
                        {
                            file.WriteLine(g.Stringify() + "");
                        }
                    }
                    else
                    {
                        Packet p2 = new Packet(PacketType.CanPick, "server");
                        if (pendingGames.ElementAt(index).Creator.Equals(p.data[1]))
                            clientmap[chmap[p.data[1]]].clientSocket.Send(p2.ToBytes());
                        else
                            clientmap[g.Creator].clientSocket.Send(p2.ToBytes());
                        
                        foreach (string s in pendingGames.ElementAt(index).playerlist)
                        {
                            clientmap[s].clientSocket.Send(p.ToBytes());
                        }
                    }
                }
            }
            catch
            { }
        }

        public static void Challenge (Packet p)
        {
            if (clientmap.ContainsKey(p.data[0]))
            {
                clientmap[p.data[0]].clientSocket.Send(p.ToBytes());
            }
        }

        public static void setMotD (Packet p)
        {
            MotD = p.data[0];
            SendServerPacket(p);
        }
        public static void Unmute (Packet p)
        {
            SendServerPacket(p);
        }

        public static void Warn (Packet p)
        {
            SendServerPacket(p);
        }

        public static void RequestPending (Packet p)
        {
            Packet p2 = new Packet(PacketType.RequestPending, p.senderID);
            foreach (Game s in pendingGames)
                p2.data.Add(s.Stringify());
            clientmap[p.data[0]].clientSocket.Send(p2.ToBytes());
        }

        public static void RequestStarted(Packet p)
        {
            Packet p2 = new Packet(PacketType.RequestStarted, p.senderID);
            foreach (Game s in startedGames)
                p2.data.Add(s.Stringify());
            clientmap[p.data[0]].clientSocket.Send(p2.ToBytes());
        }


        public static void UnsignGame (Packet p)
        {
            if (pendingGames.Exists(ByID(p.data[0])))
            {
                int index = pendingGames.FindIndex(ByID(p.data[0]));
                pendingGames.ElementAt(index).playerlist.Remove(p.data[1]);
                pendingGames.ElementAt(index).Count--;
                SendServerPacket(p);
            }
        }

        public static void DeleteGame(Packet p)
        {
            Game g = (from game in pendingGames where game.ID == p.data[0] select game).FirstOrDefault();
            if (g== null)
                (from game in startedGames where game.ID == p.data[0] select game).FirstOrDefault();
            if (g != null && g.Creator == p.data[1])
            {
                if (g.isChallenge) chmap.Remove(g.Creator);
                pendingGames.Remove(g);
                SendServerPacket(p);
            }
        }

        public static void NewGame (Packet p)
        {
            Game temp = Game.Unstringify(p.data[0]);
            Console.WriteLine("New game processing...");
            Random rand = new Random();
            int ID = rand.Next(1, 100);
            while (IDG.Exists(ByIDG(ID))) ID = rand.Next(1, 100);
            temp.ID = ID + "";
            if (temp.isChallenge)
            {
                temp.Dire.Add(temp.playerlist[0]);
                temp.Radiant.Add(temp.playerlist[1]);
                chmap.Add(temp.Creator, temp.playerlist[1]);
            }
            
            pendingGames.Add(temp);

            p.data[0] = temp.Stringify();

            SendDivPacket(p, usermap[temp.Creator].Div);
        }
        static Predicate<int> ByIDG(int test)
        {
            return delegate(int id)
            {
                return id == test;
            };
        }
        static Predicate<Game> ByID(string ID)
        {
            return delegate(Game game)
            {
                return game.ID.Equals(ID);
            };
        }

        public static void JoinGame(Packet p)
        {
            lock (myLock)
            {
                var g = (from game in pendingGames where game.ID == p.data[0] select game).FirstOrDefault();
                if (g != null)
                {
                    if (!g.isChallenge)
                    {
                        if (g.Count < 10)
                        {
                            g.Count++;
                            g.playerlist.Add(p.data[1]);
                        }
                    
                        if (g.Count == 10)
                        {
                            g.Votes = new List<int>();
                            int indexc = pendingGames.FindIndex(ByID(p.data[0]));
                            int sum = 0;
                            foreach (string player in g.playerlist)
                                sum += usermap[player].Points;
                            sum /= 2;
                            var magiclist = Methods.Combinations<string>(g.playerlist, 5);
                            List<int> sums = new List<int>();
                            foreach (string[] arr in magiclist)
                                sums.Add(usermap[arr[0]].Points + usermap[arr[1]].Points + usermap[arr[2]].Points + usermap[arr[3]].Points + usermap[arr[4]].Points);
                            sums.Sort();
                            sums.Reverse();
                            List<int> differences = new List<int>();
                            for (int i = 0; i < sums.Count; i++)
                            {
                                differences.Add(sum - sums[i]);
                            }
                            int index = 0;
                            for (int i = 0; i < differences.Count; i++)
                            {
                                if (differences[i] >= 0)
                                {
                                    index = i;
                                    break;
                                }
                            }
                            var temp = magiclist.ElementAt(index);
                            foreach (string s in temp)
                                g.Radiant.Add(s);
                            foreach (string s in g.playerlist)
                                if (!g.Radiant.Exists(BySName(s)))
                                {
                                    g.Dire.Add(s);
                                }
                            Packet p2 = new Packet(PacketType.CreateGame, "server");
                            p2.data.Add(g.Stringify());
                            p2.data.Add("ED2L" + new Random().Next(100, 1000));
                            using (var file = File.AppendText("Gamelist.txt"))
                            {
                                file.WriteLine(g.Stringify() + "");
                            }
                            foreach (string s in g.playerlist)
                            {
                                clientmap[s].clientSocket.Send(p2.ToBytes());
                            }
                            startedGames.Add(g);
                            pendingGames.RemoveAt(indexc);
                            }
                        }
                        else
                        {
                            g.Count++;
                            g.playerlist.Add(p.data[1]);
                        }
                    }
                    p.data.Add(g.Stringify());
                    SendServerPacket(p);
                }
            }
        
        
        static Predicate<User> ByName(string username)
        {
            return delegate(User user)
            {
                return user.Username == username;
            };
        }
        
        
        public static void Vote (Packet p)
        {
            try
            {
                Game game = (from gamecur in startedGames where gamecur.ID == p.data[0] select gamecur).FirstOrDefault();
                if (game.Votes.Count < 7)
                {
                    game.Votes.Add(Convert.ToInt16(p.data[1]));
                    foreach (string player in game.playerlist)
                    {
                        clientmap[player].clientSocket.Send(p.ToBytes());
                    }
                }
                else
                {
                    game.Votes.Add(Convert.ToInt16(p.data[1]));
                    int[] verdict = new int[3] { 0, 0, 0 };
                    foreach (int input in game.Votes)
                        verdict[input]++;
                    //Test Conditions
                    if (verdict[0] > 5)
                        GameDireWin(game);
                    else
                        if (verdict[1] > 5)
                            GameRadiantWin(game);
                        else
                            if (verdict[2] > 5)
                                GameDraw(game);
                            else
                                ResetVote(game);
                }
            }
            catch { }
        }

        private static void ResetVote(Game game)
        {
            Packet p = new Packet(PacketType.ResetVote, "server");
            foreach (string u in game.playerlist)
            {
                clientmap[u].clientSocket.Send(p.ToBytes());
            }
                
            startedGames.Find(ByID(game.ID)).Votes = new List<int>();
        }
           
        private static void GameDireWin (Game game)
        {
            switch (game.isChallenge)
            {
                case false:
                    foreach (string player in game.Dire)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points += 40;
                                gplayer.Wins++;
                                if (gplayer.Streak < 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak++;
                                break;
                            }

                    foreach (string player in game.Radiant)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points -= 40;
                                gplayer.Losses++;
                                if (gplayer.Streak > 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak--;
                                break;
                            }
                    break;
                case true:
                    float diresum = 0;
                    float radsum = 0;
                    float ratio = 0;
                    foreach (string player in game.Dire)
                        diresum += usermap[player].Points;
                    foreach(string player in game.Radiant)
                        radsum += usermap[player].Points;
                    
                    if (diresum >= radsum)
                    {
                        ratio = diresum/radsum;
                        diresum = 80 - ratio*40;
                        radsum = ratio*40;
                    }
                    else
                    {
                        ratio = radsum/diresum;
                        diresum = ratio*40;
                        radsum = 80 - diresum;
                    }

                    foreach (string player in game.Dire)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points += (int)Math.Round(diresum);
                                gplayer.Wins++;
                                if (gplayer.Streak < 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak++;
                                break;
                            }

                    foreach (string player in game.Radiant)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points -= (int)Math.Round(radsum);
                                gplayer.Losses++;
                                if (gplayer.Streak > 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak--;
                                break;
                            }
                    break;
            }
            Packet p = new Packet(PacketType.FinishGame, "server");
            p.data.Add("DireWin");
            p.data.Add(game.ID);
            /*foreach (string player in game.playerlist)
            {
                clientmap[player].clientSocket.Send(p.ToBytes());
            }*/
            SendServerPacket(p);
            cont.SubmitChanges();
            startedGames.Remove(game);
            FreeGID(game.ID);
            if (game.isChallenge) chmap.Remove(game.Creator);
        }

        private static void FreeGID (string ID)
        {
            int IDx;
            if(Int32.TryParse(ID, out IDx))
            {
                IDG.Remove(IDx);
            }
        }
        private static void GameRadiantWin(Game game)
        {
             switch (game.isChallenge)
            {
                case false:
                    foreach (string player in game.Radiant)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points += 40;
                                gplayer.Wins++;
                                if (gplayer.Streak < 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak++;
                                break;
                            }

                    foreach (string player in game.Dire)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points -= 40;
                                gplayer.Losses++;
                                if (gplayer.Streak > 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak--;
                                break;
                            }
                    break;
                case true:
                    float diresum = 0;
                    float radsum = 0;
                    float ratio = 0;
                    foreach (string player in game.Dire)
                        diresum += usermap[player].Points;
                    foreach(string player in game.Radiant)
                        radsum += usermap[player].Points;
                    
                    if (diresum >= radsum)
                    {
                        ratio = diresum/radsum;
                        diresum = 80 - ratio*40;
                        radsum = ratio*40;
                    }
                    else
                    {
                        ratio = radsum/diresum;
                        diresum = ratio*40;
                        radsum = 80 - diresum;
                    }

                    foreach (string player in game.Radiant)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points += (int)Math.Round(diresum);
                                gplayer.Wins++;
                                if (gplayer.Streak < 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak++;
                                break;
                            }

                    foreach (string player in game.Dire)
                        foreach (User gplayer in Users)
                            if (gplayer.Username.Equals(player))
                            {
                                gplayer.Points -= (int)Math.Round(radsum);
                                gplayer.Losses++;
                                if (gplayer.Streak > 0)
                                    gplayer.Streak = 0;
                                else
                                    gplayer.Streak--;
                                break;
                            }
                     break;
            }

             Packet p = new Packet(PacketType.FinishGame, "server");
             p.data.Add("RadiantWin");
             p.data.Add(game.ID);
             /*foreach (string player in game.playerlist)
             {
                 clientmap[player].clientSocket.Send(p.ToBytes());
             }*/
             SendServerPacket(p);
            cont.SubmitChanges();
            startedGames.Remove(game);
            FreeGID(game.ID);
            if (game.isChallenge) chmap.Remove(game.Creator);
        }

        private static void GameDraw(Game game)
        {
            Packet p = new Packet(PacketType.FinishGame, "server");
            p.data.Add("GameDraw");
            p.data.Add(game.ID);
            /*foreach (string player in game.playerlist)
            {
                clientmap[player].clientSocket.Send(p.ToBytes());
            }*/
            SendServerPacket(p);
            startedGames.Remove(game);
            FreeGID(game.ID);
            if (game.isChallenge) chmap.Remove(game.Creator);
        }
        private static void Mute (Packet p)
        {
            SendServerPacket(p);
        }

        private static void Demote (Packet p)
        {
            var temp = (from user in Users where user.Username == p.data[0] select user).SingleOrDefault();
            if (temp.Rank == 1)
            {
                temp.Rank--;
                cont.SubmitChanges();
                SendServerPacket(p);
            }
        }
        private static void Promote(Packet p)
        {
            var temp = (from user in Users where user.Username == p.data[0] select user).SingleOrDefault();
            if (temp.Rank == 0)
            {
                temp.Rank++;
                cont.SubmitChanges();
                SendServerPacket(p);
            }
        }
        private static void Kick(Packet p)
        {
            SendServerPacket(p);
        }

        private static void Ban(Packet p)
        {
            IPEndPoint IP = GetClientByID(p).clientSocket.RemoteEndPoint as IPEndPoint;

            var temp = (from user in Users where user.Username == p.data[0] select user).SingleOrDefault();
            if (temp.Banned == 0)
            {
                temp.Banned = 1;
                cont.SubmitChanges();
                SendServerPacket(p);
                Blacklist.Add(IP.Address + "");
                using (var file = File.AppendText("Blacklist.txt"))
                {
                    file.WriteLine(IP.Address + "");
                }
            }
        }

        private static void AddGenDiv(User p)
        {
            genList.Add(clientmap[p.Username]);
            switch (p.Div)
            {
                case 0:
                    divbList.Add(clientmap[p.Username]);
                    break;
                case 1:
                    divaList.Add(clientmap[p.Username]);
                    break;
                case 2:
                    divsList.Add(clientmap[p.Username]);
                    break;
            }
        }
        
        private static void NewUser (Packet p)
        {
            lock (myLock5)
            {
                try
                {
                    var client = GetClientByID(p);
                    IPEndPoint IP = client.clientSocket.RemoteEndPoint as IPEndPoint;

                    if (Blacklist.Exists(BySName(IP.Address + "")))
                    {
                        p = new Packet(PacketType.Banned, "server");
                        client.clientSocket.Send(p.ToBytes());
                        return;
                    }

                    Packet p2;
                    User temp = User.Unstringify(p.data[0]);
                    var query = from user in Users where user.Username == temp.Username select user;
                    if (query.Count() != 0)
                    {
                        p2 = new Packet(PacketType.UserExists, "server");
                    }
                    else
                    {
                        temp.ID = ++ID;
                        temp.Points = 1000;
                        Users.InsertOnSubmit(temp);
                        cont.SubmitChanges();
                        p2 = new Packet(PacketType.RegOK, "server");
                    }
                    var userc = GetClientByID(p);
                    userc.clientSocket.Send(p2.ToBytes());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException + "'n" + ex.StackTrace);
                }
            }
        }
        
        public static void CheckUser(Packet p)
        {
            lock (myLock3)
            {
                try
                {
                    var client = GetClientByID(p);
                    IPEndPoint IP = client.clientSocket.RemoteEndPoint as IPEndPoint;

                    Console.WriteLine("User logging...");
                    Packet result;
                    User temp = new User();
                    var check = from user in Users where user.Username == p.data[0] select user;

                    if (check.ToList().Count != 0 && BCrypt.CheckPassword(p.data[1], check.ToList().First().Password))
                    {
                        temp = check.ToList().First();
                        if (temp.Banned == 1 || Blacklist.Exists(BySName(IP.Address + "")))
                        {
                            p = new Packet(PacketType.Banned, "server");
                            client.clientSocket.Send(p.ToBytes());
                            return;
                        }
                        if (onlineUsers.Exists(ByName(temp.Username)))
                        {
                            if (clientmap.ContainsKey(temp.Username))
                            {
                                Packet p2 = new Packet(PacketType.CloseConnection, clientmap[temp.Username].id);
                                p2.data.Add(temp.Username);
                                p2.data.Add(temp.Div + "");
                                ClearClient(p2);
                            }
                            // result = new Packet(PacketType.UserOnline, "server");
                            //
                            //return;
                        }

                        result = new Packet(PacketType.LoginY, "server");
                        clientmap.Add(temp.Username, client);
                        onlineUsers.Add(temp);
                        AddGenDiv(temp);
                        usermap.Add(temp.Username, temp);
                        result.data.Add(MotD);
                        result.data.Add(temp.Stringify());
                        
                        foreach (var user in Users)
                        {
                            result.data.Add(user.Stringify());
                        }
                        result.data.Add("end");
                        //list = onlineUsers;
                        foreach (var user in onlineUsers)
                        {
                            result.data.Add(user.Stringify());
                        }
                        MoreUsers(temp);
                    }
                    else
                        result = new Packet(PacketType.LoginN, "server");
                    client.clientSocket.Send(result.ToBytes());
                }
                catch (Exception ex)
                {
                    using (var file = File.AppendText("Error.txt"))
                    {
                        file.WriteLine(ex.StackTrace);
                    }
                }
            }
           
        }
        
        public static void MoreUsers (User temp)
        {
            Packet p = new Packet(PacketType.MoreUsers, "server");
            p.data.Add(temp.Stringify());
            foreach(User s in onlineUsers)
            {
                p.senderID = s.Div + "";
                if (!temp.Username.Equals(s.Username)) clientmap[s.Username].clientSocket.Send(p.ToBytes());
            }
        }
        public static void Chat(Packet p)
        {
            int div = 0;
            switch(p.data[0])
            {
                case "General":
                    //temp = _clients;
                    div = -1;
                    break;
                case "Division B":
                   // temp = divbList;
                    div = 0;
                    break;
                case "Division A":
                    //temp = divaList;
                    div = 1;
                    break;
                case "Division S":
                    //temp = divsList;
                    div = 2;
                    break;
                case "Game":
                    GameChat(p);
                    return;
                case "PM":
                    PMChat(p);
                    return;
                default:
                    return;
            }
            if (div == -1)
                foreach (User c in onlineUsers)
                {
                    clientmap[c.Username].clientSocket.Send(p.ToBytes());
                }
            else
                SendDivPacket(p, div);
        }

        public static void SendServerPacket(Packet p)
        {
            foreach (User c in onlineUsers)
            {
                if (clientmap[c.Username].clientSocket.Connected)
                clientmap[c.Username].clientSocket.Send(p.ToBytes());
            }
        }

            public static void SendDivPacket (Packet p, int div)
        {
            foreach (User c in onlineUsers)
            {
                if (c.Div == div)
                clientmap[c.Username].clientSocket.Send(p.ToBytes());
            }
        }

        public static void SendDeletePacket (Packet p)
        {
            foreach (ClientData c in _clients)
            {
                if (c.id != p.senderID)
                c.clientSocket.Send(p.ToBytes());
            }
        }

        public static void GameChat(Packet p)
        {
            Game temp = (from game in startedGames where game.ID == p.data[3] select game).FirstOrDefault();
            if (temp == null)
                temp = (from game in pendingGames where game.ID == p.data[3] select game).FirstOrDefault();
            if (temp != null)
            for (int i = 0; i < temp.playerlist.Count; i++)
            {
                if (clientmap.ContainsKey(temp.playerlist[i]) && clientmap[temp.playerlist[i]].clientSocket.Connected)
                clientmap[temp.playerlist[i]].clientSocket.Send(p.ToBytes());
            }
        }
        
        public static void PMChat(Packet p)
        {
            try
            {
                clientmap[p.data[1]].clientSocket.Send(p.ToBytes());
                clientmap[p.data[0]].clientSocket.Send(p.ToBytes());
            }
            catch { }
        }

        private static ClientData GetClientByID(Packet p)
        {
            return (from client in _clients
                    where client.id == p.senderID
                    select client)
                    .FirstOrDefault();
        }

        private static void CloseClientConnection(ClientData c)
        {
            c.clientSocket.Close();
        }
        private static void RemoveClientFromList(ClientData c)
        {
            _clients.Remove(c);
        }
        private static void AbortClientThread(ClientData c)
        {
            c.clientThread.Abort();
        }
    }
}
