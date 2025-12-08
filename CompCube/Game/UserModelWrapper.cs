using CompCube.Configuration;
using CompCube.Interfaces;
using CompCube.Server.Debug;
using Zenject;

namespace CompCube.Game;

public class UserModelWrapper
{
    [Inject] private readonly IPlatformUserModel _platformUserModel = null!;
    [Inject] private readonly PluginConfig _config = null!;

    public string UserId => _config.SkipServer ? DebugApi.Self.UserId : _platformUserModel.GetUserInfo(CancellationToken.None).Result.platformUserId;
    
    public string UserName => _config.SkipServer ? DebugApi.Self.Username : _platformUserModel.GetUserInfo(CancellationToken.None).Result.userName;
}