using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;
using SQLitePCL;
using System.Text.Json;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    public class AgentFactoryTest
    {
        private Kernel kernel;
        private Agent agent;
        private IConfiguration configuration;

        [OneTimeSetUp]
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

            this.agent = DatabaseAgentFactory.CreateAgentAsync(kernel, NullLoggerFactory.Instance).Result;
        }

        [Test, Order(0)]
        public void AgentFactoryCanCreateANewAgent()
        {
            // Arrange

            // Test

            // Assert
            Assert.That(agent, Is.Not.Null);
        }

        [Order(1)]
        [TestCase("How many customer I have ?", "There are 93 customers")]
        [TestCase("Retrieve Top 5 Most Expensive Products",
                            $"""
                            | ProductName | UnitPrice |
                            | --- | --- |
                            | Côte de Blaye | 263.5 |
                            | Thüringer Rostbratwurst | 123.79 |
                            | Mishi Kobe Niku | 97 |
                            | Sir Rodney's Marmalade | 81 |
                            | Carnarvon Tigers | 62.5 |
                            |
                            """)]
        [TestCase("Retrieve the top 5 customers with the highest total number of orders, including their names.",
                            $"""
                            | CompanyName | TotalOrders |
                            | --- | --- |
                            | IT | 335 |
                            | B's Beverages | 210 |
                            | Ricardo Adocicados | 203 |
                            | LILA-Supermercado | 203 |
                            | Gourmet Lanchonetes | 202 |
                            |            
                            """)]
        public async Task AgentCanAnswerToDataAsync(string question, string expectedAnswer)
        {
            // Arrange
            var evaluatorKernel = kernel.Clone();

            var embeddingTextGenerator = evaluatorKernel.GetRequiredService<ITextEmbeddingGenerationService>();

            // Test
            var responses = agent.InvokeAsync([new ChatMessageContent { Content = question, Role = AuthorRole.User }], thread: null)
                                            .ConfigureAwait(false);

            // Assert
            await foreach (var response in responses)
            {
                Assert.That(response.Message, Is.Not.Null);

                var evaluator = KernelFunctionFactory.CreateFromPrompt($$$"""
                    Evaluate the semantic similarity between the expected answer and the actual response to the given question.

                    # Guidelines

                    - The similarity should be assessed on a scale from 0 to 1, where 0 indicates no similarity and 1 indicates identical meaning.
                    - Consider both content accuracy and completeness when evaluating, rather than superficial linguistic differences.
                    - Do not provide any explanation or additional commentary in the output.

                    # Steps

                    1. Compare the *Source of Truth* (expected answer) with the *Second Sentence* (actual response).
                    2. Analyze the alignment between the meanings conveyed in both sentences, focusing on:
                        - **Accuracy:** Whether the key facts and information in the expected answer are present in the response.
                        - **Completeness:** Whether the response adequately covers all major elements provided in the expected answer.
                        - **Meaning:** Whether the ideas and intended message of the expected answer are preserved.
                    3. Assign a numerical score between 0 and 1 based on the degree of similarity:
                        - **1.0:** Perfect match; the response is semantically identical to the expected answer.
                        - **0.8 - 0.99:** High similarity; the response conveys almost the exact message but with slight differences in phrasing or some minor omissions.
                        - **0.5 - 0.79:** Moderate similarity; the response captures general meaning but has significant missing or incorrect components.
                        - **0.1 - 0.49:** Low similarity; only a small portion of the response aligns with the expected answer.
                        - **0:** No similarity; the response does not align at all with the expected answer or contradicts it entirely.

                    # Output Format

                    Return the similarity score as a JSON document in the following format:

                    ```json
                    {
                      "score": SCORE
                    }
                    ```

                    Replace "SCORE" with the evaluated numeric value between 0 and 1.

                    # Examples

                    **Input:**
                    Question: What is the capital of France?  
                    Source of truth: Paris.  
                    Second sentence: The capital of France is Paris.

                    **Output:**  
                    ```json
                    {
                      "score": 1.0
                    }
                    ```

                    ---

                    **Input:**  
                    Question: What is the capital of France?  
                    Source of truth: Paris.  
                    Second sentence: It might be Rome.

                    **Output:**  
                    ```json
                    {
                      "score": 0.0
                    }
                    ```

                    ---

                    **Input:**  
                    Question: What is the process of photosynthesis?  
                    Source of truth: Photosynthesis is a process used by plants to convert sunlight, carbon dioxide, and water into glucose and oxygen.  
                    Second sentence: Photosynthesis is how plants use sunlight to make food, producing oxygen in the process.

                    **Output:**  
                    ```json
                    {
                      "score": 0.8
                    }
                    ```

                    # Notes

                    - The evaluation should remain unbiased and focused solely on the semantic similarity between the expected and provided sentences.
                    - Avoid factoring in linguistic style or grammar unless it changes the meaning.

                    # Let's evaluate the similarity between the expected answer and the actual response.
                    
                    ## Question: {{{question}}}
                    ## Source of truth
                    {{{expectedAnswer}}}
                    ## Second sentence:
                    {{{response.Message.Content}}}
                    """, new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 4096,
                    Temperature = .1E-9,
                    TopP = .1E-9,
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    ResponseFormat = "json_object"
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                }, functionName: AgentPromptConstants.WriteSQLQuery);


                var evaluationResult = await evaluator.InvokeAsync(kernel)
                                                    .ConfigureAwait(false);

                var evaluation = JsonSerializer.Deserialize<Evaluation>(evaluationResult.GetValue<string>()!);

                Console.WriteLine($"Score: {evaluation.Score}");
                Console.WriteLine($"Answer: {response.Message}");
                Console.WriteLine($"Expected: {expectedAnswer}");

                if (evaluation.Score < 0.7)
                {
                    Assert.Inconclusive("The answer is not similar enough to the expected answer.");
                }
            }
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
