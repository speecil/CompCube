using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace CompCube.Configuration
{
    public class PluginConfig
    {
        public virtual string ServerIp { get; set; } = "71.115.177.153";
        public virtual int ServerPort { get; set; } = 8008;
        public virtual int ServerApiPort { get; set; } = 7198;
        
        public virtual bool DownloadMapsAutomatically { get; set; } = false;

        public virtual bool ScoreSubmission { get; set; } = true;

        public virtual bool ConnectToDebugQueue { get; set; } = false;
        public virtual bool SkipServer { get; set; } = false;
        
        public virtual bool SkipServerCertificateValidation { get; set; } = false;
    }
}