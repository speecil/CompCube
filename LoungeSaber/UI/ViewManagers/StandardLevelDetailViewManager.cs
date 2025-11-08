using CompCube_Models.Models.Map;
using HMUI;
using LoungeSaber.Models.Map;
using Zenject;

namespace LoungeSaber.UI.ViewManagers;

public class StandardLevelDetailViewManager : ViewManager
{
    [Inject] private readonly StandardLevelDetailViewController _standardLevelDetailViewController = null;
    
    public override ViewController ManagedController => _standardLevelDetailViewController;
    
    public event Action<VotingMap, List<VotingMap>> OnMapVoteButtonPressed;

    private List<VotingMap> _votingMaps;
    public VotingMap CurrentVotingMap { get; private set; }
    
    public void SetData(VotingMap votingMap, List<VotingMap> votingMaps)
    {
        CurrentVotingMap = votingMap;
        _votingMaps = votingMaps;
        
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

    private void OnActionButtonPressed() => OnMapVoteButtonPressed?.Invoke(CurrentVotingMap, _votingMaps);

    protected override void ResetManagedController()
    {
        _standardLevelDetailViewController._standardLevelDetailView.actionButton.onClick.RemoveListener(OnActionButtonPressed);
    }
}