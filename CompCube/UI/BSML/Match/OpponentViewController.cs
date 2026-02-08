using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Extensions;

namespace CompCube.UI.BSML.Match;

[ViewDefinition("CompCube.UI.BSML.Match.OpponentView.bsml")]
public class OpponentViewController : BSMLAutomaticViewController
{
    [UIValue("roundText")] private string RoundText { get; set; }
    [UIValue("pointsText")] private string PointsText { get; set; }
    [UIValue("redText")] private string RedText { get; set; }
    [UIValue("blueText")] private string BlueText { get; set; }
    
    public void PopulateData(CompCube_Models.Models.ClientData.UserInfo[] redTeam, CompCube_Models.Models.ClientData.UserInfo[] blueTeam)
    {
        RedText = redTeam[0].GetFormattedUserName();
        BlueText = blueTeam[0].GetFormattedUserName();
        
        NotifyPropertyChanged(null);
    }

    public void UpdatePoints(int redPoints, int bluePoints)
    {
        PointsText = $"{redPoints} - {bluePoints}";
        NotifyPropertyChanged(nameof(PointsText));
    }

    public void UpdateRound(int round)
    {
        RoundText = $"Round {round}";
        NotifyPropertyChanged(nameof(RoundText));
    }

    public void SetStatus(string status)
    {
        RoundText = status;
        NotifyPropertyChanged(nameof(RoundText));
    }
}