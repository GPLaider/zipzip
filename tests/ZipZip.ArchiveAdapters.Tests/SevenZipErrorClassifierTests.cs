using ZipZip.ArchiveAdapters.SevenZip;

namespace ZipZip.ArchiveAdapters.Tests;

public sealed class SevenZipErrorClassifierTests
{
    [Theory]
    [InlineData("Wrong password?")]
    [InlineData("Can not open encrypted archive. Wrong password?")]
    [InlineData("암호가 필요한 압축 파일입니다.")]
    [InlineData("잘못된 암호입니다.")]
    [InlineData("헤더 오류: 암호가 올바르지 않습니다.")]
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
