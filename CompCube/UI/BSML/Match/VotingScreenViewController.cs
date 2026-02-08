using System.Collections;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube_Models.Models.Map;
using CompCube_Models.Models.Packets.ServerPackets;
using HMUI;
using JetBrains.Annotations;
using CompCube.Extensions;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Match;

[ViewDefinition("CompCube.UI.BSML.Match.VotingScreenView.bsml")]
public class VotingScreenViewController : BSMLAutomaticViewController
{
    [Inject] private readonly SiraLog _log = null!;
    
    public event Action<VotingMap, List<VotingMap>>? MapSelected;
    public event Action<List<VotingMap>>? RanOutOfTime;

    [UIComponent("mapList")] private readonly CustomListTableData _mapListTableData = null!;
    private VotingListDataSource _votingListDataSource = null!;

    [UIComponent("voteStatusText")] private readonly TextMeshProUGUI _voteStatusText = null!;
    
    private Action? _activationCallback = null;
    
    private DateTime? _timeWhenCountdownWillEnd = null;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        
        _activationCallback?.Invoke();
        _activationCallback = null;
    }

    public void SetActivationCallback(Action callback)
    {
        if (isActivated)
        {
            callback?.Invoke();
            return;
        }
        
        _activationCallback = callback;
    }

    [UIAction("#post-parse")]
    void PostParse()
    {
        _votingListDataSource = gameObject.AddComponent<VotingListDataSource>();
        _mapListTableData.TableView.SetDataSource(_votingListDataSource, true);
        
        _votingListDataSource.Init(_mapListTableData.TableView);
        
        Destroy(_mapListTableData);
        
        _votingListDataSource.TableView.didSelectCellWithIdxEvent += DidSelectCellWithIdxEvent;
    }

    private void DidSelectCellWithIdxEvent(TableView tableView, int idx)
    {
        MapSelected?.Invoke(_votingListDataSource.Data[idx], _votingListDataSource.Data);
    }

    public void SetCountdownTime(DateTime timeWhenCountdownWillEnd)
    {
        _timeWhenCountdownWillEnd = timeWhenCountdownWillEnd;
    }

    public void PopulateData(VotingMap[] maps)
    {
        _log.Notice("Populating maps");
        StartCoroutine(PopulateDataCoroutine());
        return;
        
        IEnumerator PopulateDataCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            _votingListDataSource.SetData(maps.ToList());
            _votingListDataSource.TableView.ClearSelection();

            yield return CountDown();
        }

        IEnumerator CountDown()
        {
            _log.Notice("Counting down");
            while (true)
            {
                if (_timeWhenCountdownWillEnd == null)
                    yield return null;
                
                var remaining = (_timeWhenCountdownWillEnd ?? DateTime.Now.AddSeconds(10)) - DateTime.Now;
                if (remaining.TotalSeconds <= 0)
                    break;

                _voteStatusText.text =
                    $"Please vote on a map to play!\nTime left: {Mathf.CeilToInt((float)remaining.TotalSeconds)}";

                yield return null;
            }
            RanOutOfTime?.Invoke(maps.ToList());
        }
    }
}
    
public class VotingListDataSource : MonoBehaviour, TableView.IDataSource
{
    public TableView TableView { get; private set; }
        
    public List<VotingMap> Data { get; private set; } = new();

    private LevelListTableCell _tableCellPrefab;

    private LevelListTableCell CreateTableCellPrefab()
    {
        var gameObj = Instantiate(
            Resources.FindObjectsOfTypeAll<LevelCollectionViewController>()
                .First()
                .transform
                .Find("LevelsTableView/TableView/Viewport/Content/LevelListTableCell")
                .gameObject);
            
        gameObj.name = "MyListCell";

        var cell = gameObj.GetComponent<LevelListTableCell>();
        return cell;
    }
        
    public void Init(TableView tableView) => TableView = tableView;

    public void SetData(List<VotingMap> maps)
    {
        Data = maps;
        TableView.ReloadData();
    }

    public float CellSize(int idx) => 8.5f;

    public int NumberOfCells() => Data.Count;

    public TableCell CellForIdx(TableView tableView, int idx)
    {
        var cell = (LevelListTableCell) tableView.DequeueReusableCellForIdentifier("VotingListTableCell");

        if (cell is null)
        {
            _tableCellPrefab ??= CreateTableCellPrefab();
            cell = Instantiate(_tableCellPrefab);
            cell.reuseIdentifier = "VotingListTableCell";
        }

        var info = Data[idx];
        cell.SetDataFromLevelAsync(info.GetBeatmapLevel(), false,false, false, true);

        return cell;
    }
}