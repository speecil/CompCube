using CompCube_Models.Models.Map;
using HMUI;
using CompCube.Extensions;
using Zenject;

namespace CompCube.UI.ViewManagers;

public class StandardLevelDetailViewManager : ViewManager
{
    [Inject] private readonly StandardLevelDetailViewController _standardLevelDetailViewController = null;
    
    public override ViewController ManagedController => _standardLevelDetailViewController;
    
    private Action<VotingMap, List<VotingMap>, bool>? _votedCallback;

    private List<VotingMap> _votingMaps;
    public VotingMap CurrentVotingMap { get; private set; }
    
    public void SetData(VotingMap votingMap, List<VotingMap> votingMaps, Action<VotingMap, List<VotingMap>, bool> voteClickedCallback)
    {
        CurrentVotingMap = votingMap;
        _votingMaps = votingMaps;
        
        _votedCallback = voteClickedCallback;
        
        _standardLevelDetailViewController.SetData(
            votingMap.GetBeatmapLevel(), 
            true, 
            "Vote", 
            votingMap.GetBaseGameDifficultyTypeMask(), 
            votingMap.GetBeatmapLevel()?.beatmapBasicData.Keys
                .Select(i => i.characteristic)
                .Where(i => i.serializedName != "Standard")
                .ToArray()
            );
    }

    protected override void SetupManagedController()
    {
        _standardLevelDetailViewController._standardLevelDetailView.actionButton.onClick.AddListener(OnActionButtonPressed);
    }

    private void OnActionButtonPressed()
    {
        _votedCallback?.Invoke(CurrentVotingMap, _votingMaps, false);
        _votedCallback = null;
    }

    protected override void ResetManagedController()
    {
        _standardLevelDetailViewController._standardLevelDetailView.actionButton.onClick.RemoveListener(OnActionButtonPressed);
    }
}