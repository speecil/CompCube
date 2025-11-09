using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.UI.BSML.Components;
using CompCube.UI.BSML.Info;
using HarmonyLib;
using HMUI;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Leaderboard;

[ViewDefinition("CompCube.UI.BSML.Leaderboard.RankingDataTabSwitcherView.bsml")]
public class RankingDataTabSwitcherViewController : BSMLAutomaticViewController
{
    [Inject] private readonly LeaderboardViewController _leaderboardViewController = null;
    [Inject] private readonly InfoViewController _infoViewController = null;
    
    [UIComponent("rankingDataTabSelector")]
    private readonly TabSelector _rankingDataTabSelector = null;
    
    [UIValue("rankingDataTabItems")]
    private readonly List<RankingDataTab> _rankingDataTabItems = [];
    
    [UIObject("rankingsTab")]
    private readonly GameObject _rankingsTab = null;
    
    [UIObject("selfTab")]
    private readonly GameObject _selfTab = null;

    [UIAction("#post-parse")]
    void PostParse()
    {
        BSMLParser.Instance.Parse(_leaderboardViewController.Content, _rankingsTab, _leaderboardViewController);
        BSMLParser.Instance.Parse(_infoViewController.Content, _selfTab, _infoViewController);
        
        _rankingDataTabSelector.TextSegmentedControl.ReloadData();
        _rankingDataTabSelector.Refresh();
    }
}