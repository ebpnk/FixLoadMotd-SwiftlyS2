using System.Runtime.CompilerServices;

namespace FixLoadMotd;

internal partial struct SetStringUserDataRequest_t;

internal unsafe class INetworkStringTable
{
    private readonly nint* _vtable;
    private readonly nint _handle;

    public const uint INVALID_STRING_INDEX = uint.MaxValue;

    public INetworkStringTable(nint pointer)
    {
        _handle = pointer;
        _vtable = *(nint**)pointer;
    }

    public uint AddString(bool bIsServer, string value, ref SetStringUserDataRequest_t pUserData)
    {
        // Use LINUX_OFFSET_PREDICT from Plugin class
        int offset = Plugin.LINUX_OFFSET_PREDICT;
        return ((delegate* unmanaged<nint, bool, string, nint, uint>)_vtable[7 + offset])(_handle, bIsServer, value, (nint)Unsafe.AsPointer(ref pUserData));
    }
}