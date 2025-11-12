using BeatSaberMarkupLanguage;
using CompCube_Models.Models.Map;
using CompCube_Models.Models.Packets.ServerPackets;
using CompCube_Models.Models.Packets.UserPackets;
using CompCube.Game;
using CompCube.Interfaces;
using CompCube.UI.BSML.Disconnect;
using CompCube.UI.BSML.Match;
using CompCube.UI.Sound;
using CompCube.UI.ViewManagers;
using HMUI;
using CompCube.Extensions;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace CompCube.UI.FlowCoordinators
{
    public class MatchFlowCoordinator : FlowCoordinator
    {
        [Inject] private readonly VotingScreenViewController _votingScreenViewController = null;
        [Inject] private readonly AwaitingMapDecisionViewController _awaitingMapDecisionViewController = null;
        [Inject] private readonly WaitingForMatchToStartViewController _waitingForMatchToStartViewController = null;
        [Inject] private readonly AwaitMatchEndViewController _awaitMatchEndViewController = null;
        [Inject] private readonly MatchResultsViewController _matchResultsViewController = null;
        [Inject] private readonly OpponentViewController _opponentViewController = null;
        
        [Inject] private readonly IServerListener _serverListener = null;
        [Inject] private readonly MatchManager _matchManager = null;
        
        [Inject] private readonly SiraLog _siraLog = null;
        
        [Inject] private readonly StandardLevelDetailViewManager _standardLevelDetailViewManager = null;
        [Inject] private readonly GameplaySetupViewManager _gameplaySetupViewManager = null;
        
        [Inject] private readonly DisconnectHandler _disconnectHandler = null;

        [Inject] private readonly DisconnectFlowCoordinator _disconnectFlowCoordinator = null;
        [Inject] private readonly DisconnectedViewController _disconnectedViewController = null;
        
        [Inject] private readonly IPlatformUserModel _platformUserModel = null;
        [Inject] private readonly SoundEffectManager _soundEffectManager = null;

        private NavigationController _votingScreenNavigationController;

        private Action _onMatchFinishedCallback = null;
        
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle("Match Room");
            showBackButton = false;
            
            _votingScreenNavigationController = BeatSaberUI.CreateViewController<NavigationController>();
            
            ProvideInitialViewControllers(_votingScreenNavigationController, _gameplaySetupViewManager.ManagedController, bottomScreenViewController: _opponentViewController);
            _votingScreenNavigationController.PushViewController(_votingScreenViewController, null);
            
            _votingScreenViewController.MapSelected += OnVotingMapSelected;
            _serverListener.OnMatchStarting += OnMatchStarting;
            _serverListener.OnMatchResults += OnMatchResultsReceived;
            _standardLevelDetailViewManager.OnMapVoteButtonPressed += OnMapVotedFor;
            _disconnectHandler.ShouldShowDisconnectScreen += OnShouldShowDisconnectScreen;
        }

        public void StartMatch(MatchCreatedPacket packet, Action matchFinishedCallback)
        {
            _onMatchFinishedCallback = matchFinishedCallback;
            _opponentViewController.PopulateData(packet.Opponent);
            
            _votingScreenViewController.SetActivationCallback(() =>
            {
                _votingScreenViewController.PopulateData(packet);
            });
        }

        private void OnShouldShowDisconnectScreen(string reason, bool matchOnly)
        {
            while (!isActivated);
            
            _siraLog.Info(reason);
            
            this.PresentFlowCoordinatorSynchronously(_disconnectFlowCoordinator);
            _disconnectedViewController.SetReason(reason, async void () =>
            {
                try
                {
                    await DismissChildFlowCoordinatorsRecursively();
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void OnVotingMapSelected(VotingMap votingMap, List<VotingMap> votingMaps)
        {
            if (!_standardLevelDetailViewManager.ManagedController.isActivated)
                _votingScreenNavigationController.PushViewController(_standardLevelDetailViewManager.ManagedController,
                    () =>
                    {
                        _standardLevelDetailViewManager.ManagedController.transform.position = new Vector3(1.4f,
                            _standardLevelDetailViewManager.ManagedController.transform.position.y, 
                            _standardLevelDetailViewManager.ManagedController.transform.position.z);
                    });
            
            _standardLevelDetailViewManager.SetData(votingMap, votingMaps);
            
            _soundEffectManager.PlayBeatmapLevelPreview(votingMap.GetBeatmapLevel());
        }

        private void OnMatchResultsReceived(MatchResultsPacket results)
        {
            this.ReplaceViewControllerSynchronously(_matchResultsViewController);
            _matchResultsViewController.PopulateData(results, () =>
            {
                _onMatchFinishedCallback?.Invoke();
                _onMatchFinishedCallback = null;
            });

            if (results.WinnerScore.User.UserId != _platformUserModel.GetUserInfo(CancellationToken.None).Result.platformUserId)
                return;
            
            _soundEffectManager.PlayWinningMusic();
        }

        private async void OnMatchStarting(MatchStartedPacket packet)
        {
            try
            {
                this.ReplaceViewControllerSynchronously(_waitingForMatchToStartViewController);
                await _waitingForMatchToStartViewController.PopulateData(packet.MapSelected, DateTime.UtcNow.AddSeconds(packet.TransitionToGameWait));
                
                _soundEffectManager.PlayGongSoundEffect();
                
                await Task.Delay(packet.TransitionToGameWait * 1000);
                
                _matchManager.StartMatch(packet.MapSelected, DateTime.UtcNow.AddSeconds(packet.StartingWait), _gameplaySetupViewManager.ProMode, packet.Opponent,  
                    (levelCompletionResults, standardLevelScenesTransitionSetupData) =>
                    {
                        _serverListener.SendPacket(new ScoreSubmissionPacket(levelCompletionResults.multipliedScore, ScoreModel.ComputeMaxMultipliedScoreForBeatmap(standardLevelScenesTransitionSetupData.transformedBeatmapData),
                            levelCompletionResults.gameplayModifiers.proMode, levelCompletionResults.notGoodCount, levelCompletionResults.fullCombo));
                        
                        this.ReplaceViewControllerSynchronously(_awaitMatchEndViewController, true);
                        SetLeftScreenViewController(null, ViewController.AnimationType.None);
                        SetBottomScreenViewController(null, ViewController.AnimationType.None);
                    });
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        private async void OnMapVotedFor(VotingMap votingMap, List<VotingMap> votingMaps)
        {
            try
            {
                _votingScreenNavigationController.PopViewControllers(1, () => {}, true);
                
                ReplaceTopViewController(_awaitingMapDecisionViewController);
                _awaitingMapDecisionViewController.PopulateData(votingMap, votingMaps);
                
                _soundEffectManager.CrossfadeToDefault();

                await _serverListener.SendPacket(new VotePacket(votingMaps.IndexOf(votingMap)));
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }
        
        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _votingScreenViewController.MapSelected -= OnVotingMapSelected;
            _serverListener.OnMatchStarting -= OnMatchStarting;
            _serverListener.OnMatchResults -= OnMatchResultsReceived;
            _standardLevelDetailViewManager.OnMapVoteButtonPressed -= OnMapVotedFor;
            _disconnectHandler.ShouldShowDisconnectScreen -= OnShouldShowDisconnectScreen;
        }
    }
}