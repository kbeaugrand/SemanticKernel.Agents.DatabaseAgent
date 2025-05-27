using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SemanticKernel.Agents.DatabaseAgent.Internals
{
    internal static class RetryHelper
    {
        internal static async Task<T> Try<T>(Func<Exception?, Task<T>> func, 
            int count = 3, 
            ILoggerFactory loggerFactory = null!, 
            CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;

            Exception? lastException = null;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    return await func(lastException)
                                .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (i == count - 1)
                    {
                        (loggerFactory ?? NullLoggerFactory.Instance)
                                    .CreateLogger(nameof(DatabaseAgentFactory))
                                        .LogWarning(ex, "Failed to execute the function after {Count} attempts.", count);
                        throw;
                    }

                    lastException = ex;

                    await Task.Delay(200, token)
                                .ConfigureAwait(false);
                }
            }
            throw new InvalidOperationException("Failed to execute the function.");

        }
    }
}
