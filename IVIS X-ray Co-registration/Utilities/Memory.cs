using System;
using System.Runtime.InteropServices;

namespace IVIS_X_ray_Co_registration.Utilities
{
    public static class Memory
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }
}
