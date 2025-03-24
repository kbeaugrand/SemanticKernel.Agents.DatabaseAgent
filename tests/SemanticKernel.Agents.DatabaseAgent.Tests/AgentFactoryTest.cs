using KernelMemory.Evaluation.Evaluators;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SQLitePCL;
using System.Data.Common;

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    public class AgentFactoryTest
    {
        private IKernelBuilder kernelBuilder;
        private IConfiguration configuration;
        private IKernelMemory memory;

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

            memory = new KernelMemoryBuilder()
                .WithAzureOpenAITextGeneration(completionConfig)
                .WithAzureOpenAITextEmbeddingGeneration(embeddingsConfig)
                .WithSimpleTextDb(new SimpleTextDbConfig()
                {
                    StorageType = FileSystemTypes.Volatile
                })
                .WithSimpleFileStorage(new SimpleFileStorageConfig()
                {
                    StorageType = FileSystemTypes.Volatile
                })
                .Build();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            kernelBuilder = Kernel
                .CreateBuilder()
                .AddAzureOpenAIChatCompletion(completionConfig.Deployment, completionConfig.Endpoint, completionConfig.APIKey)
                .AddAzureOpenAITextEmbeddingGeneration(embeddingsConfig.Deployment, embeddingsConfig.Endpoint, embeddingsConfig.APIKey);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            kernelBuilder.Services.AddSingleton<DbConnection>((sp) =>
            {
                return new SqliteConnection(configuration.GetConnectionString("DefaultConnection"));
            });
        }

        [Test]
        public async Task AgentFactoryCanCreateANewAgentAsync()
        {
            // Arrange


            // Test
            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernelBuilder.Build(), memory);

            // Assert
            Assert.IsNotNull(agent);
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

            var agent = await DatabaseAgentFactory.CreateAgentAsync(kernelBuilder.Build(), memory);
            var faithfulnessEvaluator = new FaithfulnessEvaluator(evaluatorKernel);
            var answerSimilarityEvaluator = new AnswerSimilarityEvaluator(evaluatorKernel);

            var chatHistory = new ChatHistory(question, AuthorRole.User);

            // Test
            var responses = agent.InvokeAsync(chatHistory)
                                            .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(responses);

            await foreach (var response in responses)
            {
                Assert.IsNotNull(response.Content);

                var score = await answerSimilarityEvaluator.EvaluateAsync(truth: expectedAnswer, response.Content!);

                Assert.IsTrue(score > 0.8);
            }
        }
    }
}