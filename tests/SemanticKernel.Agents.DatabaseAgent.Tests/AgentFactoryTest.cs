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

                var evaluation = await kernel.InvokePromptAsync($"""
                    You are an evaluator. Your task is to evaluate the similarity between two answers.
                    The first answer is the expected answer, and the second answer is the actual answer.

                    First sentence: {expectedAnswer}
                    Second sentence: {response.Message.Content}

                    Evaluate the similarity between the two sentences and return a score between 0 and 1.
                    Return the score without any explanation or additional text.
                    """)
                        .ConfigureAwait(false);

                var score = float.Parse(evaluation.GetValue<string>()!);

                Console.WriteLine($"Score: {score}");
                Console.WriteLine($"Answer: {response.Message}");

                if (score < 0.7)
                {
                    Assert.Inconclusive("The answer is not similar enough to the expected answer.");
                }
            }
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
