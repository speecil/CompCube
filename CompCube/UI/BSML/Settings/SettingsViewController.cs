using System.Net;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Configuration;
using Zenject;

namespace CompCube.UI.BSML.Settings;

[ViewDefinition("CompCube.UI.BSML.Settings.SettingsView.bsml")]
public class SettingsViewController : BSMLAutomaticViewController, IInitializable, IDisposable
{
    [Inject] private readonly BSMLSettings _bsmlSettings = null;
    [Inject] private readonly PluginConfig _config = null;

    [UIParams] private readonly BSMLParserParams _parserParams = null;

    [UIValue("serverIp")]
    private string ServerIp
    {
        get => _config.ServerIp;
        set
        {
            if (!IPAddress.TryParse(value, out _))
            {
                _parserParams.EmitEvent("invalidValueModalShow");
                return;
            }

            _config.ServerIp = ServerIp;
        }
    }

    [UIValue("serverPort")]
    private string ServerPort
    {
        get => _config.ServerPort.ToString();
        set
        {
            if (!int.TryParse(value, out var port))
            {
                _parserParams.EmitEvent("invalidValueModalShow");
                return;
            }

            _config.ServerPort = port;
        }
    }

    [UIValue("apiPort")]
    private string ApiPort
    {
        get => _config.ServerApiPort.ToString();
        set
        {
            if (!int.TryParse(value, out var port))
            {
                _parserParams.EmitEvent("invalidValueModalShow");
                return;
            }

            _config.ServerApiPort = port;
        }
    }

    [UIValue("mapAutoDownload")]
    private bool MapAutoDownload
    {
        get => _config.DownloadMapsAutomatically;
        set => _config.DownloadMapsAutomatically = value;
    }

    [UIValue("scoreSubmission")]
    private bool ScoreSubmission
    {
        get => _config.ScoreSubmission;
        set => _config.ScoreSubmission = value;
    }

    [UIAction("invalidValueModalOkButtonOnClick")]
    private void OkButtonOnClick() => _parserParams.EmitEvent("invalidValueModalHide");

    public void Initialize() => _bsmlSettings.AddSettingsMenu("CompCube", "CompCube.UI.BSML.Settings.SettingsView.bsml", this);

    public void Dispose() => _bsmlSettings.RemoveSettingsMenu(this);
}