using System.Collections;
using System.Reflection;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace CompCube.Extensions;

public static class FlowCoordinatorExtensions
{
    internal static void ReplaceViewControllerSynchronously(this FlowCoordinator flowCoordinator, ViewController viewController, bool immediately = false)
    {
        if (!flowCoordinator.isActivated) 
            return;

        if (flowCoordinator.topViewController == viewController)
            return;

        if (flowCoordinator.isInTransition)
            return;
            
        flowCoordinator.StartCoroutine(PresentViewControllerSynchronouslyCoroutine(flowCoordinator, viewController, immediately: immediately));
    }
    
    private static IEnumerator PresentViewControllerSynchronouslyCoroutine(FlowCoordinator flowCoordinator, ViewController viewController, bool immediately)
    {
        yield return new WaitForEndOfFrame();
        
        flowCoordinator.GetType().GetMethod("ReplaceTopViewController", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(flowCoordinator,
            [viewController, null, immediately ? ViewController.AnimationType.None : ViewController.AnimationType.In, ViewController.AnimationDirection.Horizontal]);
        // flowCoordinator.ReplaceTopViewController(viewController, animationType: immediately ? ViewController.AnimationType.None : ViewController.AnimationType.In);
    }

    public static void SetBackButtonInteractivity(this FlowCoordinator flowCoordinator, bool interactable)
    {
        var screenSystem = flowCoordinator.GetField<ScreenSystem, FlowCoordinator>("_screenSystem");
        screenSystem.GetField<Button, ScreenSystem>("_backButton").interactable = interactable;
    }

    public static void PresentFlowCoordinatorSynchronously(this FlowCoordinator parent, FlowCoordinator flowCoordinator, bool immediately = false)
    {
        while (parent.isInTransition);
        
        parent.StartCoroutine(PresentFlowCoordinatorCoroutine(parent, flowCoordinator, immediately));
    }

    private static IEnumerator PresentFlowCoordinatorCoroutine(FlowCoordinator parent, FlowCoordinator flowCoordinator, bool immediately)
    {
        yield return new WaitForEndOfFrame();
        
        parent.PresentFlowCoordinator(flowCoordinator, immediately: immediately);
    }
    
    public static void DismissAllChildFlowCoordinators(this FlowCoordinator flowCoordinator)
    {
        var parent = flowCoordinator;
        
        if (flowCoordinator is not MainFlowCoordinator)
            parent = (FlowCoordinator) flowCoordinator.GetType()
                 .GetField("_parentFlowCoordinator", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(flowCoordinator);
        
        parent?.GetType().GetMethod("DismissChildFlowCoordinatorsRecursively", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(parent,
             [false]);
    }
}