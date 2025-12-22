using CompCube.UI.BSML.Disconnect;
using HMUI;
using CompCube.Extensions;
using Zenject;

namespace CompCube.UI.FlowCoordinators;

public class DisconnectFlowCoordinator : FlowCoordinator
{
    [Inject] private readonly DisconnectedViewController _disconnectedViewController = null!;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        SetTitle("CompCube");
        showBackButton = false;
        ProvideInitialViewControllers(_disconnectedViewController);
    }

    public void Setup(string reason, Action callback)
    {
        _disconnectedViewController.SetReason(reason, callback);
    }
}