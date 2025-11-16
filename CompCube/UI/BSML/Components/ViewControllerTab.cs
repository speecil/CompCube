using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CompCube.Interfaces;
using UnityEngine;

namespace CompCube.UI.BSML.Components;

public class ViewControllerTab
{
    [UIValue("tabName")] 
    private readonly string _tabName;

    [UIObject("tabObject")] 
    private readonly GameObject _gameObject;

    private readonly BSMLAutomaticViewController _host;
    
    public ViewControllerTab(string tabName, BSMLAutomaticViewController host)
    {
        _host = host;
        _tabName = tabName;
    }

    [UIAction("#post-parse")]
    private void PostParse()
    {
        BSMLParser.Instance.Parse(_host.Content, _gameObject, _host);
    }

    public void Refresh()
    {
        if (_host is not IRefreshableView refreshableHost)
            return;
        
        refreshableHost.Refresh();
    }
}