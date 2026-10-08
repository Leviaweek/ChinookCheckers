using System.Runtime.InteropServices;

namespace ChinookCheckers.Engine;

public sealed class KingsRowNative: IDisposable
{
    // The CheckerBoard API exports. The names are case-insensitive, but the engine expects lowercase.
    private const string GetMoveExport = "getmove";
    private const string EngineCommandExport = "enginecommand";
    
    
    // The native library handle. Freed in Dispose.
    private readonly nint _library;
    
    
    // Delegates bound to the native exports.
    private readonly GetMove _getMove;
    private readonly EngineCommand _engineCommand;
    
    // Pointer to an int flag checked by the engine to determine whether it should
    // return its best move immediately. Owned by this instance and freed in Dispose.
    private readonly nint _playNow;
    
    
    // Pointer to the native move buffer. Owned by this instance and freed in Dispose.
    private readonly nint _moveBuffer;
    
    
    // CheckBoard API struct CBmove is ~270 bytes; English checkers ignores it, but give it room anyway.
    private const int MoveBufferBytes = 1024;

    private bool _disposed;
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetMove(
        [In, Out] int[] board,
        int color,
        double maxTime,
        [In, Out] byte[] status,
        nint playNow,
        int info,
        int moreInfo,
        nint move);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EngineCommand([In] byte[] command, [In, Out] byte[] reply);
    
    private KingsRowNative(nint library, GetMove getMove, EngineCommand engineCommand, nint playNow, nint moveBuffer)
    {
        _library = library;
        _getMove = getMove;
        _engineCommand = engineCommand;
        
        _playNow = playNow;
        
        _moveBuffer = moveBuffer;
    }

    public static KingsRowNative Create(string dllPath)
    {
        var library = NativeLibrary.Load(dllPath);

        nint playNow = 0;
        nint moveBuffer = 0;
        
        try
        {
            var getMove = Marshal.GetDelegateForFunctionPointer<GetMove>(NativeLibrary.GetExport(library, GetMoveExport));
            var engineCommand = Marshal.GetDelegateForFunctionPointer<EngineCommand>(NativeLibrary.GetExport(library, EngineCommandExport));
            
            playNow = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(playNow, 0);

            moveBuffer = Marshal.AllocHGlobal(MoveBufferBytes);
            
            // for (var i = 0; i < MoveBufferBytes; i++) Marshal.WriteByte(moveBuffer, i, 0);
            
            return new KingsRowNative(library, getMove, engineCommand, playNow, moveBuffer);
        }
        catch
        {
            if (playNow != 0) Marshal.FreeHGlobal(playNow);
            if (moveBuffer != 0) Marshal.FreeHGlobal(moveBuffer);
            
            NativeLibrary.Free(library);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        Marshal.FreeHGlobal(_playNow);
        Marshal.FreeHGlobal(_moveBuffer);
        NativeLibrary.Free(_library);
    }
}