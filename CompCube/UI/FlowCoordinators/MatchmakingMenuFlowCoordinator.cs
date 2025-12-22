using CompCube_Models.Models.Packets.ServerPackets;
using CompCube.Interfaces;
using CompCube.UI.BSML.Leaderboard;
using CompCube.UI.BSML.Menu;
using CompCube.UI.FlowCoordinators.Events;
using HMUI;
using CompCube.Extensions;
using CompCube.UI.BSML.EarlyLeaveWarning;
using CompCube.UI.ViewManagers;
using Zenject;

namespace CompCube.UI.FlowCoordinators
{
    public class MatchmakingMenuFlowCoordinator : FlowCoordinator, IInitializable, IDisposable
    {
        [Inject] private readonly MainFlowCoordinator _mainFlowCoordinator = null!;
        [Inject] private readonly MatchFlowCoordinator _matchFlowCoordinator = null!;
        [Inject] private readonly InfoFlowCoordinator _infoFlowCoordinator = null!;
        
        [Inject] private readonly IServerListener _serverListener = null!;
        [Inject] private readonly MatchmakingMenuViewController _matchmakingMenuViewController = null!;

        [Inject] private readonly GameplaySetupViewManager _gameplaySetupViewManager = null!;
        [Inject] private readonly RankingDataTabSwitcherViewController _rankingDataTabSwitcherViewController = null!;
        [Inject] private readonly EarlyLeaveWarningModalViewController _earlyLeaveWarningModalViewController = null!;
        
        [Inject] private readonly EventsFlowCoordinator _eventsFlowCoordinator = null!;
        
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            showBackButton = true;
            SetTitle("CompCube");
            ProvideInitialViewControllers(_matchmakingMenuViewController, rightScreenViewController: _rankingDataTabSwitcherViewController, leftScreenViewController: _gameplaySetupViewManager.ManagedController);
        }

        private void OnMatchCreated(MatchCreatedPacket packet)
        {
            this.PresentFlowCoordinatorSynchronously(_matchFlowCoordinator);

            _matchFlowCoordinator.PopulateData(packet, () =>
            {
                DismissFlowCoordinator(_matchFlowCoordinator);
                _serverListener.Disconnect();
            });
        }

        public void Dispose()
        {
            _serverListener.OnMatchCreated -= OnMatchCreated;
            _infoFlowCoordinator.OnBackButtonPressed -= OnInfoFlowCoordinatorBackButtonPressed;
            _eventsFlowCoordinator.OnBackButtonPressed -= EventsFlowCoordinatorOnBackButtonPressed;
        }
        
        public void Initialize()
        {
            _matchmakingMenuViewController.SetButtonCallbacks(() =>
            {
                this.PresentFlowCoordinatorSynchronously(_infoFlowCoordinator);
            });
            
            _serverListener.OnMatchCreated += OnMatchCreated;
            _infoFlowCoordinator.OnBackButtonPressed += OnInfoFlowCoordinatorBackButtonPressed;
            _eventsFlowCoordinator.OnBackButtonPressed += EventsFlowCoordinatorOnBackButtonPressed;
        }

        private void EventsFlowCoordinatorOnBackButtonPressed() => DismissFlowCoordinator(_eventsFlowCoordinator);

        private void OnEventsButtonClicked() => this.PresentFlowCoordinatorSynchronously(_eventsFlowCoordinator);

        private void OnInfoFlowCoordinatorBackButtonPressed() => DismissFlowCoordinator(_infoFlowCoordinator);

        private void OnAboutButtonClicked()
        {
            this.PresentFlowCoordinatorSynchronously(_infoFlowCoordinator);
        }

        protected override void BackButtonWasPressed(ViewController viewController)
        {
            if (_serverListener.Connected)
            {
                _earlyLeaveWarningModalViewController.ParseOntoGameObject(viewController, "Are you sure you want to leave the matchmaking queue?", () =>
                {
                    _serverListener.Disconnect();
                    _mainFlowCoordinator.DismissAllChildFlowCoordinators();
                });
                return;
            }
                
            _mainFlowCoordinator.DismissAllChildFlowCoordinators();
        }
    }
}  