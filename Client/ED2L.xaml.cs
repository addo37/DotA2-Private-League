using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Timers;
// using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.IO;
using ServerData;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
//using ServerData;
namespace Client
{
    public enum PasswordScore
    {
        Blank = 0,
        VeryWeak = 1,
        Weak = 2,
        Medium = 3,
        Strong = 4,
        VeryStrong = 5
    }

    public class Advisor
    {
        public static PasswordScore CheckPassStrength(string password)
        {
            int score = 1;

            if (password.Length < 1)
                return PasswordScore.Blank;
            if (password.Length < 4)
                return PasswordScore.VeryWeak;

            if (password.Length >= 8)
                score++;
            if (password.Length >= 12)
                score++;
            if (Regex.Match(password, @"/\d+/", RegexOptions.ECMAScript).Success)
                score++;
            if (Regex.Match(password, @"/[a-z]/", RegexOptions.ECMAScript).Success &&
              Regex.Match(password, @"/[A-Z]/", RegexOptions.ECMAScript).Success)
                score++;
            if (Regex.Match(password, @"/.[!,@,#,$,%,^,&,*,?,_,~,-,£,(,)]/", RegexOptions.ECMAScript).Success)
                score++;

            return (PasswordScore)score;
        }

        public static bool CheckEmail (string email)
        {
            var foo = new EmailAddressAttribute();
            return foo.IsValid(email);
        }
    }

    /// <summary>
    /// Interaction logic for ED2L.xaml
    /// </summary>
    public partial class ED2L : Window
    {
        public static bool ServerDC = false;
        public static string Link = "ed2l.shivtr.com";
        public static string Version = "2.0";
        public static System.Timers.Timer aTimer;
        public static Socket socket;
        public static IPAddress ipAddress;
        public static string ID = "none";
        public static Thread thread;
        public static Thread Logint;
        public static bool isConnected = false;
        public static bool shouldLog = false;
        public static User Me;
        public static MicroTimer magic;
        public static bool ThreadRunning = false;
        public static List<User> userList;
        public static List<User> genList;
        public static List<User> DivList;
        public static List<Game> pendingGames;
        public static List<Game> startedGames;
        public static Lobby lobby;
        public static Game ChallengeG;
        public static Dictionary<string, RichTextBox> tbmap = new Dictionary<string, RichTextBox>();

        public ED2L()
        {
            InitializeComponent();
            /*
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress.TryParse("127.0.0.1", out ipAddress);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 4242);
            socket.Connect(ipEndPoint);
            isConnected = true;*/
            //  ConnectBtn.IsEnabled = false;
            //  SendBtn.IsEnabled = true;
            pendingGames = new List<Game>();
            startedGames = new List<Game>();
            genList = new List<User>();
            DivList = new List<User>();
            userList = new List<User>();
            /*thread = new Thread(Data_IN);
            thread.Start();*/
            FocusManager.SetFocusedElement(this, txtUser);

            //Application.Current.Resources.Source = new Uri("Themes/MetroDark/IG.MSControls.Core.Implicit.xaml", UriKind.RelativeOrAbsolute);
        }


