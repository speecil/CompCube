using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.UI.BSML.Components;
using SiraUtil.Extras;

namespace CompCube.UI.BSML;

public abstract class TabSwitcherViewController : BSMLViewController
{
    public override string Content =>
        Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "CompCube.UI.BSML.TabSwitcherView.bsml");
    
    [UIComponent("tabSelector")]
    protected readonly TabSelector TabSelector = null!;
    
    [UIValue("tabItems")]
    protected List<object> TabItems => Tabs.Cast<object>().ToList();
    
    protected abstract List<ViewControllerTab> Tabs { get; }

    [UIAction("#post-parse")]
    protected void PostParse()
    {
        TabSelector.TextSegmentedControl.ReloadData();
        TabSelector.Refresh();
        
        Tabs[0].Refresh();
    }

    [UIAction("onCellSelected")]
    protected void OnCellSelected(object _, int index)
    {
        Tabs[index].Refresh();
    }
}