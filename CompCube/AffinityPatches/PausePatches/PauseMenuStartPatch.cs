using CompCube.Game;
using SiraUtil.Affinity;
using Zenject;

namespace CompCube.AffinityPatches.PausePatches
{
    public class PauseMenuStartPatch : IAffinity
    {
        [AffinityPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.Start))]
        [AffinityPostfix]
        private void Postfix(PauseMenuManager __instance)
        {
            __instance._restartButton.gameObject.SetActive(false);
            __instance._backButton.gameObject.SetActive(false);
            __instance._continueButton.gameObject.SetActive(false);
        }
    }
}