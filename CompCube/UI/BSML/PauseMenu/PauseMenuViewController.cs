using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Extensions;
using CompCube.Game;
using TMPro;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.PauseMenu;

[ViewDefinition("CompCube.UI.BSML.PauseMenu.PauseMenuView.bsml")]
public class PauseMenuViewController : BSMLAutomaticViewController, IInitializable, IDisposable, ITickable
{
    [Inject] private readonly PauseController _pauseController = null!;
    [Inject] private readonly MatchStartUnpauseController _matchStartUnpauseController = null!;
    
    private readonly FloatingScreen _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(50f, 50f), false, Vector3.zero, Quaternion.identity);

    [UIComponent("matchStartingTimeText")] private readonly TextMeshProUGUI _matchStartingTimeText = null!;

    private DateTime? _matchStartingTime;
    
    [UIValue("opponentText")] private string OpponentText { get; set; }
    
    [UIValue("pointsText")] private string PointsText { get; set; }
    
    public void Initialize()
    {
        _pauseController.didPauseEvent += Paused;
        
        _floatingScreen.SetRootViewController(this, AnimationType.None);
        _floatingScreen.gameObject.SetActive(true);
    }

    private void Paused()
    {
        _floatingScreen.transform.SetParent(_pauseController._pauseMenuManager._levelBar.transform);
        _floatingScreen.transform.localPosition = new Vector3(0f, -20f, 0f);
        
        _floatingScreen.gameObject.SetActive(true);
    }

    public void PopulateData(DateTime time, CompCube_Models.Models.ClientData.UserInfo[] red, CompCube_Models.Models.ClientData.UserInfo[] blue, int redPoints, int bluePoints)
    {
        _matchStartingTime = time;
        
        OpponentText = $"{red[0].GetFormattedUserName()} vs. {blue[0].GetFormattedUserName()}";
        PointsText = $"{redPoints} - {bluePoints}";
        NotifyPropertyChanged(null);
    }

    public void Dispose()
    {
        _pauseController._gamePause.didPauseEvent -= Paused;
    }

    public void Tick()
    {
        // dog shit
        if (!_matchStartUnpauseController.StillInStartingPauseMenu)
            _matchStartingTimeText.gameObject.SetActive(false);
        
        if (_matchStartingTime == null) 
            return;

        _matchStartingTimeText.text = $"Match starting in {(int) (_matchStartingTime - DateTime.UtcNow).Value.TotalSeconds + 1}";
    }
}