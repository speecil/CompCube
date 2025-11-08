using CompCube_Models.Models.Packets;
using CompCube_Models.Models.Packets.ServerPackets;
using CompCube_Models.Models.Packets.ServerPackets.Event;

namespace LoungeSaber.Interfaces;

public interface IServerListener
{
    public event Action<MatchCreatedPacket> OnMatchCreated;
    public event Action<OpponentVotedPacket> OnOpponentVoted;
    public event Action<MatchStartedPacket> OnMatchStarting;
    public event Action<MatchResultsPacket> OnMatchResults;
    
    public event Action<OutOfEventPacket> OnOutOfEvent;
    
    public event Action OnDisconnected;
    public event Action OnConnected;
    public event Action<PrematureMatchEndPacket> OnPrematureMatchEnd;
    
    public event Action<EventStartedPacket> OnEventStarted;

    public Task Connect(string queue, Action<JoinResponsePacket> onConnectedCallback);

    public Task SendPacket(UserPacket packet);

    public void Disconnect();
}