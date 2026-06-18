using System;

namespace Avora.Services.Player
{
    #region Media Key Hook Classes

    public class MediaKeyEventArgs : EventArgs
    {
        public MediaKey Key { get; }

        public MediaKeyEventArgs(MediaKey key)
        {
            Key = key;
        }
    }

    #endregion
}