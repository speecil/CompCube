using CompCube.Configuration;
using CompCube.Game;
using CompCube.Server;
using CompCube.UI.BSML.Menu;
using HMUI;
using CompCube.Extensions;
using SiraUtil.Logging;
using SongCore;
using Zenject;

namespace CompCube.UI.FlowCoordinators;

public class ServerCheckingFlowCoordinator : FlowCoordinator
{
    [Inject] private readonly MatchmakingMenuFlowCoordinator _matchmakingMenuFlowCoordinator = null!;
    
    [Inject] private readonly CheckingServerStatusViewController _checkingServerStatusViewController = null!;
    [Inject] private readonly CantConnectToServerViewController _cantConnectToServerViewController = null!;
    [Inject] private readonly MissingMapsViewController _missingMapsViewController = null!;
    
    [Inject] private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

    [Inject] private readonly InitialServerChecker _serverChecker = null!;
    [Inject] private readonly MapDownloader _mapDownloader = null!;
    
    [Inject] private readonly SiraLog _siraLog = null!;
    [Inject] private readonly PluginConfig _config = null!;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        showBackButton = true;
        SetTitle("CompCube");

        ProvideInitialViewControllers(_checkingServerStatusViewController);
        _checkingServerStatusViewController.SetControllerState(InitialServerChecker.ServerCheckingStates.ServerStatus);

        _cantConnectToServerViewController.OnContinueButtonPressed += OnContinueButtonPressed;

        _serverChecker.ServerCheckFailed += OnServerCheckFailed;
        _serverChecker.ServerCheckFinished += ServerCheckFinished;
        _serverChecker.StartMapDownload += OnStartMapDownload;

        Task.Run(async () => await _serverChecker.CheckServer());
    }

    protected override void BackButtonWasPressed(ViewController _)
    {
        _mainFlowCoordinator.DismissFlowCoordinator(this);
    }

    private void OnStartMapDownload(string[] missingMapHashes)
    {
        if (_config.DownloadMapsAutomatically)
        {
            UserChoseToDownloadMaps(true, missingMapHashes);
            return;
        }
        
        this.ReplaceViewControllerSynchronously(_missingMapsViewController);
        _missingMapsViewController.SetMissingMapCount(missingMapHashes.Length, (choice) =>
        {
            UserChoseToDownloadMaps(choice, missingMapHashes);
        });
    }
    
    private async void UserChoseToDownloadMaps(bool choice, string[] hashes)
    {
        try
        {
            if (choice)
            {
                await DownloadMaps(hashes);
                return;
            }
                
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
        catch(Exception e)
        {
            _siraLog.Error(e);
        }
    }

    private void ServerCheckFinished() => this.PresentFlowCoordinatorSynchronously(_matchmakingMenuFlowCoordinator);

    private void OnServerCheckFailed(string reason)
    {
        _serverChecker.ServerCheckFinished -= ServerCheckFinished;
        
        this.ReplaceViewControllerSynchronously(_cantConnectToServerViewController);
        _cantConnectToServerViewController.SetReasonText(reason);
    }

    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _cantConnectToServerViewController.OnContinueButtonPressed -= OnContinueButtonPressed;
        _serverChecker.ServerCheckFailed -= OnServerCheckFailed;
        _serverChecker.ServerCheckFinished -= ServerCheckFinished;
        _serverChecker.StartMapDownload -= OnStartMapDownload;
    }

    private void OnContinueButtonPressed() => _mainFlowCoordinator.DismissFlowCoordinator(this);
    
    private async Task DownloadMaps(string[] missingMapHashes)
    {
        try
        {
            showBackButton = false;
            
            this.ReplaceViewControllerSynchronously(_checkingServerStatusViewController);
            _checkingServerStatusViewController.SetControllerState(InitialServerChecker.ServerCheckingStates.DownloadingMaps);

            await _mapDownloader.DownloadMaps(missingMapHashes);
            
            showBackButton = true;
            Loader.Instance.RefreshSongs();

            while (Loader.AreSongsLoading)
                await Task.Delay(25);
            
            this.PresentFlowCoordinatorSynchronously(_matchmakingMenuFlowCoordinator);
        }
        catch (Exception ex)
        {
            _siraLog.Error(ex);
            showBackButton = true;
            this.ReplaceViewControllerSynchronously(_cantConnectToServerViewController);
            _cantConnectToServerViewController.SetReasonText("Unhandled exception downloading beatmaps, please check your logs for more details!");
        }
    }
}