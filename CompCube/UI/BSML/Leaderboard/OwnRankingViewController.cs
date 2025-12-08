using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Extensions;
using CompCube.Game;
using CompCube.Interfaces;
using Zenject;

namespace CompCube.UI.BSML.Leaderboard;

[ViewDefinition("CompCube.UI.BSML.Leaderboard.OwnRankingDataView.bsml")]
public class OwnRankingViewController : BSMLAutomaticViewController, IRefreshableView
{
    [Inject] private readonly IApi _api = null!;
    [Inject] private readonly UserModelWrapper _userModelWrapper = null!;
    
    [UIValue("nameText")] private string NameText { get; set; } = "default";
    [UIValue("mmrText")] private string MmrText { get; set; } = "default";
    [UIValue("rankText")] private string RankText { get; set; } = "default";
    [UIValue("winRateText")] private string WinRateText { get; set; } = "default";
    [UIValue("winstreakText")] private string WinStreakText { get; set; } = "default";
    
    private bool _loading = true;

    [UIValue("loading")]
    private bool Loading
    {
        get => _loading;
        set
        {
            _loading = value;
            NotifyPropertyChanged(null);  
        }
    }
    
    [UIValue("notLoading")] private bool NotLoading => !Loading;

    private async Task UpdateDataAsync()
    {
        Loading = true;

        var selfData = await _api.GetUserInfo(_userModelWrapper.UserId);
        
        if (selfData == null)
            return;

        Loading = false;

        NameText = selfData.GetFormattedUserName();
        MmrText = $"MMR: {selfData.Mmr} ({selfData.Division.GetFormattedDivision()})";
        RankText = $"Rank: #{selfData.Rank}";
        WinRateText = $"Win rate: {selfData.Wins}/{selfData.TotalGames} ({(float) selfData.Wins / selfData.TotalGames:F}%)";
        WinStreakText = $"Winstreak: {selfData.Winstreak} (Highest: {selfData.HighestWinstreak})";
        
        NotifyPropertyChanged(null);
    }

    public async void Refresh()
    {
        try
        {
            await UpdateDataAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Info(e);
        }
    }
}