using CompCube_Models.Models.Packets.ServerPackets;
using CompCube.Interfaces;
using HarmonyLib;
using Zenject;

namespace CompCube.Game;

public class MatchStateManager : IInitializable, IDisposable
{
    [Inject] private readonly IServerListener _serverListener = null!;

    [Inject] private readonly IPlatformUserModel _platformUserModel = null!;

    public Dictionary<CompCube_Models.Models.ClientData.UserInfo, Team> Players { get; private set; } = new();
    
    public List<CompCube_Models.Models.ClientData.UserInfo> RedTeam => Players.Where(i => i.Value == Team.Red).Select(i => i.Key).ToList();
    public List<CompCube_Models.Models.ClientData.UserInfo> BlueTeam => Players.Where(i => i.Value == Team.Blue).Select(i => i.Key).ToList();
    
    public Dictionary<Team, int> Points { get; private set; } = new();

    public Team OwnTeam => Players.First(i =>
        i.Key.UserId == _platformUserModel.GetUserInfo(CancellationToken.None).Result.platformUserId).Value;

    
    public void Initialize()
    {
        _serverListener.OnMatchCreated += HandleMatchCreated;
        _serverListener.OnUserDisconnected += HandleUserDisconnected;
        _serverListener.OnDisconnected += HandleDisconnect;
    }

    private void HandleDisconnect()
    {
        Players.Clear();
        Points.Clear();
    }

    private void HandleUserDisconnected(UserDisconnectedPacket packet)
    {
        Players.Remove(Players.First(i => i.Key.UserId == packet.UserId).Key);
    }

    private void HandleMatchCreated(MatchCreatedPacket matchCreated)
    {
        matchCreated.Red.Do(i => Players.Add(i, Team.Red));
        matchCreated.Blue.Do(i => Players.Add(i, Team.Blue));

        Points[Team.Red] = 0;
        Points[Team.Blue] = 0;
    }
    
    private void HandleRoundResults(RoundResultsPacket results)
    {
        Points[Team.Red] = results.RedPoints;
        Points[Team.Blue] = results.BluePoints;
    }

    public void Dispose()
    {
        _serverListener.OnMatchCreated -= HandleMatchCreated;
        _serverListener.OnRoundResults -= HandleRoundResults;
        _serverListener.OnUserDisconnected -= HandleUserDisconnected;
        _serverListener.OnDisconnected -= HandleDisconnect;
    }

    public enum Team
    {
        Red,
        Blue
    }
}