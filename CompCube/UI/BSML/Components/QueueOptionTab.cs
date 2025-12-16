using BeatSaberMarkupLanguage.Attributes;

namespace CompCube.UI.BSML.Components;

public class QueueOptionTab(string name, string queue)
{
    [UIValue("tabName")] public string TabName { get; private set; } = name;
    
    public readonly string Queue = queue;
}