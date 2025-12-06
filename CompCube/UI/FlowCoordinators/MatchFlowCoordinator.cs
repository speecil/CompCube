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
        
        [Inject] private readonly IServerListener _serverListener = null!;
        [Inject] private readonly MatchManager _matchManager = null!;
        
        [Inject] private readonly SiraLog _siraLog = null!;
        
        [Inject] private readonly StandardLevelDetailViewManager _standardLevelDetailViewManager = null!;
        [Inject] private readonly GameplaySetupViewManager _gameplaySetupViewManager = null!;
        
        [Inject] private readonly DisconnectHandler _disconnectHandler = null!;

        [Inject] private readonly DisconnectFlowCoordinator _disconnectFlowCoordinator = null!;
        [Inject] private readonly DisconnectedViewController _disconnectedViewController = null!;
        
        [Inject] private readonly IPlatformUserModel _platformUserModel = null!;
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
            showBackButton = false;
            
            _votingScreenNavigationController = BeatSaberUI.CreateViewController<NavigationController>();
            
            ProvideInitialViewControllers(_votingScreenNavigationController, _gameplaySetupViewManager.ManagedController, bottomScreenViewController: _opponentViewController);
            _votingScreenNavigationController.PushViewController(_votingScreenViewController, null);
            
            _votingScreenViewController.MapSelected += HandleVotingScreenMapSelected;
            
            _serverListener.OnRoundStarted += OnRoundStarted;
            _serverListener.OnBeginGameTransition += TransitionToGame;
            _serverListener.OnRoundResults += OnRoundResults;
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

        private async void HandleVoteButtonPressed(VotingMap votingMap, List<VotingMap> votingMaps)
        {
            try
            {
                this.ReplaceViewControllerSynchronously(_awaitingMapDecisionViewController);
                
                while (!_awaitingMapDecisionViewController.isActivated)
                    await Task.Delay(25);
                
                _awaitingMapDecisionViewController.PopulateData(votingMap, votingMaps);
                
                await _serverListener.SendPacket(new VotePacket(votingMaps.IndexOf(votingMap)));
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        private void OnRoundResults(RoundResultsPacket results)
        {
            this.ReplaceViewControllerSynchronously(_roundResultsViewController);
            
            _roundResultsViewController.PopulateData(results);
            
            _opponentViewController.UpdatePoints(results.RedPoints, results.BluePoints);
        }

        private async void TransitionToGame(BeginGameTransitionPacket packet)
        {
            try
            {
                this.ReplaceViewControllerSynchronously(_waitingForMatchToStartViewController);
                await _waitingForMatchToStartViewController.PopulateData(packet.Map, DateTime.UtcNow.AddSeconds(packet.TransitionToGameTime));
            
                await Task.Delay(packet.TransitionToGameTime * 1000);
            
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
            _votingScreenViewController.SetActivationCallback(() =>
            { 
                _votingScreenViewController.PopulateData(roundStartedPacket.Maps, roundStartedPacket.VotingTime);
            });

            if (!_roundResultsViewController.isActivated) 
                return;
            
            _roundResultsViewController.SetContinueButtonCallback(() =>
            {
                ResetNavigationController();
                this.ReplaceViewControllerSynchronously(_votingScreenNavigationController);
            });
                
            _opponentViewController.UpdateRound(1);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _votingScreenViewController.MapSelected -= HandleVotingScreenMapSelected;
        }

        private void ResetNavigationController()
        {
            if (_votingScreenNavigationController)
                Destroy(_votingScreenNavigationController);
            _votingScreenNavigationController = BeatSaberUI.CreateViewController<NavigationController>();
            _votingScreenNavigationController.PushViewController(
                                                    _votingScreenViewController, null);
        }
    }
}