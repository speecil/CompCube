#nullable enable
using CompCube_Models.Models.Server;
using CompCube.Configuration;
using CompCube.Interfaces;
using IPA.Utilities;
using SiraUtil.Tools.FPFC;
using SongCore;
using Zenject;

namespace CompCube.Server;

public class InitialServerChecker
{
    [Inject] private readonly IApi _api = null!;
    [Inject] private readonly IPlatformUserModel _platformUserModel = null!;

    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly IFPFCSettings _fpfcSettings = null!;
    
    public event Action<string>? ServerCheckFailed;

    public event Action<ServerCheckingStates>? ServerCheckingStateUpdated;

    // keeping this around bc i will need it for the leaderboard redesign
    public event Action<CompCube_Models.Models.ClientData.UserInfo?>? OnUserInfoFetched;

    public event Action? ServerCheckFinished;

    public event Action<string[]>? StartMapDownload;
    
    public async Task CheckServer()
    {
        // if (!CheckFpfc())
            // return;
        
        if (!await CheckServerState())
            return;
        if (!await CheckUserData())
            return;
        if (!await CheckMaps())
            return;
        
        ServerCheckFinished?.Invoke();
    }

    private bool CheckFpfc()
    {
        if (_config.SkipServer)
            return true;

        if (!_fpfcSettings.Enabled) 
            return true;
        
        ServerCheckFailed?.Invoke("FPFC is enabled. Please restart your game.");
        return false;

    }

    private async Task<bool> CheckMaps()
    {
        ServerCheckingStateUpdated?.Invoke(ServerCheckingStates.Maps);
        
        while (Loader.AreSongsLoading)
            await Task.Delay(25);

        var maps = await _api.GetMapHashes();
        var missingMapHashes = maps.Where(i => Loader.GetLevelByHash(i) == null).ToArray();
        
        if (missingMapHashes.Length == 0)
        {
            return true;
        }
        
        StartMapDownload?.Invoke(missingMapHashes);
        return false;
    }

    private async Task<bool> CheckUserData()
    {
        ServerCheckingStateUpdated?.Invoke(ServerCheckingStates.UserData);

        var userData = await _api.GetUserInfo((await _platformUserModel.GetUserInfo(CancellationToken.None)).platformUserId);
        
        if (userData != null && userData?.Banned == true)
        {
            ServerCheckFailed?.Invoke("You have been banned from CompCube.");
            return false;
        }
        
        OnUserInfoFetched?.Invoke(userData);
        return true;
    }
    
    private async Task<bool> CheckServerState()
    {
        var serverResponse = await _api.GetServerStatus();

        if (serverResponse == null)
        {
            ServerCheckFailed?.Invoke("InvalidServerResponse");
            return false;
        }

        if (!serverResponse.AllowedModVersions.Contains(IPA.Loader.PluginManager.GetPluginFromId("CompCube").HVersion
                .ToString()))
        {
            ServerCheckFailed?.Invoke("OutdatedPluginVersion");
            return false;
        }

        if (!serverResponse.AllowedGameVersions.Contains(UnityGame.GameVersion.ToString()))
        {
            ServerCheckFailed?.Invoke("OutdatedGameVersion");
            return false;
        }

        if (serverResponse.State == ServerState.State.Online) 
            return true;
        
        ServerCheckFailed?.Invoke("ServerInMaintenance");
        return false;

    }

    public enum ServerCheckingStates
    {
        ServerStatus,
        UserData,
        Maps,
        DownloadingMaps
    }
}