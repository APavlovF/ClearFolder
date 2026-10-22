using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClearFolder.Models;

namespace ClearFolder.Services;

public class RecycleBinService : IRecycleBinService
{
    public const string ZeroItems = "0 Items";

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    enum RecycleFlags : uint
    {
        SHRB_NOCONFIRMATION = 0x00000001
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    public (bool success, string count, string size) TryEmptyRecycleBin()
    {
        try
        {
            _ = SHEmptyRecycleBin(IntPtr.Zero, null!, RecycleFlags.SHRB_NOCONFIRMATION);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            
            return (true, "Unknown", "Unknown");
        }
        
        return (true, ZeroItems, "0 B");
    }

    public (string RecycleCount, string RecycleSize) GetRecycleData()
    {
        SHQUERYRBINFO rbInfo = new SHQUERYRBINFO
        {
            cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO))
        };
        int result = SHQueryRecycleBin(null!, ref rbInfo);

        return result == 0
            ? ($"{rbInfo.i64NumItems} Items", Folder.FormatFileSize(rbInfo.i64Size))
            : (ZeroItems, "0 KB");
    }
}