        /// <summary>
        /// on window close event update server for close connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ED2L_Closed(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (!ServerDC)
                {
                    Packet p = new Packet(PacketType.CloseConnection, ID);
                    p.data.Add(Me.Username);
                    p.data.Add(Me.Div + "");
                    socket.Send(p.ToBytes());
                }
                socket.Close();
                thread.Abort();
                ServerDC = false;
                if (lobby != null)
                    lobby.Close();
            }
            this.Close();
        }


        /// <summary>
        /// connect Button click event
        /// validate login and ip adress
        /// connect if valid and start new thread for receiving data from server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            TryConnect();
            if (isConnected && !ThreadRunning)
            {
                Logint = new Thread(Login);
                ThreadRunning = true;
                Logint.Start();
            }
        }

        private void TryConnect()
        {
            try
            {
                if (!isConnected)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress.TryParse("204.12.124.180", out ipAddress); //91.105.117.240 - 87.110.90.56 - 204.12.124.180 - 192.168.1.7 - 192.168.42.198 - 192.168.42.98
                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 4242);
                    socket.Connect(ipEndPoint);

                    //    login = Login.Text;

                    isConnected = true;
                    //  ConnectBtn.IsEnabled = false;
                    //  SendBtn.IsEnabled = true;

                    thread = new Thread(Data_IN);
                    thread.Start();
                }
            }
            catch (SocketException ex)
            {
                txbStatus.Text = "Unable to connect to server.";
            }
        }

        /// <summary>
        /// send message to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*
        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            string msg = Msg.Text;
            Msg.Text = string.Empty;

            Packet p = new Packet(PacketType.Chat, ID);
            p.data.Add(login);
            p.data.Add(msg);
            ClientStream.Write(pack, 0, pack.Length);
        }
        */

        /// <summary>
        /// receive data from socket
        /// then data received call to data manager
        /// </summary>
        private void Data_IN()
        {
            byte[] buffer;
            int readBytes;

            while (true)
            {
                try
                {
                    buffer = new byte[socket.SendBufferSize];
                    readBytes = socket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer));
                    }
                }
                catch (SocketException ex)
                {
                    ConnectionToServerLost();
                    break;
                }

            }
        }


        /// <summary>
        /// manage all received packages by PacketType
        /// </summary>
        /// <param name="p"></param>
        private void DataManager(Packet p)
        {
            switch (p.packetType)
            {
                case PacketType.Connect:
                    Connect(p);
                    break;
                case PacketType.UserExists:
                    UserExists();
                    break;
                case PacketType.RegOK:
                    RegOK();
                    break;
                case PacketType.LoginN:
                    LoginN();
                    break;
                case PacketType.LoginY:
                    LoginY(p);
                    break;
                case PacketType.UserOnline:
                    UserOnline();
                    break;
                case PacketType.MoreUsers:
                    MoreUsers(p);
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
                case PacketType.CreateGame:
                    CreateGame(p);
                    break;
                case PacketType.FinishGame:
                    FinishGame(p);
                    break;
                case PacketType.DeleteGame:
                    DeleteGame(p);
                    break;
                case PacketType.Vote:
                    Vote(p);
                    break;
                case PacketType.ResetVote:
                    ResetVote(p);
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
                case PacketType.Warning:
                    Warn(p);
                    break;
                case PacketType.Chat:
                    Chat(p);
                    break;
                case PacketType.SetMotD:
                    SetMotD(p);
                    break;
                case PacketType.PM:
                    PMChat(p);
                    break;
                case PacketType.CloseConnection:
                    UserDisconnected(p);
                    break;
                case PacketType.Challenge:
                    Challenge(p);
                    break;
                case PacketType.ChallengePick:
                    ChallengePick(p);
                    break;
                case PacketType.CanPick:
                    CanPick();
                    break;
                case PacketType.StartVotes:
                    CanVote();
                    break;
                case PacketType.AssignDivision:
                    AssignDivision(p);
                    break;
                case PacketType.UpdateUserList:
                    UpdateUserList(p);
                    break;
                case PacketType.Banned:
                    Banned();
                    break;
            }
        }

        private void Banned()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txbStatus.Text = "You're banned.";
                txbStatusN.Text = "You're banned.";
            }));
        }

        private void UpdateUserList(Packet p)
        {
            int index = 0;
            userList.Clear();
            genList.Clear();
            DivList.Clear();
            for (int i = 0; i < p.data.Count; i++)
            {
                if (p.data[i].Equals("end"))
                {
                    index = i;
                    break;
                }
                userList.Add(User.Unstringify(p.data[i]));
            }

            for (int i = index + 1; i < p.data.Count; i++)
            {
                genList.Add(User.Unstringify(p.data[i]));
            }

            foreach (User s in genList)
                if (s.Div == Me.Div)
                    DivList.Add(s);

            lobby.SortScores();

            this.Dispatcher.Invoke(new Action(() =>
            {
                lobby.lstUsers.Items.Refresh();
                lobby.lstScore.Items.Refresh();
            }));
        }

        private void AssignDivision(Packet p)
        {
            if (p.data[0] == ED2L.Me.Username)
            {
                int div = 0;

                switch (p.data[1])
                {
                    case "divb":
                        break;
                    case "diva":
                        div = 1;
                        break;
                    case "divs":
                        div = 2;
                        break;
                    default:
                        return;
                }
                Me.Div = div;
                UpdateUserList();
                RequestPending();
                RequestStarted();
                string temp = div == 0 ? "B" : div == 1 ? "A" : "S";
                lobby.AddChat("Server", "You've been set to division " + temp + ".", lobby.tabsel);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.tabDiv.Header = checkDiv();
                }));
            }
            else
            {
                UpdateUserList();
                RequestPending();
                RequestStarted();
            }
        }
        public static void UpdateUserList()
        {
            Packet p = new Packet(PacketType.UpdateUserList, ED2L.ID);
            socket.Send(p.ToBytes());
        }
        private void CanVote()
        {
            lobby.canVote = true;
            lobby.AddChat("Server", "You may now vote.", 2);
        }

        private void CanPick()
        {
            lobby.canPick = true;
            lobby.AddChat("Server", "You may now pick.", 2);
        }

        private void ChallengePick(Packet p)
        {
            lobby.PickList.Add(p.data[2]);
            lobby.AddChat("Server", p.data[1] + " picks " + p.data[2] + ".", 2);
            for (int i = 0; i < lobby.myGame.playerlist.Count; i++)
            {
                if (lobby.myGame.playerlist[i].Equals(p.data[2]))
                {
                    lobby.myGame.playerlist[i] += " (R)";
                    lobby.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstGUsers.Items.Refresh();

                    }));
                }
            }
            
        }
        
        private void Challenge (Packet p)
        {
            if (ChallengeG == null)
            {
                ChallengeG = Game.Unstringify(p.data[1]);
                lobby.AddChat("Server", ChallengeG.Creator + " has challenged you to a game of " + ChallengeG.Type + ". To confirm, please type .con.", -1);
            }
        }

        private void PMChat(Packet p)
        {
            string source = "";
            if (p.data[1].Equals(Me.Username))
            {
                source = p.data[0];
            }
            else
                source = p.data[1];

            if (!lobby.IgnoreList.Exists(BySName(source)))
            {
                lobby.Dispatcher.Invoke(new Action(() =>
                        {
                            if (!lobby.PMs.Exists(BySName(source)))
                            {
                                TabItem PM = new TabItem();
                                PM.Header = source;

                                RichTextBox tb = new RichTextBox();
                                var bc = new BrushConverter();
                                tb.Background = (Brush)bc.ConvertFrom("#FF383737");
                                tb.Foreground = (Brush)bc.ConvertFrom("#FFFFFFFF");
                                Paragraph par = new Paragraph(); 
                                par.LineHeight = 6;
                                FlowDocument fd = new FlowDocument(par); 
                                tb.Document = fd;
                                tb.IsReadOnly = true;
                                tb.Height = 200;
                                PM.MouseLeftButtonUp += new MouseButtonEventHandler(tabPM_MouseLeftButtonUp); 
                                StackPanel newChild = new StackPanel();
                                newChild.Children.Add(tb);
                                PM.Content = newChild;
                                tbmap.Add(source, tb);
                                lobby.tbcMain.Items.Add(PM);
                                lobby.PMs.Add(source);
                                   
                            }
                            else
                            {
                                tbmap[source].AppendText("\n<" + DateTime.Now.ToString("HH:mm:ss") + "> " + p.data[1] + ": " + p.data[2]);
                                tbmap[source].ScrollToEnd();
                            }

                        }));
             }
        }

        private void tabPM_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            lobby.tabsel = 3;
        }

        

        static Predicate<string> BySName(string Name)
        {
            return delegate(string oname)
            {
                return oname.Equals(Name);
            };
        }
        
        private void SetMotD (Packet p)
        {
            lobby.AddChat("Message of the Day", p.data[0], -1);
        }
        
         private void Warn(Packet p)
        {
            if (Me.Username.Equals(p.data[0]))
                lobby.AddChat("Server", "You've been warned.", -1);
        }

        private void UserOnline ()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txbStatus.Text = "This user is online.";
            }));
        }

        private void UserDisconnected(Packet p)
        {
            for (int i = 0; i < genList.Count; i++)
            {
                if (genList[i].Username.Equals(p.data[0]))
                {
                    genList.RemoveAt(i);
                    break;
                }
            }

            for (int i = 0; i < DivList.Count; i++)
            {
                if (DivList[i].Username.Equals(p.data[0]))
                {
                    DivList.RemoveAt(i);
                    break;
                }
            }
            if (lobby != null)
            lobby.Dispatcher.Invoke(new Action(() =>
            {
                lobby.lstUsers.Items.Refresh();
                lobby.AddChat("ED2L", p.data[0] + " has disconnected.", 0);
            }));
        }
        
        private void Connect (Packet p)
        {
            if (!p.data[1].Equals(Version))
                this.Dispatcher.Invoke(new Action(() =>
                {
                    txbStatus.Text = "Outdated version. Please download the latest client at: " + Link;
                    socket.Close();
                    Logint.Abort();
                    return;
                }));
            ID = p.data[0];
            isConnected = true;
        }
        
        private void ResetVote (Packet p)
        {
            lobby.Voted = false;
            lobby.AddChat("Server", "No result can be concluded. Please cast your votes again.", 2);
        }

        private void FinishGame(Packet p)
        {
            Game g = startedGames.Find(ByID(p.data[1]));
            if (g.playerlist.Exists(BySName(ED2L.Me.Username)))
            {
                switch (p.data[0])
                {
                    case "DireWin":
                        lobby.AddChat("ED2L", "Dire has won!", 1);
                        break;
                    case "RadiantWin":
                        lobby.AddChat("ED2L", "Radiant has won!.", 1);
                        break;
                    case "GameDraw":
                        lobby.AddChat("ED2L", "It is a draw!", 1);
                        break;
                }
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstPending_Right();
                }));
                ResetGame();
            }
            else
            { 
                startedGames.Remove(g);
                lobby.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstStarted.ItemsSource = startedGames;
                    lobby.lstStarted.Items.Refresh();
                }));
            }
        }

        private void ResetGame()
        {
            if (lobby.myGame != null && startedGames.Exists(ByID(lobby.myGame.ID)))
            startedGames.RemoveAt(startedGames.FindIndex(ByID(lobby.myGame.ID)));
            lobby.myGame = null;
            lobby.GID = "";
            lobby.tabsel = 1;
            lobby.Voted = false;
            lobby.hosted = false;
            lobby.joined = false;
            lobby.isStarted = false;
            lobby.isDire = false;
            lobby.PickList.Clear();
            /*aTimer = new System.Timers.Timer(1000 * 10);
            aTimer.Elapsed += new ElapsedEventHandler(CloseGame);
            aTimer.Enabled = true; */
            DisableGame();

            lobby.Dispatcher.Invoke(new Action(() =>
            {
                lobby.lstStarted.ItemsSource = startedGames;
                lobby.lstStarted.Items.Refresh();

                lobby.txtGame.Document.Blocks.Clear();
            }));
        }

        private void CloseGame(Object source, ElapsedEventArgs e)
        {
            
                aTimer.Enabled = false; 
                aTimer.Close();
            
        }
        
        private void RequestPending(Packet p)
        {
            pendingGames.Clear();

            foreach(string s in p.data)
            {
                pendingGames.Add(Game.Unstringify(s));
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                lobby.lstPending.ItemsSource = ED2L.pendingGames;
            }));
        }

        private void RequestStarted(Packet p)
        {
            startedGames.Clear();
            foreach (string s in p.data)
            {
                startedGames.Add(Game.Unstringify(s));
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                lobby.lstStarted.ItemsSource = ED2L.startedGames;
            }));
        }
        
        private void JoinGame(Packet p)
        {
            if (pendingGames.Exists(ByID(p.data[0])))
            {
                int index = pendingGames.FindIndex(ByID(p.data[0]));
                if (!pendingGames.ElementAt(index).isChallenge)
                {
                    if (pendingGames.ElementAt(index).Count < 10)
                    {
                        pendingGames.ElementAt(index).Count += 1;
                    }
                    if (pendingGames.ElementAt(index).Count == 10)
                    {
                        startedGames.Add(pendingGames.ElementAt(index));
                        pendingGames.RemoveAt(index);
                        /*this.Dispatcher.Invoke(new Action(() =>
                        {
                            lobby.lstStarted.ItemsSource = startedGames;
                            lobby.lstStarted.Items.Refresh();
                        }));*/
                    }
                    lobby.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstStarted.ItemsSource = startedGames;
                        lobby.lstStarted.Items.Refresh();
                        lobby.lstPending.Items.Refresh();
                    }));
                    if (p.data[1].Equals(ED2L.Me.Username))
                    {
                        lobby.joined = true;
                        lobby.GID = p.data[0];
                        if (pendingGames.Exists(ByID(p.data[0])))
                            lobby.AddChat("Server", "You've been enlisted for game ID#" + p.data[0] + ".", lobby.tabsel);
                    }
                    /*
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstPending.ItemsSource = ED2L.pendingGames;
                        lobby.lstStarted.ItemsSource = ED2L.startedGames;
                    }));*/
                }
                else
                {
                    Game temp = Game.Unstringify(p.data[2]);
                    pendingGames.ElementAt(index).Count += 1;

                    lobby.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstStarted.ItemsSource = startedGames;
                        lobby.lstStarted.Items.Refresh();
                        lobby.lstPending.Items.Refresh();
                    }));

                    if (temp.playerlist.Exists(BySName(Me.Username)))
                    {
                        if (lobby.PickList.Count == 0)
                        {
                            lobby.GID = temp.ID;
                            lobby.PickList.Add(temp.playerlist[0]);
                            lobby.PickList.Add(temp.playerlist[1]);
                        }
                        if (lobby.joined == false)
                        {
                            lobby.joined = true;
                            lobby.AddChat("Server", "You've been enlisted for game ID#" + temp.ID + ".", lobby.tabsel);
                        }
                        if (pendingGames.ElementAt(index).Count >= 10)
                        {
                            if (temp.Creator.Equals(ED2L.Me.Username))
                                lobby.canPick = true;

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                lobby.myGame = temp;
                                lobby.GID = lobby.myGame.ID;
                                lobby.lstPending.Items.Refresh();
                                EnableGame();

                                pendingGames.ElementAt(index).playerlist.Add(p.data[1]);
                                lobby.lstGUsers.ItemsSource = lobby.myGame.playerlist;
                            }));
                        }
                    }
                }

                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstStarted.ItemsSource = startedGames;
                    lobby.lstStarted.Items.Refresh();
                }));
                lobby.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstStarted.ItemsSource = startedGames;
                    lobby.lstStarted.Items.Refresh();
                }));
                
            }
        }

        private void UnsignGame(Packet p)
        {
            if (pendingGames.Exists(ByID(p.data[0])))
            {
                int index = pendingGames.FindIndex(ByID(p.data[0]));
                pendingGames.ElementAt(index).Count--;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstPending.Items.Refresh();
                }));
                if (p.data[1].Equals(Me.Username))
                    ResetGame();
            }
        }

        private void EnableGame()
        {
            lobby.Dispatcher.Invoke(new Action(() =>
            {
                lobby.tabGame.IsEnabled = true;
                lobby.tabGame.IsSelected = true;
                lobby.tabsel = 2;
                lobby.lstGUsers.Visibility = Visibility.Visible;
                lobby.lstUsers.Visibility = Visibility.Collapsed;

                lobby.lstStarted.ItemsSource = startedGames;
                lobby.lstStarted.Items.Refresh();
                lobby.lstPending.ItemsSource = pendingGames;
                lobby.lstPending.Items.Refresh();
             }));
        }

        private void DisableGame()
        {
            lobby.Dispatcher.Invoke(new Action(() =>
            {
                lobby.tabGame.IsEnabled = false;
                lobby.tabDiv.IsSelected = true;
                lobby.lstUsers.ItemsSource = DivList;
                lobby.tabsel = 1;
                lobby.lstGUsers.Visibility = Visibility.Collapsed;
                lobby.lstUsers.Visibility = Visibility.Visible;

                lobby.lstStarted.ItemsSource = startedGames;
                lobby.lstStarted.Items.Refresh();
                lobby.lstPending.ItemsSource = pendingGames;
                lobby.lstPending.Items.Refresh();
            }));
        }

        private void CreateGame(Packet p)
        {
            Game temp = Game.Unstringify(p.data[0]);
            if (temp.Radiant.Exists(BySName(Me.Username)) || temp.Dire.Exists(BySName(Me.Username)))
            {
                int index = -1;
                index = pendingGames.FindIndex(ByID(temp.ID));
                if (index != -1)
                {
                    lobby.myGame = temp;
                    startedGames.Add(temp);
                    pendingGames.RemoveAt(index);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstPending.Items.Refresh();
                        lobby.lstStarted.Items.Refresh();
                        EnableGame();
                        lobby.lstDire.ItemsSource = lobby.myGame.Dire;
                        lobby.lstRadiant.ItemsSource = lobby.myGame.Radiant;
                        lobby.lstGUsers.ItemsSource = lobby.myGame.playerlist;
                    }));
                }
                lobby.GID = lobby.myGame.ID;
                lobby.isStarted = true;
                string DireCaptain = "";
                int DirePoints = 0;
                foreach (string s in lobby.myGame.Dire)
                {
                    if (DivList.Find(ByName(s)).Points >= DirePoints)
                    {
                        DireCaptain = s;
                        DirePoints = DivList.Find(ByName(s)).Points;
                    }
                    if (s.Equals(ED2L.Me.Username))
                    {
                        lobby.isDire = true;
                    }
                }
                lobby.joined = true;
                string RadiantCaptain = "";
                int RadiantPoints = 0;
                foreach (string s in lobby.myGame.Radiant)
                    if (DivList.Find(ByName(s)).Points > RadiantPoints)
                    {
                        RadiantCaptain = s;
                        RadiantPoints = DivList.Find(ByName(s)).Points;
                    }
                if (temp.isChallenge)
                {
                    lobby.AddChat("ED2L", "Captain of team Dire is: " + temp.Dire[0] + ".", 2);
                    lobby.AddChat("ED2L", "Captain of team Radiant is: " + temp.Radiant[0] + ".", 2);
                }
                else
                {
                    lobby.AddChat("ED2L", "Captain of team Dire is: " + DireCaptain + ".", 2);
                    lobby.AddChat("ED2L", "Captain of team Radiant is: " + RadiantCaptain + ".", 2);
                }
                lobby.AddChat("ED2L", "Game has now been created. Please log Dota 2 and start the game formation. Good luck!", 2);
                lobby.AddChat("ED2L", "The game password is: " + p.data[1], 2);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstStarted.ItemsSource = startedGames;
                    lobby.lstPending_Left();
                    lobby.tabGame.IsSelected = true;
                }));
            }
            else
            {
                int index = -1;
                index = pendingGames.FindIndex(ByID(temp.ID));
                if (index != -1)
                {
                    startedGames.Add(temp);
                    pendingGames.RemoveAt(index);
                }

                if (lobby.joined == true && lobby.GID == temp.ID)
                {
                    DisableGame();
                    lobby.myGame = null;
                    lobby.GID = "";
                    lobby.tabsel = 1;
                    lobby.Voted = false;
                    lobby.hosted = false;
                    lobby.joined = false;
                    lobby.isStarted = false;
                    lobby.isDire = false;
                }
            }
        }

        private string GeneratePassword()
        {
            Random rand = new Random();
            return "ED2L" + rand.Next(100, 999);
        }

        private void DeleteGame(Packet p)
        {
            int index = -1;
            if (pendingGames.Exists(ByID(p.data[0])))
            {
                index = pendingGames.FindIndex(ByID(p.data[0]));
                if (index != -1)
                {
                    pendingGames.RemoveAt(index);
                    if (lobby.GID.Equals(p.data[0]))
                    {
                        ChallengeG = null;
                        lobby.joined = false;
                        lobby.hosted = false;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            DisableGame();
                            lobby.txtGame.Document.Blocks.Clear();
                        }));
                        lobby.myGame = null;
                    }
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstPending.Items.Refresh();
                    }));
                }
            }
            else
            {
                if (startedGames.Exists(ByID(p.data[0])))
                {
                    index =  startedGames.FindIndex(ByID(p.data[0]));
                    if (index != -1)
                    {
                        if (lobby.GID.Equals(p.data[0]))
                        {
                            lobby.joined = false;
                            lobby.hosted = false;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                DisableGame();
                                lobby.txtGame.Document.Blocks.Clear();
                            }));
                            lobby.myGame = null;
                        }
                        startedGames.RemoveAt(index);
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            lobby.lstStarted.ItemsSource = startedGames;

                        }));
                    }
                }
            }

        }

        static Predicate<Game> ByID(string ID)
        {
            return delegate(Game game)
            {
                return game.ID.Equals(ID);
            };
        }

        private void Vote(Packet p)
        {
            string temp = "";
            switch(p.data[1])
            {
                case "0":
                    temp = "Dire Win";
                   break;
                case "1":
                    temp = "Radiant Win";
                    break;
                case "2":
                    temp = "Draw";
                    break;
                default:
                    temp = "Draw";
                    break;
            }
            lobby.AddChat("Server", p.data[2] + " has voted for " + temp + ".", 2);
        }

        private void Ban(Packet p)
        {
            lobby.AddChat("Server", p.data[0] + " has been banned.", lobby.tabsel);
        }
        private void Mute (Packet p)
        {
            lobby.AddChat("Server", p.data[0] + " has been muted.", lobby.tabsel);
            if (p.data[0].Equals(ED2L.Me.Username))
                lobby.isMuted = true;
        }

        private void Unmute (Packet p)
        {
            lobby.AddChat("Server", p.data[0] + " has been unmuted.", lobby.tabsel);
            if (p.data[0].Equals(ED2L.Me.Username))
                lobby.isMuted = false;
        }
        private void Kick(Packet p)
        {
            lobby.AddChat("Server", p.data[0] + " has been kicked.", lobby.tabsel);
            if (p.data[0].Equals(ED2L.Me.Username))
                this.Dispatcher.Invoke(new Action(() =>
                { lobby.Close(); }));
        }

        private void Demote(Packet p)
        {
            User temp = userList.Find(ByName(p.data[0]));
            if (temp != null)
            {
                if (temp.Rank == 1)
                {
                    userList.Find(ByName(p.data[0])).Rank = 0;
                    lobby.AddChat("Server", p.data[0] + " has been demoted.", lobby.tabsel);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstUsers.Items.Refresh();
                    }));
                }
            }
        }

        private void Promote (Packet p)
        {
            User temp =  userList.Find(ByName(p.data[0]));
            if (temp != null)
            {
                if (temp.Rank != 1)
                {
                    userList.Find(ByName(p.data[0])).Rank = 1;
                    lobby.AddChat("Server", p.data[0] + " has been promoted.", lobby.tabsel);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstUsers.Items.Refresh();
                    }));
                }
            }
        }
        static Predicate<User> ByName(string username)
        {
            return delegate(User user)
            {
                return user.Username == username;
            };
        }
         
        private void MoreUsers  (Packet p)
        {
            User temp = User.Unstringify(p.data[0]);
            genList.Add(temp);
            if (Me != null)
            {
                int div = int.Parse(p.senderID);
                if (temp.Div == div)
                    DivList.Add(temp);

                this.Dispatcher.Invoke(new Action(() =>
                {
                    lobby.lstUsers.Items.Refresh();
                    lobby.AddChat("ED2L", temp.Username + " has logged in.", 0);
                }));
                //UpdateUserList();
            }
        }
        
        private void Chat(Packet p)
        {
            if (p.senderID.Equals("server"))
                lobby.AddChat(p.data[1], p.data[2], -1);
            else
                if (p.data[0].Equals("Division A") || p.data[0].Equals("Division B") || p.data[0].Equals("Division S"))
                    lobby.AddChat(p.data[1], p.data[2], 1);
                else
                    if (p.data[0].Equals("General"))
                        lobby.AddChat(p.data[1], p.data[2], 0);
                    else
                        if (p.data[0].Equals("Game"))
                            if(lobby.myGame != null)
                                if (lobby.myGame.ID.Equals(p.data[3]))
                                    lobby.AddChat(p.data[1], p.data[2], 2);
                   
        }
        
        private void NewGame(Packet p)
        {

            Game temp = Game.Unstringify(p.data[0]);
            if (DivList.Exists(ByName(temp.Creator)))
            {
                if (temp.playerlist.Exists(BySName(Me.Username)))
                {
                    lobby.joined = true;
                }
                ED2L.pendingGames.Add(temp);
                if (temp.Creator.Equals(ED2L.Me.Username))
                    lobby.GID = temp.ID;
                this.Dispatcher.Invoke(new Action(() =>
                    {
                        lobby.lstPending.Items.Refresh();

                    }));
            }
        }


        private void RegOK()
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    txbStatusN.Text = "Registration successful!";
                }));
        }
        
        private void UserExists()
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    txbStatusN.Text = "Please choose another username.";
                }));
        }
        private void LoginN()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txbStatus.Text = "Login unsuccessful! Please review your Username/Password.";
            }));
        }
        private void LoginY(Packet p){
            
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    Me = User.Unstringify(p.data[1]);
                    txbStatus.Text = "Login successful!";
                    PopUserList(p);

                    lobby = new Lobby();
                    lobby.tabDiv.Header = checkDiv();
                        
                    Lobby.main = this;
                    Game temp = new Game();
                    this.Hide();
                    lobby.Show();
                    RequestPending();
                    RequestStarted();
                    lobby.AddChat("Message of the Day", p.data[0], -1);
                }));
                
            }
        }

        private void PopUserList(Packet p)
        {
            int index = 2;
            for (int i = 2; i < p.data.Count; i++)
            {
                if (p.data[i].Equals("end"))
                {
                    index = i;
                    break;
                }
                userList.Add(User.Unstringify(p.data[i]));
            } 
            
            for (int i = index+1; i < p.data.Count; i++)
            {
                genList.Add(User.Unstringify(p.data[i]));
            }

            foreach (User s in genList)
                if (s.Div == Me.Div)
                    DivList.Add(s);
        }

        
        private void RequestPending()
        {
            Packet p = new Packet(PacketType.RequestPending, ID);
            p.data.Add(Me.Username);
            socket.Send(p.ToBytes());
        }

        private void RequestStarted()
        {
            Packet p = new Packet(PacketType.RequestStarted, ID);
            p.data.Add(Me.Username);
            socket.Send(p.ToBytes());
        }
        
        private string checkDiv ()
        {
            if (Me.Div == 0)
                return "Division B";
            if (Me.Div == 1)
                return "Division A";
            if (Me.Div == 2)
                return "Division S";
            return "Division B";
        }
        
        private void Login()
        {
            while (true)
                if (!ID.Equals("none"))
            this.Dispatcher.Invoke(new Action(() =>
            {
                Packet login = new Packet(PacketType.Login, ID);
                login.data.Add(txtUser.Text);
                login.data.Add(txtPass.Password);
                socket.Send(login.ToBytes());
                ThreadRunning = false;
                Logint.Abort();
            }));
        }
        /*
        /// <summary>
        /// Server connection lost
        /// add message to user, close socket and thread
        /// enable to user try to connect to server
        /// </summary>*/
        private void ConnectionToServerLost()
        {
            
            socket.Close();
            
            if (isConnected)
            {
                ServerDC = true;
                if (lobby != null)
                this.Dispatcher.Invoke(new Action(() =>
                {
                    ID = "none";
                    this.Show();
                    lobby.Close();
                    this.Close();
                    txbStatus.Text = "Connection to server has been lost.";
                })); 
               
                isConnected = false;
            }
            thread.Abort();

            
        }

        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
           /* var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick_Right);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
            */
            magic = new MicroTimer();
            magic.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(dispatcherTimer_Tick_Right);
            magic.Interval = 500;
            magic.Enabled = true;
            //magic.Enabled = false;
        }

        private void dispatcherTimer_Tick_Right(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (Main.Margin.Left < 0)
                    Main.Margin = new System.Windows.Thickness(Main.Margin.Left + 1, 0, 0, 0);
                else
                    magic.Enabled = false;
            }));

            
        }
        private void dispatcherTimer_Tick_Left(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
            if (Main.Margin.Left > -440)
                Main.Margin = new System.Windows.Thickness(Main.Margin.Left - 1, 0, 0, 0);
            else
                magic.Enabled = false;
            }));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            magic = new MicroTimer();
            magic.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(dispatcherTimer_Tick_Left);
            magic.Interval = 500;
            magic.Enabled = true;
        }

        private void btnRegN_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckParameters())
                return;
            TryConnect();
            if (isConnected)
            {
                User temp = new User();
                String hash = BCrypt.HashPassword(txtPassN.Password, BCrypt.GenerateSalt(12));
                temp.Password = hash;
                temp.Email = txtEmailN.Text;
                temp.Username = txtUserN.Text;
                Packet p = new Packet(PacketType.Registration, ID);
                p.data.Add(temp.Stringify());
                socket.Send(p.ToBytes());
            }
            else
                txbStatusN.Text = "Unable to connect to server.";
        }

        private bool CheckParameters()
        {

            int errcode = 0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (!txtPassN.Password.Equals(txtPassNC.Password))
                    errcode = 1;
                else
                    if ((int)Advisor.CheckPassStrength(txtPassN.Password) < 2)
                        errcode = 2;
                    else
                        if (!Advisor.CheckEmail(txtEmailN.Text))
                            errcode = 3;
                        else
                            if (String.IsNullOrWhiteSpace(txtUserN.Text) || txtUserN.Text.Length < 4)
                                errcode = 4;
                switch(errcode)
                {
                    case 0:
                        break;
                    case 1:
                        txbStatusN.Text = "Passwords don't match.";
                        return;
                    case 2:
                        txbStatusN.Text = "Password is weak.";
                        break;
                    case 3:
                        txbStatusN.Text = "Email entry is not valid.";
                        break;
                    case 4:
                        txbStatusN.Text = "Choose an appropriate Username with atleast 4 characters in length.";
                        break;
                    default:
                        txbStatusN.Text = "Invalid parameters.";
                        break;
                }
            }));

            if (errcode != 0)
                return false;
            else
                return true;
        }

        private void txtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryConnect();
                if (isConnected && !ThreadRunning)
                {
                    Logint = new Thread(Login);
                    ThreadRunning = true;
                    Logint.Start();
                }
            }
        }

        private void txtPassNC_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
            btnRegN_Click(sender, e);
            }
        }


    }
}
