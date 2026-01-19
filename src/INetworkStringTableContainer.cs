namespace FixLoadMotd;

internal unsafe class INetworkStringTableContainer
{
    private readonly nint* _vtable;
    private readonly nint _handle;

    public INetworkStringTableContainer(nint pointer)
    {
        _handle = pointer;
        if (_handle == nint.Zero)
        {
            throw new Exception("Failed to create INetworkStringTableContainer");
        }

        _vtable = *(nint**)_handle;
    }

    public INetworkStringTable? FindTable(string tableName)
    {
        // Use LINUX_OFFSET_PREDICT from Plugin class
        int offset = Plugin.LINUX_OFFSET_PREDICT;
        nint pTable = ((delegate* unmanaged<nint, string, nint>)_vtable[14 + offset])(_handle, tableName);
        return pTable != nint.Zero ? new INetworkStringTable(pTable) : null;
    }
}