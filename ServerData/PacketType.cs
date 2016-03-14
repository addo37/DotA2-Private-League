using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerData
{
    public enum PacketType
    {
        Version,
        Registration,
        GeneralChat,
        UserExists,
        RegOK,
        Connect,
        Login,
        LoginY,
        LoginN,
        Chat,
        AddGenDiv,
        RequestGames,
        LeaveGame,
        NewGame,
        StartGame,
        CreateGame,
        JoinGame,
        UnsignGame,
        DeleteGame,
        ResetVote,
        FinishGame,
        Vote,
        StartVotes,
        Ban,
        Mute,
        Kick,
        Promote,
        Demote,
        CloseConnection,
        SetMotD,
        Warning,
        ForceResult,
        Penalty,
        MoreUsers,
        Unmute,
        Logout,
        RequestScoreboard,
        UserOnline,
        PM,
        Challenge,
        ConfirmChallenge,
        ChallengePick,
        CreateChallenge,
        CanPick,
        CanVote,
        RequestPending,
        RequestStarted,
        AssignDivision,
        UpdateUserList,
        Banned,
        MultipleReg,
        ForceAbort
    }
}
