using System.Diagnostics;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube_Models.Models.Packets.ServerPackets;
using CompCube.Configuration;
using CompCube.Interfaces;
using CompCube.UI.BSML.Components;
using SiraUtil.Logging;
using UnityEngine.UI;
using Zenject;

namespace CompCube.UI.BSML.Menu
{
    [ViewDefinition("CompCube.UI.BSML.Menu.MatchmakingMenuView.bsml")]
    public class MatchmakingMenuViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly IServerListener _serverListener = null!;
        [Inject] private readonly SiraLog _siraLog = null!;

        [UIParams] private readonly BSMLParserParams _parserParams = null!;

        public event Action? AboutButtonClicked;

        public event Action? EventsButtonClicked;

        private bool _isQueued = false;
        
        [UIValue("queueOptions")] 
        private readonly List<object> _queueOptions = [new QueueOptionTab("Casual 1v1", "standard_casual_1v1"), new QueueOptionTab("Competitive 1v1", "standard_competitive_1v1")];

        [UIComponent("queueTabSelector")] private readonly TabSelector _queueTabSelector = null!;

        [UIValue("is-queued")]
        private bool IsInMatchmakingQueue
        {
            get => _isQueued;
            set
            {
                _isQueued = value;
                NotifyPropertyChanged(null);
            }
        }

        [UIValue("is-not-queued")]
        private bool IsNotInMatchmakingQueue => !IsInMatchmakingQueue;

        [UIValue("failedToConnectReason")] private string FailedToConnectReason { get; set; } = "";

        [UIAction("joinMatchmakingPoolButtonOnClick")]
        private void HandleJoinMatchmakingPoolClicked()
        {
            IsInMatchmakingQueue = true;
            
            _serverListener.Connect(((QueueOptionTab) _queueOptions[_queueTabSelector.TextSegmentedControl.selectedCellNumber]).Queue, (response) =>
            {
                if (response.Successful) 
                    return;
                
                _parserParams.EmitEvent("failedToConnectModalShow");
                FailedToConnectReason = $"Reason: {response.Message}";
                NotifyPropertyChanged(nameof(FailedToConnectReason));
            });
        }

        [UIAction("leaveMatchmakingPoolButtonOnClick")]
        private void HandleLeaveMatchmakingPoolButtonClicked() => _parserParams.EmitEvent("disconnectModalShowEvent");
        
        [UIAction("leaveMatchmakingPoolDenyButtonOnClick")] 
        private void HandleLeaveMatchmakingPoolDenied() => _parserParams.EmitEvent("disconnectModalHideEvent");

        [UIAction("leaveMatchmakingPoolAllowButtonOnClick")]
        private void LeaveMatchmakingPoolAllowButton()
        {
            _parserParams.EmitEvent("disconnectModalHideEvent");
            _serverListener.Disconnect();
        }

        [UIAction("failedToConnectModalOkButtonOnClick")]
        private void HandleFailedToConnectModalOkButtonClicked() => _parserParams.EmitEvent("failedToConnectModalHide");

        public void Initialize()
        {
            _serverListener.OnDisconnected += HandleDisconnected;
            
            if (!_config.ConnectToDebugQueue)
                return;
            
            if (_queueOptions.Any(i => ((QueueOptionTab) i).Queue == "debug"))
                return;
            
            _queueOptions.Add(new QueueOptionTab("Debug", "debug"));
        }

        private void HandleDisconnected() => IsInMatchmakingQueue = false;

        public void Dispose()
        {
            _serverListener.OnDisconnected -= HandleDisconnected;
        }
    }
}