using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;
using SQLitePCL;
using System.Numerics.Tensors;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    public class AgentFactoryTest
    {
        private Kernel kernel;
        private IConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            Batteries.Init();

            configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddUserSecrets<AgentFactoryTest>()
                    .AddEnvironmentVariables()
                    .Build();

            this.kernel = AgentKernelFactory.ConfigureKernel(configuration, NullLoggerFactory.Instance);

            foreach (var kvp in configuration.AsEnumerable())
            {
                Console.WriteLine($"{kvp.Key} = {kvp.Value}");
            }
        }

        [Test]
        public async Task AgentFactoryCanCreateANewAgentAsync()
        {
            // Arrange


            // Test
            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernel, NullLoggerFactory.Instance);

            // Assert
            Assert.That(agent, Is.Not.Null);
        }

        [TestCase("How many customer I have ?", "There are 93 customers")]
        [TestCase("Retrieve Top 5 Most Expensive Products",
                        "| ProductName | UnitPrice |\n" +
                        "| --- | --- |\n" +
                        "| C�te de Blaye | 264 |\n" +
                        "| Th�ringer Rostbratwurst | 124 |\n" +
                        "| Mishi Kobe Niku | 97 |\n" +
                        "| Sir Rodney's Marmalade | 81 |\n" +
                        "| Carnarvon Tigers | 62 |\n" +
                        "|")]
        [TestCase("Retrieve the top 5 customers with the highest total number of orders, including their names.",
                        "| CompanyName | total_orders |\n" +
                        "| --- | --- |\n" +
                        "| B's Beverages | 210 |\n" +
                        "| Ricardo Adocicados | 203 |\n" +
                        "| LILA - Supermercado | 203 |\n" +
                        "| Gourmet Lanchonetes | 202 |\n" +
                        "| Princesa Isabel Vinhos | 200 |\n" +
                        "|")]
        public async Task AgentCanAnswerToDataAsync(string question, string expectedAnswer)
        {
            // Arrange
            var evaluatorKernel = kernel.Clone();

            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernel, NullLoggerFactory.Instance);
            var embeddingTextGenerator = evaluatorKernel.GetRequiredService<ITextEmbeddingGenerationService>();

            // Test
            var responses = agent.InvokeAsync([new ChatMessageContent { Content = question, Role = AuthorRole.User }], thread: null)
                                            .ConfigureAwait(false);

            // Assert
            await foreach (var response in responses)
            {
                Assert.That(response.Message, Is.Not.Null);
                var embeddings = await embeddingTextGenerator.GenerateEmbeddingsAsync([expectedAnswer, response.Message.Content!])
                                                .ConfigureAwait(false);

                var score = TensorPrimitives.CosineSimilarity(embeddings[0].Span, embeddings[1].Span);

                Console.WriteLine($"Score: {score}");
                Console.WriteLine($"Answer: {response.Message}");
                Assert.That(score, Is.GreaterThan(0.7));
            }
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
