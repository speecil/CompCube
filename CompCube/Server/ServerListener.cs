using System.Net;
using System.Net.Sockets;
using System.Text;
using CompCube_Models.Models.Packets;
using CompCube_Models.Models.Packets.ServerPackets;
using CompCube_Models.Models.Packets.ServerPackets.Event;
using CompCube_Models.Models.Packets.UserPackets;
using CompCube.Configuration;
using CompCube.Game;
using CompCube.Interfaces;
using SiraUtil.Logging;
using Zenject;

namespace CompCube.Server
{
    public class ServerListener : IServerListener, IDisposable
    {
        [Inject] private readonly PluginConfig _config = null!;
        [Inject] private readonly SiraLog _siraLog = null!;

        private TcpClient _client = new();
        private Thread? _listenerThread;

        private bool _shouldListenToServer = false;
        
        public event Action<MatchCreatedPacket>? OnMatchCreated;
        public event Action<PlayerVotedPacket>? OnPlayerVoted;
        public event Action<BeginGameTransitionPacket>? OnBeginGameTransition;
        public event Action<RoundResultsPacket>? OnRoundResults;
        public event Action<RoundStartedPacket>? OnRoundStarted;
        public event Action<UserDisconnectedPacket>? OnUserDisconnected;
        public event Action<MatchResultsPacket>? OnMatchResults;

        public event Action? OnDisconnected;
        public event Action? OnConnected;
        public event Action<PrematureMatchEndPacket>? OnPrematureMatchEnd;
        
        public event Action<EventStartedPacket>? OnEventStarted;
        public event Action<EventMapSelected>? OnEventMapSelected;
        public event Action<EventMatchStartedPacket>? OnEventMatchStarted;
        public event Action<EventClosedPacket>? OnEventClosed;
        public event Action<EventScoresUpdated>? OnEventScoresUpdated;

        [Inject] private readonly UserModelWrapper _userModelWrapper = null!;

        public bool Connected
        {
            get
            {
                try
                {
                    if (!_client.Connected)
                        return false;

                    var blockingState = _client.Client.Blocking;
                    _client.Client.Blocking = false;
                    _client.Client.Send([], 0, SocketFlags.None);
                    _client.Client.Blocking = blockingState;

                    return true;
                }
                catch (SocketException e)
                {
                    return e.NativeErrorCode == 10035;
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }

                return false;
            }
        }

        public async Task Connect(string queue, Action<JoinResponsePacket> onConnectedCallBack)
        {
            if (Connected)
            {
                _siraLog.Error("Tried to connect to server while connected!");
                return;
            }
            
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(_config.ServerIp), _config.ServerPort);
                
                //todo: change this to not be standard by default
                await SendPacket(new JoinRequestPacket(_userModelWrapper.UserName, _userModelWrapper.UserId, queue));

                while (!_client.GetStream().DataAvailable)
                    await Task.Delay(25);
                
                var bytes = new byte[1024];
                
                var bytesRead = _client.GetStream().Read(bytes, 0, bytes.Length);
                Array.Resize(ref bytes, bytesRead);
                
                _siraLog.Info(Encoding.UTF8.GetString(bytes));

                if (ServerPacket.Deserialize(Encoding.UTF8.GetString(bytes)) is not JoinResponsePacket responsePacket)
                    return;
                
                onConnectedCallBack.Invoke(responsePacket);

                if (responsePacket.Successful)
                {
                    _listenerThread = new Thread(ListenToServer);
                    _shouldListenToServer = true;
                    _listenerThread.Start();
                    OnConnected?.Invoke();
                }
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        public async Task SendPacket(UserPacket packet)
        { 
            var bytes = packet.SerializeToBytes();
            await _client.GetStream().WriteAsync(bytes, 0, bytes.Length);
        }

        public void Disconnect()
        {
            if (!Connected)
                return;
            
            _shouldListenToServer = false;
            _client.Close();
            OnDisconnected?.Invoke();
        }

        private void ListenToServer()
        {
            while (_shouldListenToServer)
            {
                try
                {
                    if (!Connected)
                        return;
                    
                    var data = new byte[1024];

                    var bytesRead = _client.GetStream().Read(data, 0, data.Length);
                    Array.Resize(ref data, bytesRead);

                    var json = Encoding.UTF8.GetString(data);
                    
                    if (json == "") 
                        continue;
                    
                    _siraLog.Info(json);

                    var packet = ServerPacket.Deserialize(json);

                    switch (packet.PacketType)
                    {
                        case ServerPacket.ServerPacketTypes.MatchCreated:
                            OnMatchCreated?.Invoke(packet as MatchCreatedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.OpponentVoted:
                            OnPlayerVoted?.Invoke(packet as PlayerVotedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.RoundStarted:
                            OnRoundStarted?.Invoke(packet as RoundStartedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.MatchResults:
                            OnMatchResults?.Invoke(packet as MatchResultsPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.PrematureMatchEnd:
                            OnPrematureMatchEnd?.Invoke(packet as PrematureMatchEndPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.EventStarted:
                            OnEventStarted?.Invoke(packet as EventStartedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.EventMapSelected:
                            OnEventMapSelected?.Invoke(packet as EventMapSelected);
                            break;
                        case ServerPacket.ServerPacketTypes.EventMatchStarted:
                            OnEventMatchStarted?.Invoke(packet as EventMatchStartedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.EventClosed:
                            OnEventClosed?.Invoke(packet as EventClosedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.EventScoresUpdated:
                            OnEventScoresUpdated?.Invoke(packet as EventScoresUpdated);
                            break;
                        case ServerPacket.ServerPacketTypes.UserDisconnected:
                            OnUserDisconnected?.Invoke(packet as UserDisconnectedPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.RoundResults:
                            OnRoundResults?.Invoke(packet as RoundResultsPacket);
                            break;
                        case ServerPacket.ServerPacketTypes.BeginGameTransition:
                            OnBeginGameTransition?.Invoke(packet as BeginGameTransitionPacket);
                            break;
                        default:
                            throw new Exception("Could not get packet type!");
                    }
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                    Disconnect();
                }
            }
        }

        public void Dispose() => Disconnect();
    }
}