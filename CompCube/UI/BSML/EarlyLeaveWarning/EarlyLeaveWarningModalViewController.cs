using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using UnityEngine;

namespace CompCube.UI.BSML.EarlyLeaveWarning;

[ViewDefinition("CompCube.UI.BSML.EarlyLeaveWarning.EarlyLeaveWarningModalView.bsml")]
public class EarlyLeaveWarningModalViewController : BSMLAutomaticViewController
{
    private Action? _onDisconnectButtonPressedCallback = null;
    
    [UIParams] private readonly BSMLParserParams _parserParams = null!;

    public void ParseOntoGameObject(ViewController viewController, string text, Action? onDisconnectButtonPressedCallback)
    {
        _onDisconnectButtonPressedCallback = onDisconnectButtonPressedCallback;

        ModalText = text;
        
        BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "CompCube.UI.BSML.EarlyLeaveWarning.EarlyLeaveWarningModalView.bsml"), viewController.gameObject, this);
        Show();
    }

    [UIValue("modalText")] private string ModalText { get; set; } = "";

    public void Show()
    {
        _parserParams.EmitEvent("showModal");
    }

    [UIAction("onDisconnectButtonClicked")]
    private void OnDisconnectButtonPressed()
    {
        _onDisconnectButtonPressedCallback?.Invoke();
        _onDisconnectButtonPressedCallback = null;
    }

    [UIAction("onContinueButtonClicked")]
    private void OnContinueButtonPressed()
    {
        _parserParams.EmitEvent("hideModal");
    }
}