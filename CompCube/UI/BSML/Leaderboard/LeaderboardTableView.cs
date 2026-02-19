using HMUI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompCube.Extensions;
using SiraUtil.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace CompCube.UI.BSML.Leaderboard;

public class PlayerCellAvatarState : MonoBehaviour
{
    public int CurrentIndex = -1;
    public string CurrentAvatarUrl;

    public Task LoadTask;
    public CancellationTokenSource CancellationTokenSource;
}

public class PlayerCellDataSource : MonoBehaviour, TableView.IDataSource
{
    [Inject] private DiContainer _diContainer = null;
    [Inject] private ICoroutineStarter _coroutineStarter = null;
    [Inject] private IHttpService _httpService = null;

    private float lastPos = float.MaxValue;

    public TableView TableView { get; private set; }

    internal List<CompCube_Models.Models.ClientData.UserInfo> Data { get; private set; } = new();

    private LevelListTableCell _tableCellPrefab;
    private bool firstLoad = true;

    internal void Init(TableView tableView)
    {
        TableView = tableView;
    }

    #region Data

    internal void SetData(List<CompCube_Models.Models.ClientData.UserInfo> users)
    {
        Data = users;
        TableView.ReloadData();

        if (firstLoad)
        {
            firstLoad = false;
            _coroutineStarter.StartCoroutine(OneFrameReload());
        }
    }
    
    internal void AddData(List<CompCube_Models.Models.ClientData.UserInfo> users, CancellationToken token = default) {
        Data.AddRange(users);

        var scrollView = TableView.scrollView;

        var oldPos = scrollView.position;

        _ = WaitForTableScrollToFinishAsync(async() => {
            await UnityGame.SwitchToMainThreadAsync();
            TableView.ReloadData();
            scrollView.ScrollTo(lastPos, false);
        }, token);
    }
    
    private async Task WaitForTableScrollToFinishAsync(Action onComplete, CancellationToken token) {
        for (int i = 0; i < 5 && !token.IsCancellationRequested; i++) {
            while (!Check()) {
                await Task.Yield();
            }
            await Task.Delay(5, token);
        }

        if (token.IsCancellationRequested) return;

        onComplete?.Invoke();

        bool Check() {
            float current = TableView.scrollView.position;
            bool stopped = Mathf.Abs(current - lastPos) < 0.001f;
            lastPos = current;
            return stopped;
        }
    }

    private IEnumerator OneFrameReload()
    {
        while (TableView == null || !TableView.isActiveAndEnabled || !TableView.visibleCells.Any())
            yield return null;

        TableView.ReloadData();
    }

    #endregion

    #region TableView

    public float CellSize(int idx) => 8.6f;

    public int NumberOfCells() => Data.Count;

    public TableCell CellForIdx(TableView tableView, int idx)
    {
        var cell = (LevelListTableCell)tableView.DequeueReusableCellForIdentifier("PlayerListTableCell");

        if (cell == null)
        {
            _tableCellPrefab ??= CreateTableCellPrefab();
            cell = Instantiate(_tableCellPrefab);
            cell.reuseIdentifier = "PlayerListTableCell";
        }

        CleanupCell(cell);

        var user = Data[idx];

        ConfigureText(cell, user);
        _ = ConfigureAvatarAsync(cell, user, idx, CancellationToken.None);

        cell.selectionDidChangeEvent -= CellSelectionDidChange;
        cell.selectionDidChangeEvent += CellSelectionDidChange;

        _coroutineStarter.StartCoroutine(ApplyCellLayoutNextFrame(cell));
        return cell;
    }

    private IEnumerator ApplyCellLayoutNextFrame(LevelListTableCell cell)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        
        cell._songBpmText.transform.localPosition = new Vector3(
            35.45f,
            cell._songBpmText.transform.localPosition.y,
            cell._songBpmText.transform.localPosition.z
        );
        
