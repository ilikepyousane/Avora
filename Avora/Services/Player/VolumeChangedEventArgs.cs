using System;

namespace Avora.Services.Player
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public double Volume { get; }
        public bool IsMuted { get; }

        public VolumeChangedEventArgs(double volume, bool isMuted = false)
        {
            Volume = volume;
            IsMuted = isMuted;
        }
    }
}