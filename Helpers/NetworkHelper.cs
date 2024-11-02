// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using Mirror;

namespace LiarsAntiCheat.Helpers
{
    public static class NetworkHelper
    {
        public static void SendTargetCustomRPCInternal(NetworkBehaviour nb, NetworkConnection conn, string functionFullName, int functionHashCode, NetworkWriter writer, int channelId)
        {
            if (!NetworkServer.active)
            {
                LiarsAntiCheatMod.mls.LogError($"TargetRPC {functionFullName} was called on {nb.name} when server not active.");
                return;
            }

            if (!nb.isServer)
            {
                LiarsAntiCheatMod.mls.LogWarning($"TargetRPC {functionFullName} was called on {nb.name} but that object has not been spawned or has been unspawned.");
                return;
            }

            if (conn == null)
            {
                conn = nb.connectionToClient;
            }

            if (conn == null)
            {
                LiarsAntiCheatMod.mls.LogError($"TargetRPC {functionFullName} can't be sent because it was given a null connection. Make sure {nb.name} is owned by a connection, or if you pass a connection manually then make sure it's not null.");
            }
            else if (!(conn is NetworkConnectionToClient))
            {
                LiarsAntiCheatMod.mls.LogError($"TargetRPC {functionFullName} called on {nb.name} requires a NetworkConnectionToClient but was given {conn.GetType().Name}");
            }
            else
            {
                RpcMessage rpcMessage = default(RpcMessage);
                rpcMessage.netId = nb.netId;
                rpcMessage.componentIndex = nb.ComponentIndex;
                rpcMessage.functionHash = (ushort)functionHashCode;
                rpcMessage.payload = writer.ToArraySegment();
                RpcMessage message = rpcMessage;
                conn.Send(message, channelId);
            }
        }
    }
}
