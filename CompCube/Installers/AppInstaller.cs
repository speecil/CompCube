using System.ComponentModel;
using CompCube.Configuration;
using CompCube.Game;
using CompCube.Server;
using CompCube.Server.Debug;
using CompCube.Interfaces;
using Zenject;

namespace CompCube.Installers
{
    internal class AppInstaller : Installer
    {
        private readonly PluginConfig _config;

        public AppInstaller(PluginConfig config)
        {
            _config = config;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(_config);
            
            Container.BindInterfacesAndSelfTo<MatchManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<DisconnectHandler>().AsSingle();
            Container.BindInterfacesAndSelfTo<MatchStateManager>().AsSingle();
            
            Container.BindInterfacesAndSelfTo<UserModelWrapper>().AsSingle();

            if (_config.SkipServer)
            {
                Container.BindInterfacesAndSelfTo<DebugServerListener>().AsSingle();
                Container.BindInterfacesAndSelfTo<DebugApi>().AsSingle();
                return;
            }
            
            Container.BindInterfacesAndSelfTo<ServerListener>().AsSingle();
            Container.BindInterfacesAndSelfTo<Api>().AsSingle();
        }
    }
}