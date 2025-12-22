using System.Collections;
using System.Globalization;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube_Models.Models.Map;
using CompCube.Configuration;
using CompCube.UI.BSML.Components.CustomLevelBar;
using HMUI;
using CompCube.Extensions;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Match
{
    [ViewDefinition("CompCube.UI.BSML.Match.WaitingForMatchToStartView.bsml")]
    public class WaitingForMatchToStartViewController(BeatmapDataLoader beatmapDataLoader) : BSMLAutomaticViewController, ITickable
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _siraLog = null!;
        
        [UIValue("matchStartTimer")] private string MatchStartTimer { get; set; } = "";

        [UIValue("scoreSubmission")]
        private bool ScoreSubmission
        {
            get => _config.ScoreSubmission;
            set => _config.ScoreSubmission = value;
        }
        
        
        [UIComponent("difficultySegmentData")] private readonly TextSegmentedControl _difficultySegmentData = null!;
        [UIComponent("categorySegmentData")] private readonly TextSegmentedControl _categorySegmentData = null!;

        private CustomLevelBar? _customLevelBar = null;
         
        private DateTime? _startTime;

        private Action? _postParseCallback = null;

        [UIAction("#post-parse")]
        private void PostParse()
        {
            _customLevelBar ??= Resources.FindObjectsOfTypeAll<CustomLevelBar>()
                .First(i => i.name == "WaitingForMatchStartLevelBar");
            
            _postParseCallback?.Invoke();
        }
        
        [UIAction("nothing")]
        private void Nothing(SegmentedControl _, int cell){}

        public void SetPostParseCallback(Action callback)
        {
            if (_customLevelBar is null)
            {
                _postParseCallback = callback;
                return;
            }
            
            callback?.Invoke();
        }

        [UIValue("songDurationText")] private string SongDurationText { get; set; } = "";
        [UIValue("songBpmText")] private string SongBpmText { get; set; } = "";
        [UIValue("songNpsText")] private string SongNpsText { get; set; } = "";
        [UIValue("songNjsText")] private string SongNjsText { get; set; } = "";
        [UIValue("songJdText")] private string SongJdText { get; set; } = "";
        [UIValue("songNoteCountText")] private string SongNoteCountText { get; set; } = "";
        [UIValue("songWallCountText")] private string SongWallCountText { get; set; } = "";
        [UIValue("songBombCountText")] private string SongBombCountText { get; set; } = "";
        
        public void PopulateData(VotingMap votingMap, DateTime? startTime)
        {
            _startTime = startTime;
            
            _customLevelBar?.Setup(votingMap);

            var beatmap = votingMap.GetBeatmapLevel();
            var key = votingMap.GetBeatmapKey();

            // var data = beatmapDataLoader.LoadBeatmapData();
            // var basicData = beatmapDataLoader.LoadBasicBeatmapData();
            
            _siraLog.Info(key.beatmapCharacteristic.serializedName + " " + key.difficulty);

            var bpm = beatmap?.beatsPerMinute ?? 0;
            // var njs = 
            // var offset = 
            
            SongDurationText = $"{(int) beatmap?.songDuration! / 60}:{(int) beatmap?.songDuration % 60}";
            SongBpmText = Mathf.RoundToInt(bpm).ToString();
            // SongNjsText = $"{njs}";
            // SongNpsText = $"{(data?.cuttableNotesCount / beatmap.songDuration)::F1}";
            // SongJdText = $"{GetJumpDistance(bpm, njs, startOffset)}";
            // SongNoteCountText = $"{data?.cuttableNotesCount}";
            // SongWallCountText = $"{data?.obstaclesCount}";
            // SongBombCountText = $"{data?.bombsCount}";
            
            NotifyPropertyChanged(null);

            if (startTime == null)
            {
                MatchStartTimer = "Starting soon. Please wait!";
            }

            StartCoroutine(UpdateTexts());

            return;

            IEnumerator UpdateTexts()
            {
                yield return new WaitForEndOfFrame();
                
                _difficultySegmentData.SetTexts([votingMap.GetBaseGameDifficultyType().Name()]);
                _categorySegmentData.SetTexts(["Category: " + votingMap.CategoryLabel]);
            }
        }

        private float GetJumpDistance(float bpm, float njs, float startBeatOffset)
        {
            return njs * (60f / bpm) * Hjd(bpm, njs, startBeatOffset) * 2;

            float Hjd(float bpm, float njs, float offset)
            {
                var num = 60f / bpm;
                var hjd = 4f;
                while (njs * num * hjd > 17.999f)
                    hjd /= 2f;

                hjd += offset;

                return Math.Max(hjd, 0.25f);
            }
        }

        public void Tick()
        {
            if (!isActivated)
                return;

            if (_startTime == null)
                return;
            
            MatchStartTimer = $"Starting in {((int) (_startTime.Value - DateTime.UtcNow).TotalSeconds + 1).ToString(CultureInfo.InvariantCulture)}...";
            
            NotifyPropertyChanged(nameof(MatchStartTimer));
        }
    }
}