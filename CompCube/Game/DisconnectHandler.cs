using CompCube_Models.Models.Packets.ServerPackets;
using CompCube.Interfaces;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using Zenject;

namespace CompCube.Game;

public class DisconnectHandler : IInitializable, IDisposable
{
    [Inject] private readonly IServerListener _serverListener = null!;
    [Inject] private readonly MatchManager _matchManager = null!;
    
    [CanBeNull] public event Action<string, bool> ShouldShowDisconnectScreen;
    
    public void Initialize()
    {
        _serverListener.OnDisconnected += OnDisconnect;
        _serverListener.OnPrematureMatchEnd += OnPrematureMatchEnd;
    }

    private void EndLevelAndShowDisconnectScreen(string reason, bool matchOnly)
    {

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            ShouldShowDisconnectScreen?.Invoke(reason, matchOnly);
            return;
        }
        
        _matchManager.StopMatch((levelDetails, sceneTransitionSetupData) =>
        {
            ShouldShowDisconnectScreen?.Invoke(reason, matchOnly);
        });
    }

    private void OnPrematureMatchEnd(PrematureMatchEndPacket packet)
    {
        EndLevelAndShowDisconnectScreen(packet.Reason, true);
    }

    private void OnDisconnect()
    {
        EndLevelAndShowDisconnectScreen("Disconnected", false);
    }

    public void Dispose()
    {
        _serverListener.OnDisconnected -= OnDisconnect;
        _serverListener.OnPrematureMatchEnd -= OnPrematureMatchEnd;
    }
}