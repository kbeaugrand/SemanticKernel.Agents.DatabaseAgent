using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using SQLitePCL;
using System.Data.Common;
using System.Numerics.Tensors;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    public class AgentFactoryTest
    {
        private IKernelBuilder kernelBuilder;
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

            var completionConfig = new AzureOpenAIConfig();
            var embeddingsConfig = new AzureOpenAIConfig();

            configuration.GetSection("AzureOpenAI:Completion").Bind(completionConfig);
            configuration.GetSection("AzureOpenAI:Embeddings").Bind(embeddingsConfig);

            kernelBuilder = Kernel
                    .CreateBuilder()
                    .AddAzureOpenAIChatCompletion(completionConfig.Deployment, completionConfig.Endpoint, completionConfig.APIKey)
                    .AddAzureOpenAITextEmbeddingGeneration(embeddingsConfig.Deployment, embeddingsConfig.Endpoint, embeddingsConfig.APIKey);

            kernelBuilder.AddInMemoryVectorStoreRecordCollection<string, TableDefinitionSnippet>("tables");

            kernelBuilder.Services.AddSingleton<DbConnection>((sp) =>
            {
                return new SqliteConnection(configuration.GetConnectionString("DefaultConnection"));
            });

            kernelBuilder.Services
                            .UseDatabaseAgentQualityAssurance(opts =>
                            {
                                opts.EnableQueryRelevancyFilter = true;
                                opts.QueryRelevancyThreshold = .8f;
                            });
        }

        [Test]
        public async Task AgentFactoryCanCreateANewAgentAsync()
        {
            // Arrange


            // Test
            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernelBuilder.Build());

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
            var evaluatorKernel = kernelBuilder.Build();

            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernelBuilder.Build());
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
                Assert.That(score, Is.GreaterThan(0.8));
            }
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
