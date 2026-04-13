using ZipZip.ArchiveAdapters.SevenZip;

namespace ZipZip.ArchiveAdapters.Tests;

public sealed class SevenZipErrorClassifierTests
{
    [Theory]
    [InlineData("Wrong password?")]
    [InlineData("Can not open encrypted archive. Wrong password?")]
    [InlineData("ERROR: encrypted.7z : Cannot open encrypted archive. Wrong password?")]
    [InlineData("Headers Error")]
    [InlineData("암호가 필요한 압축 파일입니다.")]
    [InlineData("잘못된 비밀번호입니다.")]
    [InlineData("비밀번호가 필요합니다.")]
    public void RequiresPassword_Returns_True_For_Password_Errors(string message)
    {
        Assert.True(SevenZipErrorClassifier.RequiresPassword(message));
    }

    [Theory]
    [InlineData("파일을 찾을 수 없습니다.")]
    [InlineData("Access is denied.")]
    [InlineData("압축 파일을 열지 못했습니다.")]
    public void RequiresPassword_Returns_False_For_NonPassword_Errors(string message)
    {
        Assert.False(SevenZipErrorClassifier.RequiresPassword(message));
    }
}
