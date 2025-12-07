using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Extensions;
using CompCube.Game;
using Zenject;

namespace CompCube.UI.BSML.Match;

[ViewDefinition("CompCube.UI.BSML.Match.RoundResultsView.bsml")]
public class MatchResultsViewController : BSMLAutomaticViewController
{
    [Inject] private readonly MatchStateManager _stateManager = null!;
    
    [UIValue("titleBgColor")] private string TitleBgColor { get; set; } = "#0000FF";
    [UIValue("titleText")] private string TitleText { get; set; } = "You Win";

    [UIValue("mmrChangeText")] private string MmrChangeText { get; set; } = "";
    
    private Action? _continueButtonPressedCallback = null;
    
    public void PopulateData(int redScore, int blueScore, int mmrChange, Action continueButtonPressedCallback)
    {
        var winningTeam = redScore > blueScore ? MatchStateManager.Team.Red : MatchStateManager.Team.Blue;

        var won = winningTeam == _stateManager.OwnTeam;
        
        _continueButtonPressedCallback = continueButtonPressedCallback;
        
        TitleText = won ? "You Win!" : "You Lose!";
        TitleBgColor = won ? "#0000FF" : "#FF0000";

        MmrChangeText =
            $"You {(won ? "gained" : "lost")}: {mmrChange.ToString().FormatWithHtmlColor(won ? "#90EE90" : "#FF7F7F")} MMR";
            
        NotifyPropertyChanged(null);
    }

    [UIAction("continueButtonClicked")]
    private void OnContinueButtonPressed()
    {
        _continueButtonPressedCallback?.Invoke();
        _continueButtonPressedCallback = null;
    }
}