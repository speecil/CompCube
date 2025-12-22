using CompCube.UI.BSML.Info;
using HMUI;
using CompCube.Extensions;
using Zenject;

namespace CompCube.UI.FlowCoordinators;

public class InfoFlowCoordinator : FlowCoordinator
{
    [Inject] private readonly InfoViewController _infoViewController = null!;
    
    public event Action? OnBackButtonPressed;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        SetTitle("CompCube");
        ProvideInitialViewControllers(_infoViewController);
        showBackButton = true;
    }

    protected override void BackButtonWasPressed(ViewController _) => OnBackButtonPressed?.Invoke();
}