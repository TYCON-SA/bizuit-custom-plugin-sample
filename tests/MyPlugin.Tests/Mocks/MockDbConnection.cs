using System.Data;
using Moq;

namespace MyPlugin.Tests.Mocks;

/// <summary>
/// Helper class for creating mock database connections for testing.
/// </summary>
public static class MockDbConnection
{
    /// <summary>
    /// Creates a mock IDbConnection.
    /// </summary>
    public static Mock<IDbConnection> Create()
    {
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Closed);
        return mockConnection;
    }

    /// <summary>
    /// Creates a mock IDbConnection that is already open.
    /// </summary>
    public static Mock<IDbConnection> CreateOpen()
    {
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        return mockConnection;
    }

    /// <summary>
    /// Creates a mock IDbCommand.
    /// </summary>
    public static Mock<IDbCommand> CreateCommand()
    {
        var mockCommand = new Mock<IDbCommand>();
        var mockParameters = new Mock<IDataParameterCollection>();

        mockCommand.Setup(c => c.Parameters).Returns(mockParameters.Object);
        mockCommand.Setup(c => c.CreateParameter()).Returns(() =>
        {
            var param = new Mock<IDbDataParameter>();
            return param.Object;
        });

        return mockCommand;
    }
}
