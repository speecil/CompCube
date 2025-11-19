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
    public event Action<OpponentVotedPacket>? OnOpponentVoted;
    public event Action<MatchStartedPacket>? OnMatchStarting;
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
                if (((JoinRequestPacket)packet).Queue == "test")
                {
                    await Task.Delay(5000);
                    OnEventStarted?.Invoke(new EventStartedPacket());

                    await Task.Delay(5000);
                    OnMatchStarting?.Invoke(new MatchStartedPacket(DebugApi.Maps[0], 15,
                        10, DebugApi.DebugOpponent));
                    return;
                }

                await Task.Delay(1000);
                OnMatchCreated?.Invoke(new MatchCreatedPacket(DebugApi.Maps, DebugApi.DebugOpponent));
                _siraLog.Info("join request");
                break;
            case UserPacket.UserPacketTypes.Vote:
                OnOpponentVoted?.Invoke(new OpponentVotedPacket(0));
                await Task.Delay(1000);
                OnMatchStarting?.Invoke(new MatchStartedPacket(DebugApi.Maps[0], 15,
                    10, DebugApi.DebugOpponent));
                _siraLog.Info("voted");
                await Task.Delay(30000);
                OnDisconnected?.Invoke();
                break;
            case UserPacket.UserPacketTypes.ScoreSubmission:
                _siraLog.Info("score submitted");
                await Task.Delay(1000);
                OnMatchResults?.Invoke(new MatchResultsPacket(new MatchScore(DebugApi.Self, new Score(100000, 1f, true, 0, true)),
                    new MatchScore(DebugApi.DebugOpponent, Score.Empty), 100));

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