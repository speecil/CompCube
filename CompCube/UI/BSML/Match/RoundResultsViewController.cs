using System.Collections;
using System.Globalization;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube_Models.Models.Match;
using CompCube_Models.Models.Packets.ServerPackets;
using JetBrains.Annotations;
using CompCube.Extensions;
using UnityEngine;
using Zenject;

namespace CompCube.UI.BSML.Match
{
    [ViewDefinition("CompCube.UI.BSML.Match.RoundResultsView.bsml")]
    public class RoundResultsViewController : BSMLAutomaticViewController
    {
        private Action? _onContinueButtonPressedCallback = null;
        
        [UIValue("titleBgColor")] private string TitleBgColor { get; set; } = "#0000FF";
        [UIValue("titleText")] private string TitleText { get; set; } = "Results";
        
        [UIValue("winnerScoreText")] private string WinnerScoreText { get; set; }
        [UIValue("loserScoreText")] private string LoserScoreText { get; set; }
        
        public void PopulateData(RoundResultsPacket results)
        {
            WinnerScoreText = FormatScore(new MatchScore(results.Scores.ElementAt(0).Key, results.Scores.ElementAt(0).Value), 1);
            LoserScoreText = FormatScore(new MatchScore(results.Scores.ElementAt(1).Key, results.Scores.ElementAt(1).Value), 2);
            
            NotifyPropertyChanged(null);
        }

        public void SetContinueButtonCallback(Action? onContinueButtonPressedCallback)
        {
            _onContinueButtonPressedCallback = onContinueButtonPressedCallback;
        }

        private string FormatScore(MatchScore score, int placement) => 
            $"{(placement)}. {score.User.GetFormattedUserName()} - " +
            $"{(score.Score?.RelativeScore * 100):F}% " +
            $"{(score.Score.FullCombo ? "FC".FormatWithHtmlColor("#90EE90") : $"{score.Score.Misses}x".FormatWithHtmlColor("#FF7F7F"))}" +
            $"{(score.Score.ProMode ? " (PM)" : "")}";

        [UIAction("continueButtonClicked")]
        private void ContinueButtonClicked()
        {
            _onContinueButtonPressedCallback?.Invoke();
            _onContinueButtonPressedCallback = null;
        }
    }
}