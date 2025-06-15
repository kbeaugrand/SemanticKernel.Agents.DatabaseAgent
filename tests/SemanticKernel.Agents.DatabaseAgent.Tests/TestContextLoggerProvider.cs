using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    internal class TestContextLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestContextLogger(categoryName);

        public void Dispose() { }

        private class TestContextLogger : ILogger
        {
            private readonly string _categoryName;

            public TestContextLogger(string categoryName) => _categoryName = categoryName;

            public IDisposable BeginScope<TState>(TState state) => null!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = $"{logLevel}: {_categoryName}: {formatter(state, exception)}";
                TestContext.Progress.WriteLine(message);
                if (exception != null)
                    TestContext.Progress.WriteLine(exception);
            }
        }
    }
}
