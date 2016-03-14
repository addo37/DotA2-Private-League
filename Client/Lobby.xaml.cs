using ServerData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for Lobby.xaml
    /// </summary>
    public partial class Lobby : Window
    {
        public static List<User> scoreList;
        public static ED2L main;
        public Game myGame;
        public string GID = "";
        public int tabsel = 0;
        public bool Voted = false;
        public bool hosted = false;
        public bool joined = false;
        public bool isStarted = false;
        public bool isDire = false;
        public bool isMuted = false;
        public bool canPick = false;
        public bool canVote = false;
        public bool isSpam = false;
        public List<string> PMs;
        public List<string> IgnoreList;
        public List<string> PickList;
        public static System.Timers.Timer aTimer;
        public static System.Timers.Timer myTimer;

        public Lobby()
        {
            InitializeComponent();
            scoreList = new List<User>();
            lstUsers.ItemsSource = ED2L.genList;
            
            SortScores();
            
            FocusManager.SetFocusedElement(this, txtSend);

            ED2L.shouldLog = false;
            this.Title= this.Title + " - " + ED2L.Me.Username;
            PMs = new List<string>();
            IgnoreList = new List<string>();
            PickList = new List<string>();
            //this.Close();
           // main.Close();

             // Create a timer
            var myTimer = new System.Timers.Timer();
            // Tell the timer what top do when it elapses
            myTimer.Elapsed += new ElapsedEventHandler(myEvent);
            // Set it to go off every five seconds
            myTimer.Interval = 120000;
            // And start it        
            myTimer.Enabled = true;

            // Implement a call with the right signature for events going off
            
        }

        private void myEvent(object source, ElapsedEventArgs e) 
        { 
            ED2L.UpdateUserList();
        }

        public void SortScores()
        {
            scoreList.Clear();
            foreach (User s in ED2L.userList)
                if (s.Div == ED2L.Me.Div)
                    scoreList.Add(s);
            scoreList = scoreList.OrderBy(i => i.Points).ToList();
            scoreList.Reverse();
            this.Dispatcher.Invoke(new Action(() =>
            {
                lstScore.ItemsSource = scoreList;
            }));
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            /*myTimer.Enabled = false;
            myTimer.Dispose();
            aTimer.Dispose();*/
            main.ED2L_Closed(sender, e);
        }

        private void txtSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Packet p;
                string channel = "";
                switch (tabsel)
                { case 0:
                        channel = "General";
                        break;
                    case 1:
                        channel = tabDiv.Header.ToString();
                        break;
                    case 2:
                        channel = "Game";
                        break;
                    case 3:
                        channel = "PM";
                        break;
                    default:
                        channel = "General";
                        break;
                }

                if (txtSend.Text.StartsWith("."))
                {
                    ExecuteCommand(txtSend.Text);
                }
                else
                {
                    if (!isMuted)
                    { if (!isSpam && !txtSend.Text.Equals(""))
                        {
                            if (!channel.Equals("PM"))
                            {
                                p = new Packet(PacketType.Chat, ED2L.ID);
                                p.data.Add(channel);
                            }
                            else
                            {
                                p = new Packet(PacketType.PM, ED2L.ID);
                                TabItem ti = tbcMain.SelectedItem as TabItem;
                                p.data.Add((string)ti.Header);
                            }

                            p.data.Add(ED2L.Me.Username);
                            p.data.Add(txtSend.Text);
                            if (myGame != null) p.data.Add(myGame.ID);
                            ED2L.socket.Send(p.ToBytes());
                            aTimer = new System.Timers.Timer(1000);
                            aTimer.Elapsed += new ElapsedEventHandler(Spam);
                            aTimer.Enabled = true;
                            isSpam = true;
                        }
                        else
                            AddChat("ED2L", "Don't spam.", -1);
                    }
                    else
                        AddChat("ED2L", "You're muted", -1);
                }
                txtSend.Text = "";
            }

        }


        private void Spam(Object source, ElapsedEventArgs e)
        {
            isSpam = false;
            aTimer.Close();
        }

        private void ExecuteCommand(string test)
        {
            var s = test.Split();
            switch (s.First())
            {
                case ".stats":
                case ".stat":
                    checkStats(s);
                    break;
                case ".chalcd":
                case ".ccd":
                case ".ccm":
                case ".chalcm":
                    Challenge(s);
                    break;
                case ".newcm":
                case ".newcd":
                case ".ncm":
                case ".ncd":
                    Create(s);
                    break;
                case ".s":
                case ".join":
                case ".sign":
                    JoinGame(s);
                    break;
                case ".lp":
                case ".listplayers":
                    ListPlayers(s);
                    break;
               /* case ".cf":
                    ConfirmGame();
                    break;*/
                case ".out":
                case ".unsign":
                    LeavePending();
                    break;
                case ".teams":
                    break;
                case ".abort":
                    AbortGame();
                    break;
                case ".win":
                case ".draw":
                case ".lose":
                    Vote(s[0]);
                    break;
                case ".top":
                    TopPlayer();
                    break;
                case ".streak":
                case ".ts":
                    HighStreak();
                    break;
                case ".botstreak":
                case ".bs":
                    BottomStreak();
                    break;
                case ".warn":
                case ".w":
                    Warn(s);
                    break;
                case ".penalty":
                case ".p":
                    Penalize(s);
                    break;
                case ".mr":
                case ".fr":
                case ".forceresult":
                    ForceResult(s);
                    break;
                //  case ".vg":
                //     break;
                //case ".voidgame":
                //     break;
                // case ".tb":
                //      break;
                //  case ".timeban":
                //     break;
                case ".untimedban":
                case ".ban":
                case ".utb":
                    BanPlayer(s);
                    break;
                case ".mute":
                    MutePlayer(s);
                    break;
                case ".unmute":
                    UnmutePlayer(s);
                    break;
                case ".kick":
                    KickPlayer(s);
                    break;
                case ".fa":
                    ForceAbort(s);
                    break;
                case ".promote":
                    PromotePlayer(s);
                    break;
                case ".demote":
                    DemotePlayer(s);
                    break;
                case ".setmotd":
                case ".motd":
                    SetMotD(s);
                    break;
                case ".pm":
                    PM(s);
                    break;
                case ".close":
                    ClosePM();
                    break;
                case ".con":
                    ConfirmChallenge();
                    break;
                case ".pick":
                    Pick(s);
                    break;
                case ".startvote":
                case ".sv":
                    StartVote(s);
                    break;
                case ".ignore":
                    IgnorePlayer(s);
                    break;
                case ".assign":
                    AssignDiv(s);
                    break;
                default:
                    WrongCommand();
                    break;
            }
        }
        
        private void AssignDiv(string[] s)
        {
            string assign = "";
            if (ED2L.Me.Rank >= 1)
            {
            if (s.Count() == 3)
            {
                if (ED2L.genList.Exists(ByName(s[1])))
                {
                    switch (s[2])
                    {
                        case "divb":
                            assign = "divb";
                            break;
                        case "diva":
                            assign = "diva";
                            break;
                        case "divs":
                            assign = "divs";
                            break;
                        default:
                            AddChat("ED2L", "Invalid division.", tabsel);
                            return;
                    }
                }
                else
                {
                    AddChat("ED2L", "User not found.", tabsel);
                    return;
                }
            }
            else
            {
                AddChat("ED2L", "Incorrect syntax.", tabsel);
                return;
            }
            Packet p = new Packet(PacketType.AssignDivision, ED2L.ID);
            p.data.Add(s[1]);
            p.data.Add(assign);
            ED2L.socket.Send(p.ToBytes());
            }
            else
            {
                AddChat("ED2L", "You don't have enough privileges.", tabsel);
            }
        }

        private void WrongCommand()
        {
            AddChat("ED2L", "This command doesn't exist.", tabsel);
        }

        private void IgnorePlayer(string[] s)
        {
            if (s.Count() == 2)
            {
                IgnoreList.Add(s[1]);
            }
            else
            {
                AddChat("ED2L", "Incorrect syntax.", tabsel);
            }
        }
        
        private void StartVote (string[] s)
        {
            if (myGame != null && myGame.Creator.Equals(ED2L.Me.Username))
            {
                Packet p = new Packet(PacketType.StartVotes, ED2L.ID);
                p.data.Add(myGame.ID);
                ED2L.socket.Send(p.ToBytes());
            }
            else
            {
                AddChat("ED2L", "You don't have enough privileges to do that.", tabsel);
            }
        }

        private void Pick(string[] s)
        {
            if (s.Count() == 2)
            {
                if (canPick)
                {
                    if (myGame.playerlist.Exists(BySName(s[1])))
                    {
                        if (!PickList.Exists(BySName(s[1])))
                        {
                            Packet p = new Packet(PacketType.ChallengePick, ED2L.ID);
                            p.data.Add(myGame.ID);
                            p.data.Add(ED2L.Me.Username);
                            p.data.Add(s[1]);
                            ED2L.socket.Send(p.ToBytes());
                            canPick = false;
                        }
                        else
                        {
                            AddChat("Server", "That player has already been picked.", 2);
                        } 
                        
                    }
                    else
                        AddChat("ED2L", "User not present in room.", tabsel);
                }
                else
                {
                    AddChat("ED2L", "You can't pick yet.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void ConfirmChallenge()
        {
            foreach (Game g in ED2L.pendingGames)
                if (g.Creator == ED2L.ChallengeG.Creator)
                {
                    ED2L.ChallengeG = null;
                    return;
                }
            if (ED2L.ChallengeG != null)
            {
                Packet p = new Packet(PacketType.NewGame, ED2L.ID);
                myGame = ED2L.ChallengeG;
                GID = myGame.ID;
                myGame.playerlist.Add(ED2L.Me.Username);
                p.data.Add(myGame.Stringify());
                ED2L.socket.Send(p.ToBytes());
                ED2L.ChallengeG = null;
            }
            else
                AddChat("ED2L", "You have no pending challenges.", -1);
        }

        private void Challenge(string[] s)
        {
            if (s.Count() == 2)
            {
                int MMRReq = 0;
                if (!hosted && !joined)
                {
                    if (ED2L.DivList.Exists(ByName(s[1])) && s[1] != ED2L.Me.Username)
                    {
                        Packet p = new Packet(PacketType.Challenge, ED2L.ID);
                        p.data.Add(s[1]);
                        myGame = new Game();
                        myGame.playerlist.Add(ED2L.Me.Username);
                        myGame.Count = 2;
                        if (s.Count() == 3)
                            if (int.TryParse(s[2], out MMRReq))
                                myGame.MinMMR = MMRReq;
                        if (s.Count() == 2)
                            myGame.MinMMR = 0;
                        myGame.Creator = ED2L.Me.Username;
                        if (s[0].Equals(".chalcm") || s[0].Equals(".ccm"))
                            myGame.Type = "CM";
                        else
                            myGame.Type = "CD";
                        myGame.isChallenge = true;
                        p.data.Add(myGame.Stringify());
                        ED2L.socket.Send(p.ToBytes());
                    }
                    else
                    {
                        AddChat("ED2L", "Incorrect syntax.", tabsel);
                    }
                }
                else
                {
                    AddChat("ED2L", "You've already hosted a game!", tabsel);
                }
            }
            else
            {
                AddChat("ED2L", "Incorrect syntax.", tabsel);
            }
        }


        private void ClosePM()
        {
            TabItem ti = tbcMain.SelectedItem as TabItem;
            if (PMs.Exists(BySName((string) ti.Header)))
            {
                tbcMain.Items.Remove(ti);
                ED2L.tbmap.Remove((string)ti.Header);
                PMs.Remove((string)ti.Header);
            }
        }

        static Predicate<string> BySName(string Name)
        {
            return delegate(string oname)
            {
                return oname.Equals(Name);
            };
        }

        private void PM (string[] s)
        {
            if (s.Count() == 3)
            {
                if (ED2L.genList.Exists(ByName(s[1])))
                {
                    Packet p = new Packet(PacketType.PM, ED2L.ID);
                    p.data.Add(ED2L.Me.Username);
                    p.data.Add(s[1]);
                    p.data.Add(s[2]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "User isn't online.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        static Predicate<User> ByName(string Name)
        {
            return delegate(User user)
            {
                return user.Username.Equals(Name);
            };
        }
         private void UnmutePlayer(string[] s)
        {
            int index = s.Count();
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Unmute, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }
        private void MutePlayer(string[] s)
        {
            int index = s.Count();
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Mute, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }


        private void DemotePlayer(string[] s)
        {
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Demote, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void PromotePlayer(string[] s)
        {
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Promote, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void ForceAbort(string[] s)
        {
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1 && (ED2L.startedGames.Exists(ByID(s[1])) || ED2L.pendingGames.Exists(ByID(s[1]))))
                {
                    Packet p = new Packet(PacketType.ForceAbort, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void BanPlayer(string[] s)
        {
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Ban, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                    KickPlayer(s);
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void KickPlayer(string[] s)
        {
            if (s.Count() == 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.Kick, ED2L.ID);
                    p.data.Add(s[1]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void Penalize(string[] s)
        {
            int num;
            if (s.Count() == 3)
            {
                if (ED2L.Me.Rank >= 1 && int.TryParse(s[2], out num))
                {
                    Packet p = new Packet(PacketType.Penalty, ED2L.ID);
                    p.data.Add(s[1]);
                    p.data.Add(s[2]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax.", tabsel);
        }

        private void ForceResult(string[] s)
        {
            int id;
            int vote;
            
            if (s.Count() == 3 && int.TryParse(s[1], out id) && int.TryParse(s[2], out vote) && ED2L.startedGames.Exists(ByID(s[1])) && vote > 0 && vote < 2)
            {
                if (ED2L.Me.Rank >= 1)
                {
                    Packet p = new Packet(PacketType.ForceResult, ED2L.ID);
                    p.data.Add(s[1]);
                    p.data.Add(s[2]);
                    ED2L.socket.Send(p.ToBytes());
                }
                else
                {
                    AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
                }
            }
            else
                AddChat("ED2L", "Incorrect syntax. Make sure you type valid ID/VOTE.", tabsel);
        }

        private void Warn (string[] s)
        {
            if (s.Count() == 2)
            {
            if(ED2L.Me.Rank >= 1)
            {
                Packet p = new Packet(PacketType.Warning, ED2L.ID);
                p.data.Add(s[1]);
                ED2L.socket.Send(p.ToBytes());
            }
            else
            {
                AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
            }}
            else
                AddChat("ED2L", "Please, specify a valid online username.", tabsel);
        }

        private void BottomStreak ()
        {
            int Streak = 0;
            User temp = new User();
            foreach (User s in scoreList)
                if (s.Streak < Streak)
                {
                    temp = s;
                    Streak = s.Streak;
                }
            AddChat("ED2L", "Player " + temp.Username + " has the lowest streak(" + temp.Streak + ") in the division.", tabsel);
        }

        private void HighStreak ()
        {
            int Streak = 0;
            User temp = new User();
            foreach (User s in scoreList)
                if (s.Streak > Streak)
                {
                    temp = s;
                    Streak = s.Streak;
                }
            AddChat("ED2L", "Player " + temp.Username + " has the highest streak(" + temp.Streak + ") in the division.", tabsel);
        }
        private void TopPlayer ()
        {
            AddChat("ED2L", "The top player in your division is " +  scoreList.First() + " with " + scoreList.First().Points + " Points.", tabsel);
        }


        private void SetMotD(string[] s)
        {
            if (ED2L.Me.Rank >= 1)
            {
                string temp = "";
                for (int i = 1; i < s.Count(); i++)
                {
                    temp += s[i] + " ";
                }
                temp = temp.Substring(0, temp.Length - 1);
                Packet p = new Packet(PacketType.SetMotD, ED2L.ID);
                p.data.Add(temp);
                ED2L.socket.Send(p.ToBytes());
            }
            else
                AddChat("ED2L", "You don't have the privileges to do that.", tabsel);
        }

        private void ConfirmGame ()
        {
            Packet p = new Packet(PacketType.StartGame, ED2L.ID);
            p.data.Add(GID + "");
            p.data.Add(ED2L.Me.Username);
            ED2L.socket.Send(p.ToBytes());
        }
        private void ListPlayers(string[] s)
        {
            int idi = 0;
            
            if (s.Count() == 2 && int.TryParse(s[1], out idi))
            {
                if (idi >= 1 && idi < 100)
                {
                    if (ED2L.pendingGames.Exists(ByID(s[1])))
                    {
                        string temp = "Players in game " + s[1] + " are:";
                        foreach (string y in ED2L.pendingGames.Find(ByID(s[1])).playerlist)
                            temp += " " + y;
                        temp += ".";
                        AddChat("ED2L", temp, tabsel);
                    }
                    else
                        AddChat("ED2L", "Please specify an existing game ID.", tabsel);
                }
                else
                    AddChat("ED2L", "Please specify a valid game ID.", tabsel);
            }
            else
                AddChat("ED2L", "Please specify a valid game ID.", tabsel);
        }


        static Predicate<Game> ByID(string ID)
        {
            return delegate(Game game)
            {
                return game.ID.Equals(ID);
            };
        }

        private void JoinGame(string[] s)
        {
            int idi;
            if (!joined && !hosted)
            {
                Packet p = new Packet(PacketType.JoinGame, ED2L.ID);
                if (s.Count() == 2)
                {
                    if (int.TryParse(s[1], out idi))
                    {
                        if (idi >= 1 && idi < 100 && ED2L.pendingGames.Exists(ByID(s[1])))
                        {
                            p.data.Add(idi + "");
                            p.data.Add(ED2L.Me.Username);
                            ED2L.socket.Send(p.ToBytes());
                        }
                        else
                            AddChat("ED2L", "Game ID doesn't exist.", tabsel);
                    }
                    else
                        AddChat("ED2L", "Game ID doesn't exist.", tabsel);
                }
                else
                {
                    if (ED2L.pendingGames.Count == 0)
                    {
                        AddChat("ED2L", "No pending games are present.", tabsel);
                    }
                    else
                    {
                        p.data.Add(ED2L.pendingGames.FirstOrDefault().ID);
                        p.data.Add(ED2L.Me.Username);
                        ED2L.socket.Send(p.ToBytes());
                    }
                }
            }
            else
                AddChat("ED2L", "You've already joined a game!", tabsel);
            
        }
        private void LeavePending()
        {
            Packet p = new Packet();

            if (ED2L.pendingGames.Exists(ByID(GID)))
            {
                if (ED2L.pendingGames.Find(ByID(GID)).Creator == ED2L.Me.Username)
                    p = new Packet (PacketType.DeleteGame, ED2L.ID);
                else
                    p = new Packet(PacketType.UnsignGame, ED2L.ID);

                p.data.Add(GID);
                p.data.Add(ED2L.Me.Username);
                ED2L.socket.Send(p.ToBytes());
            }
        }
        private void Vote (string vote)
        {
            if (!Voted)
            {
                if (isStarted)
                {
                    if (canVote)
                    {
                        Packet p = new Packet(PacketType.Vote, ED2L.ID);
                        p.data.Add(GID + "");
                        
                        switch (vote)
                        {
                            case ".win":
                                if (isDire)
                                    p.data.Add(0 + "");
                                else
                                    p.data.Add(1 + "");
                                break;
                            case ".draw":
                                p.data.Add(2 + "");
                                break;
                            case ".lose":
                                if (isDire)
                                    p.data.Add(1 + "");
                                else
                                    p.data.Add(0 + "");
                                break;
                        }
                        p.data.Add(ED2L.Me.Username);
                        ED2L.socket.Send(p.ToBytes());
                        Voted = true;
                        canVote = false;
                    }
                    else
                    {
                        AddChat("ED2L", "Voting hasn't started yet.", 2);
                    }
                }
                else
                    AddChat("ED2L", "You're not in a game to vote.", tabsel);
            }
            
            else
                AddChat("ED2L", "You've already voted.", tabsel);
        }

        private void AbortGame()
        {
            if (hosted)
            {
                var game = (from g in ED2L.pendingGames where g.Creator == ED2L.Me.Username select g).FirstOrDefault();
                {
                    Packet p = new Packet(PacketType.DeleteGame, ED2L.ID);
                    p.data.Add(game.ID);
                    p.data.Add(ED2L.Me.Username);
                    ED2L.socket.Send(p.ToBytes());
                }
            }
            else
                AddChat("ED2L", "You don't have a hosted game!", tabsel);
        }

        private void Create(string[] s)
        {
            int MMRReq = 0;
            string name = "";
            if (!hosted && !joined)
            {
                if (s.Count() == 2)
                    name = s[1];
            }
            else
            {
                AddChat("ED2L", "You've already hosted/joined a game!", tabsel);
                return;
            }

            Packet p = new Packet(PacketType.NewGame, ED2L.ID);
            myGame = new Game();
            if (!name.Equals(""))
            {
                myGame.Name = name;
            }
            myGame.playerlist.Add(ED2L.Me.Username);
            myGame.Count = 1;
            myGame.Creator = ED2L.Me.Username;
            if (s[0].Equals(".newcm") || s[0].Equals(".ncm"))
                myGame.Type = "CM";
            else
                myGame.Type = "CD";
            myGame.isChallenge = true;
            myGame.isChallenge = false;
            if (s.Count() == 3)
                if (int.TryParse(s[2], out MMRReq))
                    myGame.MinMMR = MMRReq;
            if (s.Count() == 2)
                myGame.MinMMR = 0;
            p.data.Add(myGame.Stringify());
            ED2L.socket.Send(p.ToBytes());
            txtSend.Text = "";
            hosted = true;
        }

        private void checkStats(string[] s)
        {
            if (s.Count() == 2)
            {
                var userx = (from user in ED2L.userList where user.Username == s[1] select user).FirstOrDefault();
                if (userx != null)
                {
                    AddChat("ED2L", s[1] + " :: " + userx.Points + " Points - " + userx.Wins + " Wins - " + userx.Losses + " Losses - " + userx.Streak + " Streak.", -1);
                }
                else
                    AddChat("ED2L", "User doesn't exist.", -1);
            }
            else
                AddChat("ED2L", ED2L.Me.Username + " :: " + ED2L.Me.Points + " Points - " + ED2L.Me.Wins + " Wins - " + ED2L.Me.Losses + " Losses - " + ED2L.Me.Streak + " Streak.", -1);
        }

        public void AddChat (string user, string say, int tabselx)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                string temp = "\n<" + DateTime.Now.ToString("HH:mm:ss") + "> " + user + ": " + say;
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                // Describes the brush's color using RGB values. 
                // Each value has a range of 0-255.

                switch (user)
                {
                    case "Server":
                        mySolidColorBrush.Color = System.Windows.Media.Color.FromArgb(255, 255, 0, 0);
                        break;
                    case "ED2L":
                        mySolidColorBrush.Color = System.Windows.Media.Color.FromArgb(255, 255, 0, 255);
                        break;
                    case "Message of the Day":
                        mySolidColorBrush.Color = System.Windows.Media.Color.FromArgb(255, 255, 255, 0);
                        break;
                    default:
                        mySolidColorBrush.Color = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
                        break;


                }
            if (tabselx == -1)
                tabselx = tabsel;
            switch (tabselx)
            {
                case 0:
                    AppendRtfText(temp, mySolidColorBrush, txtGen);
                    txtGen.ScrollToEnd();
                    break;
                case 1:
                    AppendRtfText(temp, mySolidColorBrush, txtDiv);
                    txtDiv.ScrollToEnd();
                    break;
                case 2:
                    AppendRtfText(temp, mySolidColorBrush, txtGame);
                    txtGame.ScrollToEnd();
                    break;
                default:
                    AppendRtfText(temp, mySolidColorBrush, txtGen);
                    txtGen.ScrollToEnd();
                    break;
            }
            }));

            //if (user.Equals("ED2L")) txtSend.Text = "";
        }
        private void AppendRtfText(string Text, System.Windows.Media.Brush Color, RichTextBox box)
        {
            TextRange range = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            range.Text = Text;
            range.ApplyPropertyValue(TextElement.ForegroundProperty, Color);
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            Game game = (Game)item.Content;
            string[] s = new string[2];
            s[0] = ".join";
            s[1] = game.ID;
            JoinGame(s);
        }

        protected void HandleDoubleClick1(object sender, MouseButtonEventArgs e)
        {
            string s = "";
            User user = (User)lstUsers.SelectedItem;
            if (user.Username.Length >= 4)
                if (user.Username.Substring(user.Username.Length - 4, 4) == " (R)")
                    s = ".stats " + user.Username.Substring(0, user.Username.Length - 4);
                else
                    s = ".stats " + user.Username;
            ExecuteCommand(s);
        }
        protected void HandleDoubleClick2(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            string player = (string)item.Content;
            string[] s = new string[2];
            s[0] = ".stats";
            s[1] = player;
            string[] test = s[1].Split(null);
            if (test.Count() > 1)
                s[1] = s[1].Substring(0, s[1].Length - 4);
            checkStats(s);
        }
        
        private void tabDiv_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tabsel = 1;
            lstUsers.ItemsSource = ED2L.DivList;
            lstGUsers.Visibility = Visibility.Collapsed;
            lstUsers.Visibility = Visibility.Visible;
        }

        private void tabGen_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tabsel = 0;
            lstUsers.ItemsSource = ED2L.genList;
            lstGUsers.Visibility = Visibility.Collapsed;
            lstUsers.Visibility = Visibility.Visible;
        }

        private void tabGame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tabsel = 2;
            lstGUsers.Visibility = Visibility.Visible;
            lstUsers.Visibility = Visibility.Collapsed;
        }
        
        public void lstPending_Left()
        {
            /*ED2L.magic = new MicroTimer();
            ED2L.magic.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(dispatcherTimer_Tick_Out);
            ED2L.magic.Interval = 1000;
            ED2L.magic.Enabled = true;*/
            lstPending.Visibility = Visibility.Collapsed;
            txbPending.Visibility = Visibility.Collapsed;
            lstDire.Visibility = Visibility.Visible;
            lstRadiant.Visibility = Visibility.Visible;
        }

        private void dispatcherTimer_Tick_Out(object sender, EventArgs e)
        {
            /*
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (lstDire.Margin.Top < 25)
                {
                    lstDire.Margin = new System.Windows.Thickness(lstDire.Margin.Left, lstDire.Margin.Right, lstDire.Margin.Top + 1, lstDire.Margin.Bottom);
                    lstRadiant.Margin = new System.Windows.Thickness(lstRadiant.Margin.Left, lstRadiant.Margin.Right, lstRadiant.Margin.Top + 1, lstRadiant.Margin.Bottom);
                }
                else
                {
                    ED2L.magic.Enabled = false;
                    ED2L.magic.Abort();
                    lstPending.Visibility = Visibility.Hidden;
                    txbPending.Visibility = Visibility.Hidden;
                }
            }));*/
        }

        public void lstPending_Right()
        {/*
            ED2L.magic = new MicroTimer();
            ED2L.magic.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(dispatcherTimer_Tick_In);
            ED2L.magic.Interval = 1000;
            ED2L.magic.Enabled = true;*/

            lstPending.Visibility = Visibility.Visible;
            txbPending.Visibility = Visibility.Visible;
            lstDire.Visibility = Visibility.Collapsed;
            lstRadiant.Visibility = Visibility.Collapsed;
        }

        private void dispatcherTimer_Tick_In(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (lstDire.Margin.Top > -126)
                {
                    lstDire.Margin = new System.Windows.Thickness(lstDire.Margin.Left, lstDire.Margin.Right, lstDire.Margin.Top - 1, lstDire.Margin.Bottom);
                    lstRadiant.Margin = new System.Windows.Thickness(lstRadiant.Margin.Left, lstRadiant.Margin.Right, lstRadiant.Margin.Top - 1, lstRadiant.Margin.Bottom);
                }
                else
                {
                    ED2L.magic.Enabled = false;
                    ED2L.magic.Abort();
                    lstPending.Visibility = Visibility.Visible;
                    txbPending.Visibility = Visibility.Visible;
                }
            }));
        }

        private void MenuItemPM_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".pm " + user.Username + " s";
            ExecuteCommand(s);

        }

        private void MenuItemStats_Click(object sender, RoutedEventArgs e)
        {
            string s = "";
            User user = (User)lstUsers.SelectedItem;
            if (user.Username.Length >= 4)
                if (user.Username.Substring(user.Username.Length - 4, 4) == " (R)")
                    s = ".stats " + user.Username.Substring(0, user.Username.Length - 4);
                else
                    s = ".stats " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemKick_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".kick " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemBan_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".ban " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemDemote_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".demote " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemPromote_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".promote " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemAssignDivB_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".assign " + user.Username + " divb";
            ExecuteCommand(s);
        }

        private void MenuItemAssignDivA_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
             string s = ".assign " + user.Username + " diva";
             ExecuteCommand(s);
        }

        private void MenuItemAssignDivS_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".assign " + user.Username + " divs";
            ExecuteCommand(s);
        }

        private void MenuItemMute_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".mute " + user.Username;
            ExecuteCommand(s);
        }

        private void MenuItemUnmute_Click(object sender, RoutedEventArgs e)
        {
            User user = (User)lstUsers.SelectedItem;
            string s = ".unmute " + user.Username;
            ExecuteCommand(s);
        }

    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Bitmap)
            {
                var stream = new MemoryStream();
                ((Bitmap)value).Save(stream, ImageFormat.Png);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();

                return bitmap;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
