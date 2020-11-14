using Stride.Graphics;
using System;

namespace MultiplayerExample.Engine
{
    internal class HeadlessGraphicsDevice : GraphicsDevice
    {
        public HeadlessGraphicsDevice()
            : base(adapter: GraphicsAdapterFactory.Default, profile: Array.Empty<GraphicsProfile>(), deviceCreationFlags: DeviceCreationFlags.None, windowHandle: null)
        {
        }
    }

    internal class HeadlessGraphicsDeviceService : IGraphicsDeviceService
    {
        private HeadlessGraphicsDevice _graphicsDevice = new HeadlessGraphicsDevice();
        public GraphicsDevice GraphicsDevice => _graphicsDevice;

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }
}
