using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube_Models.Models.Events;
using CompCube_Models.Models.Match;
using CompCube_Models.Models.Packets.ServerPackets.Event;
using CompCube.Extensions;
using CompCube.Game;
using CompCube.Interfaces;
using Zenject;

namespace CompCube.UI.BSML.Events;

[ViewDefinition("CompCube.UI.BSML.Events.EventPointsLeaderboardView.bsml")]
public class EventPointsLeaderboardViewController : BSMLAutomaticViewController, IInitializable, IDisposable
{
    [UIComponent("eventsLeaderboard")]
    private readonly LeaderboardTableView _eventsLeaderboard = null!;

    [Inject] private readonly IServerListener _serverListener = null!;
    [Inject] private readonly UserModelWrapper _userModelWrapper = null!;

    private List<EventScore> _scores = [];
    private readonly Dictionary<UserInfo, int> _points = new();
    
    private ActiveLeaderboardScreen _activeLeaderboardScreen = ActiveLeaderboardScreen.Scores;

    public void Initialize()
    {
        _serverListener.OnEventScoresUpdated += OnEventScoresUpdated;
    }

    private void OnEventScoresUpdated(EventScoresUpdated packet)
    {
        _scores = packet.Scores;
    }

    private void UpdateLeaderboard()
    {
        if (_activeLeaderboardScreen == ActiveLeaderboardScreen.Scores)
        {
            var scores = _scores.Select(i =>
                new LeaderboardTableView.ScoreData(i.Score.Points, i.User.GetFormattedUserName(), i.Placement,
                    i.Score.FullCombo)).ToList();
            
            _eventsLeaderboard.SetScores(scores, _scores.IndexOf(_scores.First(j => j.User.UserId == _userModelWrapper.UserId)));
            return;
        }
        
        
    }

    public void Dispose()
    {
        _serverListener.OnEventScoresUpdated -= OnEventScoresUpdated;
    }

    public enum ActiveLeaderboardScreen
    {
        Scores,
        Points
    }
}