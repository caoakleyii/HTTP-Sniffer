using System.Runtime.InteropServices;

namespace HttpLogger.Models
{
    /// <summary>
    /// Defines a COORD used for interop services.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;

        public COORD(short X, short Y)
        {
            this.X = X;
            this.Y = Y;
        }
    };
}
