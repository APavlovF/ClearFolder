using ClearFolder;

namespace TestApp;

[TestClass]
public class FileSizeExtensionsTests
{
    [DataTestMethod]
    [DataRow(0L, "0 B")]
    [DataRow(500L, "500 B")]
    [DataRow(1024L, "1.00 KB")]
    [DataRow(1048576L, "1.00 MB")]
    [DataRow(1073741824L, "1.00 GB")]
    public void FormatFileSize_ShouldReturnCorrectHumanReadableString(long bytes, string expectedDisplay)
    {
        // Act
        string result = bytes.FormatFileSize();

        // Assert
        Assert.AreEqual(expectedDisplay, result, $"Failed formatting for {bytes} bytes.");
    }
}