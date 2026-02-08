using BeatSaberMarkupLanguage;
using CompCube_Models.Models.Map;
using CompCube_Models.Models.Packets;
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
using CompCube.UI.BSML.EarlyLeaveWarning;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace CompCube.UI.FlowCoordinators
{
    public class MatchFlowCoordinator : FlowCoordinator
    {
        [Inject] private readonly VotingScreenViewController _votingScreenViewController = null!;
        [Inject] private readonly AwaitingMapDecisionViewController _awaitingMapDecisionViewController = null!;
        [Inject] private readonly WaitingForMatchToStartViewController _waitingForMatchToStartViewController = null!;
        [Inject] private readonly AwaitMatchEndViewController _awaitMatchEndViewController = null!;
        [Inject] private readonly RoundResultsViewController _roundResultsViewController = null!;
        [Inject] private readonly OpponentViewController _opponentViewController = null!;
        [Inject] private readonly MatchResultsViewController _matchResultsViewController = null!;
        [Inject] private readonly EarlyLeaveWarningModalViewController _earlyLeaveWarningModalViewController = null!;
         
        [Inject] private readonly IServerListener _serverListener = null!;
        [Inject] private readonly MatchManager _matchManager = null!;
        [Inject] private readonly MatchStateManager _matchStateManager = null!;
        
        [Inject] private readonly SiraLog _siraLog = null!;
        
        [Inject] private readonly StandardLevelDetailViewManager _standardLevelDetailViewManager = null!;
        [Inject] private readonly GameplaySetupViewManager _gameplaySetupViewManager = null!;
        
        [Inject] private readonly DisconnectHandler _disconnectHandler = null!;

        [Inject] private readonly DisconnectFlowCoordinator _disconnectFlowCoordinator = null!;
        [Inject] private readonly DisconnectedViewController _disconnectedViewController = null!;
        
        [Inject] private readonly SoundEffectManager _soundEffectManager = null!;

        private NavigationController _votingScreenNavigationController;

        private Action? _onMatchFinishedCallback = null;

        public void PopulateData(MatchCreatedPacket packet, Action? onMatchFinishedCallback)
        {
            _opponentViewController.PopulateData(packet.Red, packet.Blue);
            _opponentViewController.UpdateRound(1);
            _opponentViewController.UpdatePoints(0, 0);
            
            _onMatchFinishedCallback = onMatchFinishedCallback;
        }
        
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle("Match Room");
            showBackButton = true;
            
            _votingScreenNavigationController = BeatSaberUI.CreateViewController<NavigationController>();
            
            ProvideInitialViewControllers(_votingScreenNavigationController, _gameplaySetupViewManager.ManagedController, bottomScreenViewController: _opponentViewController);
            _votingScreenNavigationController.PushViewController(_votingScreenViewController, null);
            
            _votingScreenViewController.MapSelected += HandleVotingScreenMapSelected;
            _votingScreenViewController.RanOutOfTime += VotingScreenViewControllerOnRanOutOfTime;
            
            _serverListener.OnRoundStarted += OnRoundStarted;
            _serverListener.OnBeginGameTransition += TransitionToGame;
            _serverListener.OnRoundResults += OnRoundResults;
            _serverListener.OnMatchResults += HandleMatchResults;
            _disconnectHandler.ShouldShowDisconnectScreen += HandleShouldShowDisconnectScreen;
        }



        private void HandleShouldShowDisconnectScreen(string reason, bool matchOnly)
        {
            while (!isActivated);
            
            this.PresentFlowCoordinatorSynchronously(_disconnectFlowCoordinator);
            
            _disconnectedViewController.SetReason(reason, async void () =>
            {
                try
                {
                    await DismissChildFlowCoordinatorsRecursively();
                }
                catch(Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void HandleVotingScreenMapSelected(VotingMap votingMap, List<VotingMap> votingMaps)
        {
            if (!_standardLevelDetailViewManager.ManagedController.isActivated)
                _votingScreenNavigationController.PushViewController(_standardLevelDetailViewManager.ManagedController,
                    () =>
                    {
                        _standardLevelDetailViewManager.ManagedController.transform.position = new Vector3(1.4f,
                            _standardLevelDetailViewManager.ManagedController.transform.position.y, 
                            _standardLevelDetailViewManager.ManagedController.transform.position.z);
                    });
            
            _standardLevelDetailViewManager.SetData(votingMap, votingMaps, HandleVoteButtonPressed);
            
            _soundEffectManager.PlayBeatmapLevelPreview(votingMap.GetBeatmapLevel()!);
        }

        private async void HandleVoteButtonPressed(VotingMap votingMap, List<VotingMap> votingMaps, bool ranOutOfTime = false)
        {
            try
            {
                _votingScreenNavigationController.PopViewController(() => {}, true);
                this.ReplaceViewControllerSynchronously(_awaitingMapDecisionViewController);
                
                while (!_awaitingMapDecisionViewController.isActivated)
                    await Task.Delay(25);
                
                _awaitingMapDecisionViewController.PopulateData(votingMap, votingMaps);
                
                _soundEffectManager.CrossfadeToDefault();

                if (!ranOutOfTime)
                {
                    await _serverListener.SendPacket(new VotePacket(votingMaps.IndexOf(votingMap)));
                }
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }
        
        private void VotingScreenViewControllerOnRanOutOfTime(List<VotingMap> votingMaps)
        {
            HandleVoteButtonPressed(null!, votingMaps);
        }

        private void OnRoundResults(RoundResultsPacket results)
        {
            this.ReplaceViewControllerSynchronously(_roundResultsViewController);
            
            _roundResultsViewController.PopulateData(results);
            
            _opponentViewController.UpdatePoints(results.RedPoints, results.BluePoints);
        }
        
        private void HandleMatchResults(MatchResultsPacket results)
        {
            // garbage
            _disconnectHandler.ShouldShowDisconnectScreen -= HandleShouldShowDisconnectScreen;
            
            _opponentViewController.UpdatePoints(results.FinalRedScore, results.FinalBlueScore);
            _opponentViewController.SetStatus("Match Concluded!");
            
            showBackButton = false;
            this.ReplaceViewControllerSynchronously(_matchResultsViewController);
            _matchResultsViewController.PopulateData(results.FinalRedScore, results.FinalBlueScore, results.MmrChange, () => _onMatchFinishedCallback?.Invoke());
            
            if ((results.FinalRedScore > results.FinalBlueScore && _matchStateManager.OwnTeam == MatchStateManager.Team.Red) || 
                (results.FinalBlueScore > results.FinalRedScore && _matchStateManager.OwnTeam == MatchStateManager.Team.Blue))
                _soundEffectManager.PlayWinningMusic();
            
            _serverListener.Disconnect();
        }

        private async void TransitionToGame(BeginGameTransitionPacket packet)
        {
            try
            {
                this.ReplaceViewControllerSynchronously(_waitingForMatchToStartViewController);
                _waitingForMatchToStartViewController.SetPostParseCallback(() =>
                {
                    _waitingForMatchToStartViewController.PopulateData(packet.Map, DateTime.UtcNow.AddSeconds(packet.TransitionToGameTime));
                });
                
                _soundEffectManager.PlayGongSoundEffect();
            
                await Task.Delay(packet.TransitionToGameTime * 1000);

                if (!isActivated)
                    return;
            
                _matchManager.StartMatch(packet.Map, DateTime.UtcNow.AddSeconds(packet.UnpauseTime), _gameplaySetupViewManager.ProMode,
                    (results, so) =>
                    {
                        this.ReplaceViewControllerSynchronously(_awaitMatchEndViewController, true);

                        _serverListener.SendPacket(new ScoreSubmissionPacket(results.multipliedScore, ScoreModel.ComputeMaxMultipliedScoreForBeatmap(so.transformedBeatmapData), results.gameplayModifiers.proMode, results.notGoodCount, results.fullCombo));
                    });
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        private void OnRoundStarted(RoundStartedPacket roundStartedPacket)
        {
            _votingScreenViewController.SetCountdownTime(DateTime.Now.AddSeconds(roundStartedPacket.VotingTime));
            
            _votingScreenViewController.SetActivationCallback(() =>
            { 
                _votingScreenViewController.PopulateData(roundStartedPacket.Maps);
            });

            if (!_roundResultsViewController.isActivated) 
                return;
            
            _roundResultsViewController.SetContinueButtonCallback(() =>
            {
                ResetNavigationController();
                this.ReplaceViewControllerSynchronously(_votingScreenNavigationController);
            });
                
            _opponentViewController.UpdateRound(roundStartedPacket.Round);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _votingScreenViewController.MapSelected -= HandleVotingScreenMapSelected;
            
            _serverListener.OnRoundStarted -= OnRoundStarted;
            _serverListener.OnBeginGameTransition -= TransitionToGame;
            _serverListener.OnRoundResults -= OnRoundResults;
            _serverListener.OnMatchResults -= HandleMatchResults;
            _disconnectHandler.ShouldShowDisconnectScreen -= HandleShouldShowDisconnectScreen;
        }

        private void ResetNavigationController()
        {
            if (_votingScreenNavigationController)
                Destroy(_votingScreenNavigationController);
            _votingScreenNavigationController = BeatSaberUI.CreateViewController<NavigationController>();
            _votingScreenNavigationController.PushViewController(
                                                    _votingScreenViewController, null);
        }

        protected override void BackButtonWasPressed(ViewController viewController)
        {
            _earlyLeaveWarningModalViewController.ParseOntoGameObject(viewController, "Are you sure you want to leave the match early?\nLeaving the match early could result in penalties!", () =>
            {
                _onMatchFinishedCallback?.Invoke();
            });
        }
    }
}