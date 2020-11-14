using LiteNetLib;
using System;

namespace MultiplayerExample.Network
{
    public enum SendNetworkMessageType : byte
    {
        /// <summary>
        /// Packets may arrive out of order or lost/dropped.
        /// </summary>
        Unreliable,
        /// <summary>
        /// Packets may be lost/dropped, but won't be duplicated, and will arrive in order.
        /// </summary>
        UnreliableSequenced,
        /// <summary>
        /// Packets won't be dropped, won't be duplicated, and can arrive out of order.
        /// </summary>
        ReliableUnordered,
        /// Packets won't be dropped, won't be duplicated, and will arrive in order.
        /// </summary>
        ReliableOrdered,
        /// Packets may be lost/dropped (except the most recent one), won't be duplicated, will arrive in order.
        /// Used for things like Health updates.
        /// </summary>
        ReliableSequenced,
    }

    static class SendNetworkMessageTypeExt
    {
        public static DeliveryMethod ToDeliveryMethod(this SendNetworkMessageType sendType)
        {
            var deliveryMethod = sendType switch
            {
                SendNetworkMessageType.Unreliable => DeliveryMethod.Unreliable,
                SendNetworkMessageType.UnreliableSequenced => DeliveryMethod.Sequenced,
                SendNetworkMessageType.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                SendNetworkMessageType.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                SendNetworkMessageType.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                _ => throw new ArgumentException($"Unknown message type: {sendType}")
            };
            return deliveryMethod;
        }
    }
}
