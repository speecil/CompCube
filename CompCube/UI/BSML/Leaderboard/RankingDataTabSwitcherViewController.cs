using BeatSaberMarkupLanguage.Attributes;
using CompCube.UI.BSML.Components;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Leaderboard;

public class RankingDataTabSwitcherViewController : TabSwitcherViewController
{
    [Inject] private readonly LeaderboardViewController _leaderboardViewController = null!;
    [Inject] private readonly OwnRankingViewController _ownRankingViewController = null!;

    protected override List<ViewControllerTab> Tabs =>
    [
        new("Ranking", _ownRankingViewController),
        new("Leaderboard", _leaderboardViewController)
    ];
}