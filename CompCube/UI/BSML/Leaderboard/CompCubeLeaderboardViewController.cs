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
using CompCube.Game;
using CompCube.Server;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CompCube.UI.BSML.Leaderboard
{
    [ViewDefinition("CompCube.UI.BSML.Leaderboard.LeaderboardView.bsml")]
    public class CompCubeLeaderboardViewController : BSMLAutomaticViewController, IInitializable, IRefreshableView
    {
        [Inject] private readonly PlatformLeaderboardViewController _platformLeaderboardViewController = null;
        [Inject] private readonly IApi _api = null;
        [Inject] private readonly SiraLog _siraLog = null;
        [Inject] private readonly DiContainer _container = null;

        [Inject] private readonly UserModelWrapper _userModelWrapper = null;

        [UIParams] private readonly BSMLParserParams _parserParams = null;
        
        [UIAction("#post-parse")]
        private async void PostParse()
        {
            try
            {
                _profilePicture.material = Resources.FindObjectsOfTypeAll<Material>()
                    .FirstOrDefault(m => m.name == "UINoGlowRoundEdge");
                
                IsLoaded = false;
                
                _playerCellDataSource = _container.InstantiateComponent<PlayerCellDataSource>(gameObject);
                leaderboardTableData.TableView.SetDataSource(_playerCellDataSource, true);

                _playerCellDataSource.Init(leaderboardTableData.TableView);

                Destroy(leaderboardTableData);

                _playerCellDataSource.TableView.didSelectCellWithIdxEvent += TableView_didSelectCellWithIdxEvent;
                
                _pageNumber = 0;
                _noMorePlayers = false;

                await FetchNextPage();
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        #region UserInfo Modal

        [UIComponent("profilePicture")] private readonly Image _profilePicture = null!;
        
        [UIValue("profilePictureLink")] private string ProfilePictureLink { get; set; } = string.Empty;
        [UIValue("profileNameText")] private string ProfileNameText { get; set; } = string.Empty;
        [UIValue("profileMmrText")] private string ProfileMmrText { get; set; } = string.Empty;
        [UIValue("profileDivisionText")] private string ProfileDivisionText { get; set; } = string.Empty;
        [UIValue("profileRankText")] private string ProfileRankText { get; set; } = string.Empty;
        [UIValue("winRate")] private string WinRateText { get; set; } = string.Empty;
        [UIValue("winStreak")] private string WinStreakText { get; set; } = string.Empty;
        
        private void OnUserInfoButtonClicked(CompCube_Models.Models.ClientData.UserInfo userInfo)
        {
            _parserParams.EmitEvent("profileModalShow");
            
            ProfilePictureLink = userInfo.ProfilePictureLink;
            ProfileNameText = $"{userInfo.GetFormattedUserName()}'s Profile";
            ProfileMmrText = "MMR: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Mmr.ToString().FormatWithHtmlColor(userInfo.Division.Color)}";
            ProfileDivisionText =
                "Division: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Division.Division} {userInfo.Division.SubDivision}".FormatWithHtmlColor(userInfo.Division.Color);
            ProfileRankText = "Rank: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Rank}";
            WinRateText = "Wins: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Wins}" + "/".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.TotalGames}";
            WinStreakText = "Win Streak: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.Winstreak}" + " (Best: ".FormatWithHtmlColor("#6F6F6F") + $"{userInfo.HighestWinstreak}" + ")".FormatWithHtmlColor("#6F6F6F");
            
            NotifyPropertyChanged(null);
        }

        private void TableView_didSelectCellWithIdxEvent(TableView view, int idx)
        {
            OnUserInfoButtonClicked(_playerCellDataSource.Data[idx]);
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

        [UIComponent("leaderboardTableView")]
        private readonly CustomListTableData leaderboardTableData = null;
        
        [UIComponent("leaderboardTableView")]
        internal readonly Transform leaderboardTransform = null;
        
        internal PlayerCellDataSource _playerCellDataSource = null;

        [UIValue("cell-data")] private readonly List<IconSegmentedControl.DataItem> _cellData = new(){};

        private void SetLeaderboardData(CompCube_Models.Models.ClientData.UserInfo[] userInfo, bool isAppend = false)
        {
            if (userInfo == null || userInfo.Length == 0)
            {
                _noMorePlayers = true;
                return;
            }

            if (isAppend)
                _playerCellDataSource.AddData(userInfo.ToList());
            else
                _playerCellDataSource.SetData(userInfo.ToList());

            IsLoaded = true;
        }

        
        private async void ScrollView_scrollPositionChangedEvent(float position)
        {
            if (position <= 0f)
            {
                return;
            }


            if (_noMorePlayers)
            {
                return;
            }


            if (_isFetchingLeaderboard)
            {
                return;
            }

            if (Time.unscaledTime - _lastScrollTriggerTime < ScrollTriggerCooldown)
            {
                return;
            }

            var withinTwo =
                _playerCellDataSource.TableView.visibleCells
                    .Any(cell => cell.idx >= _playerCellDataSource.Data.Count - 3);

            if (!withinTwo)
            {
                return;
            }

            _lastScrollTriggerTime = Time.unscaledTime;
            
            await FetchNextPage();
        }
        
        private async Task FetchNextPage()
        {
            if (_noMorePlayers)
                return;

            try
            {
                _isFetchingLeaderboard = true;

                int start = _pageNumber * PageSize;

                CompCube_Models.Models.ClientData.UserInfo[] result;

                result = await _api.GetLeaderboardRange(start, PageSize);

                if (result == null || result.Length == 0)
                {
                    _noMorePlayers = true;
                    return;
                }

                if (_pageNumber == 0)
                    _playerCellDataSource.SetData(result.ToList());
                else
                    _playerCellDataSource.AddData(result.ToList());

                _pageNumber++;
                IsLoaded = true;
            }
            catch (Exception e)
            {
                _siraLog.Warn($"Leaderboard fetch failed: {e.Message}");
                _noMorePlayers = true;
            }
            finally
            {
                _isFetchingLeaderboard = false;
            }
        }
        
        private void Update() {
            if (_playerCellDataSource.TableView.scrollView != null)
            {
                ScrollView_scrollPositionChangedEvent(_playerCellDataSource.TableView.scrollView.position);
            }
        }
        
        private bool _isFetchingLeaderboard = false;
        private bool _noMorePlayers = false;
        private float _lastScrollTriggerTime = 0f;
        private const float ScrollTriggerCooldown = 1.5f;

        private const int PageSize = 10;

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
                        _noMorePlayers = false;
                        var topOfLeaderboard = await _api.GetLeaderboardRange(1, 10);
                        SetLeaderboardData(topOfLeaderboard);
                        break;
                    case LeaderboardStates.Self:
                        _noMorePlayers = true;
                        var aroundUser = await _api.GetAroundUser(_userModelWrapper.UserId);
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

        #endregion

        public void Initialize()
        {
            _cellData.Add(new IconSegmentedControl.DataItem(_platformLeaderboardViewController.GetField<Sprite, PlatformLeaderboardViewController>("_globalLeaderboardIcon"), Localization.Get("BUTTON_HIGHSCORES_GLOBAL")));
            _cellData.Add(new IconSegmentedControl.DataItem(_platformLeaderboardViewController.GetField<Sprite, PlatformLeaderboardViewController>("_aroundPlayerLeaderboardIcon"), Localization.Get("BUTTON_HIGHSCORES_AROUND_YOU")));
        }

        public void Refresh()
        {
            this.SetLeaderboardState(LeaderboardStates.Global);
        }
    }
}