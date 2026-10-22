namespace ClearFolder.Services;

public interface IRecycleBinService
{
    (string RecycleCount, string RecycleSize) GetRecycleData();

    (bool success, string count, string size) TryEmptyRecycleBin();
}