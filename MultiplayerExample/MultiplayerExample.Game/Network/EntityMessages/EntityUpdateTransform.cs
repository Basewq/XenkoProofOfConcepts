using MultiplayerExample.Core;
using MultiplayerExample.Network.NetworkMessages;
using Stride.Core.Mathematics;

namespace MultiplayerExample.Network.EntityMessages
{
    struct EntityUpdateTransform : INetworkMessageArray
    {
        // Header Fields
        public SimulationTickNumber SimulationTickNumber;

        // Array Item Fields
        public SerializableGuid NetworkEntityId;
        public Vector3 Position;
        public Quaternion Rotation;
        public float MoveSpeedDecimalPercentage;
        public Vector3 CurrentMoveInputVelocity;
        public Vector3 PhysicsEngineLinearVelocity;
        public bool IsGrounded;

        public bool TryReadHeader(NetworkMessageReader message, out ushort arraySize)
        {
            //arraySize = 0;

            bool isOk = true
                && message.Read(out arraySize)
                && message.Read(out SimulationTickNumber);

            return isOk;
        }

        public bool TryReadNextArrayItem(NetworkMessageReader message)
        {
            bool isOk = true
            && message.Read(out NetworkEntityId)
            && message.Read(out Position)
            && message.Read(out Rotation)
            && message.Read(out MoveSpeedDecimalPercentage)
            && message.Read(out CurrentMoveInputVelocity)
            && message.Read(out PhysicsEngineLinearVelocity)
            && message.Read(out IsGrounded);

            return isOk;
        }

        public void WriteHeader(NetworkMessageWriter message, ushort arraySize)
        {
            //message.Write((byte)EntityUpdateType.Transform);
            message.Write(arraySize);
            message.Write(SimulationTickNumber);
        }

        public void WriteNextArrayItem(NetworkMessageWriter message)
        {
            message.Write(NetworkEntityId);
            message.Write(Position);
            message.Write(Rotation);
            message.Write(MoveSpeedDecimalPercentage);
            message.Write(CurrentMoveInputVelocity);
            message.Write(PhysicsEngineLinearVelocity);
            message.Write(IsGrounded);
        }
    }
}
