using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.MicroThreading;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuCollision
    {
        private static readonly Queue<Channel<BepuContactPoint>> ChannelsPool = new Queue<Channel<BepuContactPoint>>();

        internal BepuCollision()
        {
        }

        public void Initialize(BepuPhysicsComponent colliderA, BepuPhysicsComponent colliderB)
        {
            ColliderA = colliderA;
            ColliderB = colliderB;

            NewContactChannel = ChannelsPool.Count == 0 ? new Channel<BepuContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactUpdateChannel = ChannelsPool.Count == 0 ? new Channel<BepuContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactEndedChannel = ChannelsPool.Count == 0 ? new Channel<BepuContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
        }

        internal void Destroy()
        {
            ColliderA = null;
            ColliderB = null;
            NewContactChannel.Reset();
            ContactUpdateChannel.Reset();
            ContactEndedChannel.Reset();
            ChannelsPool.Enqueue(NewContactChannel);
            ChannelsPool.Enqueue(ContactUpdateChannel);
            ChannelsPool.Enqueue(ContactEndedChannel);
            Contacts.Clear();
        }

        public BepuPhysicsComponent ColliderA { get; private set; }

        public BepuPhysicsComponent ColliderB { get; private set; }

        public HashSet<BepuContactPoint> Contacts = new HashSet<BepuContactPoint>();

        internal Channel<BepuContactPoint> NewContactChannel;

        public ChannelMicroThreadAwaiter<BepuContactPoint> NewContact()
        {
            return NewContactChannel.Receive();
        }

        internal Channel<BepuContactPoint> ContactUpdateChannel;

        public ChannelMicroThreadAwaiter<BepuContactPoint> ContactUpdate()
        {
            return ContactUpdateChannel.Receive();
        }

        internal Channel<BepuContactPoint> ContactEndedChannel;

        public ChannelMicroThreadAwaiter<BepuContactPoint> ContactEnded()
        {
            return ContactEndedChannel.Receive();
        }

        public async Task Ended()
        {
            BepuCollision endCollision;
            do
            {
                endCollision = await ColliderA.CollisionEnded();
            }
            while (!endCollision.Equals(this));
        }

        public override bool Equals(object obj)
        {
            var other = (BepuCollision)obj;
            return other != null && ((other.ColliderA == ColliderA && other.ColliderB == ColliderB) || (other.ColliderB == ColliderA && other.ColliderA == ColliderB));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = ColliderA?.GetHashCode() ?? 0;
                result = (result * 397) ^ (ColliderB?.GetHashCode() ?? 0);
                return result;
            }
        }

        internal bool InternalEquals(BepuPhysicsComponent a, BepuPhysicsComponent b)
        {
            return (ColliderA == a && ColliderB == b) || (ColliderB == a && ColliderA == b);
        }
    }
}
