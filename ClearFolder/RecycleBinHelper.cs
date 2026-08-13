using System.Runtime.InteropServices;
using ClearFolder.Models;

namespace ClearFolder;

public class RecycleBinHelper
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;      
        public long i64NumItems;  
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    public (string RecycleCount, string RecycleSize) GetRecycleData()
    {
        SHQUERYRBINFO rbInfo = new SHQUERYRBINFO
        {
            cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO))
        };
        int result = SHQueryRecycleBin(null, ref rbInfo);

        return result == 0
            ? ($"{rbInfo.i64NumItems} Items", Folder.FormatFileSize(rbInfo.i64Size))
            : ("0 Items", "0 KB");
    }
}