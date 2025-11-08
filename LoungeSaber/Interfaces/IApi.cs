using CompCube_Models.Models.Events;
using CompCube_Models.Models.Server;
using JetBrains.Annotations;

namespace LoungeSaber.Interfaces;

public interface IApi
{
    [ItemCanBeNull] 
    public Task<CompCube_Models.Models.ClientData.UserInfo> GetUserInfo(string id);

    [ItemCanBeNull] 
    public Task<CompCube_Models.Models.ClientData.UserInfo[]> GetLeaderboardRange(int start, int range);

    [ItemCanBeNull]
    public Task<CompCube_Models.Models.ClientData.UserInfo[]> GetAroundUser(string id);

    [ItemCanBeNull]
    public Task<ServerStatus> GetServerStatus();

    public Task<string[]> GetMapHashes();
    
    public Task<EventData[]> GetEvents();
}