        cell.RefreshVisuals();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cell.transform as RectTransform);
    }

    #endregion

    #region Cell Creation

    private LevelListTableCell CreateTableCellPrefab()
    {
        var template = Resources.FindObjectsOfTypeAll<LevelCollectionViewController>()
            .First()
            .transform
            .Find("LevelsTableView/TableView/Viewport/Content/LevelListTableCell")
            .gameObject;

        var gameObj = Instantiate(template);
        gameObj.name = "PlayerListTableCellPrefab";

        var cell = gameObj.GetComponent<LevelListTableCell>();

        CleanupCell(cell);

        cell._songNameText.color =
        cell._songAuthorText.color =
        cell._songDurationText.color =
        cell._songBpmText.color = Color.white;

        return cell;
    }

    private void CleanupCell(LevelListTableCell cell)
    {
        if (cell._promoBadgeGo) Object.DestroyImmediate(cell._promoBadgeGo);
        if (cell._updatedBadgeGo) Object.DestroyImmediate(cell._updatedBadgeGo);
        if (cell._favoritesBadgeImage?.gameObject) Object.DestroyImmediate(cell._favoritesBadgeImage.gameObject);
        if (cell.transform.Find("BpmIcon")?.gameObject) Object.DestroyImmediate(cell.transform.Find("BpmIcon").gameObject);
    }

    #endregion

    #region Text

    private void ConfigureText(LevelListTableCell cell, CompCube_Models.Models.ClientData.UserInfo user)
    {
        string name = $"#{user.Rank} - {user.GetFormattedUserName()}";
        string divisionText = $"<color={user.Division.Color}>{user.Division.Division}</color> {user.Mmr} MMR";

        int losses = user.TotalGames - user.Wins;
        string recordText = $"{user.Wins}<color=green>W</color> / {losses}<color=red>L</color>";

        cell._songNameText.SetText(name);
        cell._songAuthorText.SetText(divisionText);
        cell._songDurationText.SetText(recordText);
        cell._songBpmText.SetText($"WS: {user.Winstreak}");

        cell._songAuthorText.richText = true;
        cell._songBpmText.richText = true;
        cell._songDurationText.richText = true;
        cell._songNameText.richText = true;

        var bg = cell._highlightBackgroundColor;
        cell._selectedBackgroundColor = bg;
        cell._selectedAndHighlightedBackgroundColor = bg;
    }

    #endregion

    #region Avatar & Badge Loading

    private async Task ConfigureAvatarAsync(LevelListTableCell cell,
        CompCube_Models.Models.ClientData.UserInfo user,
        int idx,
        CancellationToken token)
    {
        var state = cell.GetComponent<PlayerCellAvatarState>() ??
                    cell.gameObject.AddComponent<PlayerCellAvatarState>();

        string avatarUrl = user.ProfilePictureLink;

        if (state.CurrentIndex == idx &&
            state.CurrentAvatarUrl == avatarUrl)
            return;

        state.CancellationTokenSource?.Cancel();
        state.CancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(token);

        state.CurrentIndex = idx;
        state.CurrentAvatarUrl = avatarUrl;

        var coverImage = cell._coverImage as ImageView;

        coverImage.sprite = null;

        state.LoadTask = LoadImagesForCellAsync(cell, state, avatarUrl, idx,
            state.CancellationTokenSource.Token);

        await state.LoadTask;
    }

    private async Task LoadImagesForCellAsync(
        LevelListTableCell cell,
        PlayerCellAvatarState state,
        string avatarUrl,
        int idx,
        CancellationToken token)
    {
        Sprite avatarSprite = null;

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            IHttpResponse response = await _httpService.GetAsync(avatarUrl, null, token);
            if (response.Successful)
            {
                byte[] imgArray = await response.ReadAsByteArrayAsync();
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(imgArray, false);
                avatarSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    Vector2.one * 0.5f
                );
            }
        }

        if (token.IsCancellationRequested || state.CurrentIndex != idx)
            return;

        await UnityGame.SwitchToMainThreadAsync();

        var coverImage = cell._coverImage as ImageView;

        coverImage.sprite = avatarSprite;

        coverImage?.SetAllDirty();
    }

    #endregion

    #region Selection

    private void CellSelectionDidChange(SelectableCell cell,
        SelectableCell.TransitionType _,
        object __)
    {
        _coroutineStarter.StartCoroutine(DeselectNextFrame(cell));
    }

    private IEnumerator DeselectNextFrame(SelectableCell cell)
    {
        yield return null;
        cell.SetSelected(false,
            SelectableCell.TransitionType.Instant,
            this,
            false);
    }

    #endregion
}