using CompCube_Models.Models.ClientData;
using CompCube_Models.Models.Events;
using CompCube_Models.Models.Map;
using CompCube_Models.Models.Server;
using CompCube.Interfaces;
using IPA.Utilities;

namespace CompCube.Server.Debug;

public class DebugApi : IApi
{
    public static readonly VotingMap[] Maps =
    [
        new("44d8d1c7c5821a7f1929542cab49c906c9e585e4", VotingMap.DifficultyType.ExpertPlus, VotingMap.Category.Unknown, "Tech"), 
        new("44d8d1c7c5821a7f1929542cab49c906c9e585e4", VotingMap.DifficultyType.ExpertPlus, VotingMap.Category.Unknown, "Balanced"),
        new("44d8d1c7c5821a7f1929542cab49c906c9e585e4", VotingMap.DifficultyType.ExpertPlus, VotingMap.Category.Unknown, "Extreme")
    ];

    public static readonly CompCube_Models.Models.ClientData.UserInfo DebugOpponent = new("debugOpponent", "1", 1000,
        new DivisionInfo("Iron", 4, "#FFFFFF", false), null, 2, null,
        false, 0, 0, 0, 0);

    public static readonly CompCube_Models.Models.ClientData.UserInfo Self = new(
        "self",
        "0",
        1000,
        new DivisionInfo("Iron", 1, "#FFFFFF", false),
        null,
        1,
        null,
        false, 0, 0, 0, 0);
    
    public async Task<CompCube_Models.Models.ClientData.UserInfo?> GetUserInfo(string id)
    {
        await Task.Delay(1000);
        return Self;
    }

    public Task<CompCube_Models.Models.ClientData.UserInfo[]?> GetLeaderboardRange(int start, int range)
    {
        var info = new List<CompCube_Models.Models.ClientData.UserInfo>()
        {
            DebugOpponent, Self, DebugOpponent, Self, DebugOpponent, Self, DebugOpponent, Self, DebugOpponent, Self
        };
        return Task.FromResult(info.ToArray());
    }

    public Task<CompCube_Models.Models.ClientData.UserInfo[]?> GetAroundUser(string id)
    {
        return Task.FromResult(Array.Empty<CompCube_Models.Models.ClientData.UserInfo>());
    }

    public Task<ServerStatus?> GetServerStatus()
    {
        return Task.FromResult(new ServerStatus([UnityGame.GameVersion.ToString()], ["1.0.0"],
            ServerState.State.Online));
    }

    public async Task<string[]?> GetMapHashes()
    {
        await Task.Delay(1000);
        return Maps.Select(i => i.Hash).ToArray();
    }

    public async Task<EventData[]?> GetEvents()
    {
        await Task.Delay(500);
        return [];
    }
}