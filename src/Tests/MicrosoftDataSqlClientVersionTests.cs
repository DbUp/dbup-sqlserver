using System.Reflection;
using Shouldly;
using Xunit;

namespace DbUp.SqlServer.Tests;

public class MicrosoftDataSqlClientVersionTests
{
    [Fact]
    public void Microsoft_Data_SqlClient_Is_Lowest_Supported_Lts()
    {
        var assembly = typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        // https://learn.microsoft.com/en-us/sql/connect/ado-net/sqlclient-driver-support-lifecycle
        version.ShouldNotBeNull();
        version.InformationalVersion.ShouldStartWith("6.1.", customMessage: "We want to stay on 6.1.x as it is the lowest LTS version which is in support (ends 14 Aug, 2028)");
    }

    [Fact]
    public void Microsoft_Data_SqlClient_Is_In_Support()
    {
        // Arrange
        var supportEndDate = new DateTime(2028, 8, 14);
        var currentDate = DateTime.UtcNow;

        // Assert
        currentDate.ShouldBeLessThanOrEqualTo(supportEndDate, 
            $"Microsoft.Data.SqlClient 6.1.x support ends on {supportEndDate:yyyy-MM-dd}. " +
            $"Current date is {currentDate:yyyy-MM-dd}. We should upgrade to the latest lowest lts https://learn.microsoft.com/en-us/sql/connect/ado-net/sqlclient-driver-support-lifecycle.");
    }
}
