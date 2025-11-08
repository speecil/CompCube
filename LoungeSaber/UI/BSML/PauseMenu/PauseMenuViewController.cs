using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using LoungeSaber.Extensions;
using LoungeSaber.Game;
using UnityEngine;
using Zenject;

namespace LoungeSaber.UI.BSML.PauseMenu;

[ViewDefinition("LoungeSaber.UI.BSML.PauseMenu.PauseMenuView.bsml")]
public class PauseMenuViewController : BSMLAutomaticViewController, IInitializable, IDisposable, ITickable
{
    [Inject] private readonly PauseController _pauseController = null;
    
    private readonly FloatingScreen _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(50f, 50f), false, Vector3.zero, Quaternion.identity);
    
    [UIValue("matchStartingTimeText")] private string MatchStartingTimeText { get; set; }

    private DateTime? _matchStartingTime;
    
    [UIValue("opponentText")] private string OpponentText { get; set; }
    
    public void Initialize()
    {
        _pauseController._gamePause.willResumeEvent += Resumed;
        _pauseController._gamePause.didPauseEvent += Paused;
        
        _floatingScreen.SetRootViewController(this, AnimationType.None);
        _floatingScreen.gameObject.SetActive(true);
    }

    private void Paused()
    {
        _floatingScreen.transform.SetParent(_pauseController._pauseMenuManager._levelBar.transform);
        _floatingScreen.transform.localPosition = new Vector3(0f, -20f, 0f);
        
        _floatingScreen.gameObject.SetActive(true);
    }

    private void Resumed() => _floatingScreen.gameObject.SetActive(false);

    public void PopulateData(DateTime time, CompCube_Models.Models.ClientData.UserInfo opponent)
    {
        _matchStartingTime = time;
        
        OpponentText = $"{opponent.GetFormattedUserName()} - {opponent.Mmr.ToString().FormatWithHtmlColor(opponent.Division.Color)} MMR";
        NotifyPropertyChanged(nameof(OpponentText));
    }

    public void Dispose()
    {
        _pauseController._gamePause.didResumeEvent -= Resumed;
        _pauseController._gamePause.didPauseEvent -= Paused;
    }

    public void Tick()
    {
        if (_matchStartingTime == null) 
            return;

        MatchStartingTimeText = $"Match starting in {(int) (_matchStartingTime - DateTime.UtcNow).Value.TotalSeconds + 1}";
        NotifyPropertyChanged(nameof(MatchStartingTimeText));
    }
}