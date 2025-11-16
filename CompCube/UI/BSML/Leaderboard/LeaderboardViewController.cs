using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using BGLib.Polyglot;
using CompCube.Interfaces;
using CompCube.UI.BSML.Components;
using HMUI;
using IPA.Utilities;
using CompCube.Extensions;
using CompCube.Server;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Leaderboard
{
    [ViewDefinition("CompCube.UI.BSML.Leaderboard.LeaderboardView.bsml")]
    public class LeaderboardViewController : BSMLAutomaticViewController, IInitializable, IRefreshableView
    {
        [Inject] private readonly PlatformLeaderboardViewController _platformLeaderboardViewController = null;
        [Inject] private readonly IApi _api = null;
        [Inject] private readonly SiraLog _siraLog = null;

        [Inject] private readonly IPlatformUserModel _platformUserModel = null;

        [UIParams] private readonly BSMLParserParams _parserParams = null;
        
        [UIAction("#post-parse")]
        private async void PostParse()
        {
            try
            {
                IsLoaded = false;
                
                var topOfLeaderboard = await _api.GetLeaderboardRange(0, 10);
                SetLeaderboardData(topOfLeaderboard);
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        #region UserInfo Modal

        [UIValue("profileNameText")] private string ProfileNameText { get; set; } = string.Empty;
        [UIValue("profileMmrText")] private string ProfileMmrText { get; set; } = string.Empty;
        [UIValue("profileDivisionText")] private string ProfileDivisionText { get; set; } = string.Empty;
        [UIValue("profileRankText")] private string ProfileRankText { get; set; } = string.Empty;
        [UIValue("profilePlayedMatches")] private string ProfilePlayedMatches { get; set; } = string.Empty;
        
        private void OnUserInfoButtonClicked(CompCube_Models.Models.ClientData.UserInfo userInfo)
        {
            _parserParams.EmitEvent("profileModalShow");

            ProfileNameText = $"{userInfo.GetFormattedUserName()}'s Profile";
            ProfileMmrText = "MMR: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Mmr.ToString().FormatWithHtmlColor(userInfo.Division.Color)}";
            ProfileDivisionText =
                "Division: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Division.Division} {userInfo.Division.SubDivision}".FormatWithHtmlColor(userInfo.Division.Color);
            ProfileRankText = "Rank: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Rank}";
            ProfilePlayedMatches = "Matches Played:".FormatWithHtmlColor("#6F6F6F");
            
            NotifyPropertyChanged(null);
        }

        #endregion
        
        #region Leaderboard

        private bool _isLoaded = false;

        [UIValue("is-loaded")]
        private bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
                NotifyPropertyChanged(null);
            }
        }
        
        [UIValue("is-loading")]
        private bool IsLoading => !IsLoaded;

        [UIComponent("leaderboard")] private readonly CustomCellListTableData _leaderboard = null;

        [UIValue("cell-data")] private readonly List<IconSegmentedControl.DataItem> _cellData = new(){};

        private void SetLeaderboardData(CompCube_Models.Models.ClientData.UserInfo[] userInfo)
        {
            _leaderboard.Data = userInfo.Select(i =>
            {
                var leaderboardSlot = new LeaderboardSlot(i, i.UserId == _platformUserModel.GetUserInfo(CancellationToken.None).Result.platformUserId);
                leaderboardSlot.OnUserInfoButtonClicked += OnUserInfoButtonClicked;
                return leaderboardSlot;
            }).ToList();
            
            _leaderboard.TableView.ReloadData();
            IsLoaded = true;
        }

        private bool _upEnabled = false;

        [UIValue("up-enabled")]
        private bool UpEnabled
        {
            get => _upEnabled;
            set
            {
                _upEnabled = value;
                NotifyPropertyChanged(null);
            }
        }
        
        private bool _downEnabled = true;
        
        [UIValue("down-enabled")]
        private bool DownEnabled
        {
            get => _downEnabled;
            set
            {
                _downEnabled = value;
                NotifyPropertyChanged(null);
            }
        }
        
        private int _pageNumber = 0;

        private enum LeaderboardStates
        {
            Global,
            Self,
        }

        private LeaderboardStates CurrentState { get; set; } = LeaderboardStates.Global;

        [UIAction("cell-selected")]
        private void OnCellSelected(SegmentedControl segmentedControl, int index)
        {
            switch (index)
            {
                case 0:
                    SetLeaderboardState(LeaderboardStates.Global);
                    break;
                case 1:
                    SetLeaderboardState(LeaderboardStates.Self);
                    break;
            }
        }

        private async void SetLeaderboardState(LeaderboardStates state)
        {
            try
            {
                if (state == CurrentState) 
                    return;
            
                CurrentState = state;

                _pageNumber = 0;
            
                IsLoaded = false;
            
                switch (state)
                {
                    case LeaderboardStates.Global:
                        UpEnabled = false;
                        DownEnabled = true;
                        var topOfLeaderboard = await _api.GetLeaderboardRange(1, 10);
                        SetLeaderboardData(topOfLeaderboard);
                        break;
                    case LeaderboardStates.Self:
                        UpEnabled = false;
                        DownEnabled = false;
                        var aroundUser = await _api.GetAroundUser(_platformUserModel.GetUserInfo(CancellationToken.None).Result.platformUserId);
                        SetLeaderboardData(aroundUser);
                        break;
                }

                IsLoaded = true;
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        [UIAction("up-clicked")]
        private async void OnUpClicked()
        {
            try
            {
                IsLoaded = false;
                _pageNumber--;
            
                var leaderboardData = await _api.GetLeaderboardRange(_pageNumber * 10, 10);
                SetLeaderboardData(leaderboardData);
                if (_pageNumber == 0) 
                    UpEnabled = false;
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        [UIAction("down-clicked")]
        private async void DownClicked()
        {
            try
            {
                IsLoaded = false;
                _pageNumber++;
            
                var leaderboardData = await _api.GetLeaderboardRange(_pageNumber * 10, 10);
                SetLeaderboardData(leaderboardData);
                if (leaderboardData?.Length < 10)
                    DownEnabled = false;
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }
        #endregion

        public void Initialize()
        {
            _cellData.Add(new IconSegmentedControl.DataItem(_platformLeaderboardViewController.GetField<Sprite, PlatformLeaderboardViewController>("_globalLeaderboardIcon"), Localization.Get("BUTTON_HIGHSCORES_GLOBAL")));
            _cellData.Add(new IconSegmentedControl.DataItem(_platformLeaderboardViewController.GetField<Sprite, PlatformLeaderboardViewController>("_aroundPlayerLeaderboardIcon"), Localization.Get("BUTTON_HIGHSCORES_AROUND_YOU")));
        }

        public void Refresh()
        {
            this.SetLeaderboardState(LeaderboardStates.Global);
            _siraLog.Info("hello");
        }
    }
}