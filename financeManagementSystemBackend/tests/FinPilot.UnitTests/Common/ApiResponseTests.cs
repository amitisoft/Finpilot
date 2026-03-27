using FinPilot.Application.Common;
using Xunit;

namespace FinPilot.UnitTests.Common;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessfulResponse()
    {
        var response = ApiResponse<string>.Ok("payload", "done");

        Assert.True(response.Success);
        Assert.Equal("payload", response.Data);
        Assert.Equal("done", response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void Fail_ShouldCreateFailedResponse()
    {
        var response = ApiResponse<string>.Fail("bad request");

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("bad request", response.Message);
    }
}
