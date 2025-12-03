using CompCube_Models.Models.Match;
using CompCube_Models.Models.Packets;
using CompCube_Models.Models.Packets.ServerPackets;
using CompCube_Models.Models.Packets.ServerPackets.Event;
using CompCube_Models.Models.Packets.UserPackets;
using CompCube.Interfaces;
using SiraUtil.Logging;
using Zenject;

namespace CompCube.Server.Debug;

public class DebugServerListener : IServerListener
{
    [Inject] private readonly SiraLog _siraLog = null!;
    
    public event Action<MatchCreatedPacket>? OnMatchCreated;
    public event Action<PlayerVotedPacket>? OnPlayerVoted;
    public event Action<BeginGameTransitionPacket>? OnBeginGameTransition;
    public event Action<RoundResultsPacket>? OnRoundResults;
    public event Action<RoundStartedPacket>? OnRoundStarted;
    public event Action<UserDisconnectedPacket>? OnUserDisconnected;
    public event Action<MatchCreatedPacket>? OnMatchStarting;
    
    public event Action<MatchResultsPacket>? OnMatchResults;
    
    public event Action? OnDisconnected;
    public event Action? OnConnected;
    public event Action<PrematureMatchEndPacket>? OnPrematureMatchEnd;
    
    public event Action<EventStartedPacket>? OnEventStarted;
    
    public event Action<EventMapSelected>? OnEventMapSelected;
    public event Action<EventMatchStartedPacket>? OnEventMatchStarted;
    public event Action<EventClosedPacket>? OnEventClosed;
    public event Action<EventScoresUpdated>? OnEventScoresUpdated;

    private bool _isConnected;
    
    public async Task Connect(string queue, Action<JoinResponsePacket> onConnectedCallback)
    {
        await Task.Delay(1000);

        _isConnected = true;
        
        onConnectedCallback?.Invoke(new JoinResponsePacket(true, ""));
        OnConnected?.Invoke();
        _siraLog.Info("connected");

        await Task.Delay(1000);
        await SendPacket(new JoinRequestPacket(DebugApi.Self.Username, DebugApi.Self.UserId, queue));
    }

    public async Task SendPacket(UserPacket packet)
    {
        if (!_isConnected)
        {
            _siraLog.Info("tried to send packet when not connected!");
            return;
        }

        switch (packet.PacketType)
        {
            case UserPacket.UserPacketTypes.JoinRequest:
                await Task.Delay(1000);
                OnMatchCreated?.Invoke(new MatchCreatedPacket([DebugApi.Self], [DebugApi.DebugOpponent]));
                await Task.Delay(1);
                OnRoundStarted?.Invoke(new RoundStartedPacket(DebugApi.Maps, 30));
                _siraLog.Info("join request");
                break;
            case UserPacket.UserPacketTypes.Vote:
                OnPlayerVoted?.Invoke(new PlayerVotedPacket(0, DebugApi.DebugOpponent.UserId));
                await Task.Delay(1000);
                OnBeginGameTransition?.Invoke(new BeginGameTransitionPacket(DebugApi.Maps[0], 15,
                    10));
                _siraLog.Info("voted");
                break;
            case UserPacket.UserPacketTypes.ScoreSubmission:
                _siraLog.Info("score submitted");
                await Task.Delay(1000);

                var scores = new Dictionary<CompCube_Models.Models.ClientData.UserInfo, Score>
                {
                    { DebugApi.Self, new Score(0, 0, true, 0, true) },
                    {DebugApi.DebugOpponent, new Score(0, 0, true, 0, true)}
                };

                OnRoundResults?.Invoke(new RoundResultsPacket(scores, 1, 1));
                await Task.Delay(1);
                OnRoundStarted?.Invoke(new RoundStartedPacket(DebugApi.Maps, 30));

                _siraLog.Info("match results invoked");
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void Disconnect()
    {
        if (!_isConnected) return;
        
        OnDisconnected?.Invoke();
    }
}