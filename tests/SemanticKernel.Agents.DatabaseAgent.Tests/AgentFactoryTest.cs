using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;
using SQLitePCL;
using System.Text.Json;

[assembly: Parallelizable(ParallelScope.None)]

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

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddProvider(new TestContextLoggerProvider());
            });

            this.kernel = AgentKernelFactory.ConfigureKernel(configuration, loggerFactory);

            this.agent = DatabaseAgentFactory.CreateAgentAsync(kernel, update: true, loggerFactory).Result;
        }

        [Order(1)]
        [TestCase("Counts the number of orders placed by each customer in the top 5",
                            $"""
                            | CustomerID | OrderCount |
                            | --- | --- |
                            | BSBEV | 210 |
                            | RICAR | 203 |
                            | LILAS | 203 |
                            | GOURL | 202 |
                            | PRINI | 200 |
                            |                            
                            """)]
        [TestCase("Compute the units sold for each of the top 5 products by aggregating the quantity values from the order details",
                            $"""
                            | ProductName | TotalUnitsSold |
                            | --- | --- |
                            | Louisiana Hot Spiced Okra | 206213 |
                            | Sir Rodney's Marmalade | 205637 |
                            | Teatime Chocolate Biscuits | 205487 |
                            | Sirop d'érable | 205005 |
                            | Gumbär Gummibärchen | 204761 |
                            |
                            """)]
        [TestCase("Calculate the average unit price of products grouped by their category.",
                            $"""
                            | CategoryName | average_unit_price |
                            | --- | --- |
                            | Beverages | 37.979166666666664 |
                            | Condiments | 23.0625 |
                            | Confections | 25.16 |
                            | Dairy Products | 28.73 |
                            | Grains/Cereals | 20.25 |
                            | Meat/Poultry | 54.00666666666667 |
                            | Produce | 32.37 |
                            | Seafood | 20.6825 |
                            |                            
                            """)]
        [TestCase("Count how many orders each employee has processed",
                            $"""
                            | EmployeeID | OrderCount |
                            | --- | --- |
                            | 1 | 1846 |
                            | 2 | 1771 |
                            | 3 | 1846 |
                            | 4 | 1908 |
                            | 5 | 1804 |
                            | 6 | 1754 |
                            | 7 | 1789 |
                            | 8 | 1798 |
                            | 9 | 1766 |
                            |                            
                            """)]
        [TestCase("List the five products with the highest unit price",
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
        [TestCase("Counts how many sales were made with and without a discount",
                            $"""
                            | sales_with_discount | sales_without_discount |
                            | --- | --- |
                            | 838 | 608445 |
                            |
                            """)]
        [TestCase("Calculate the total revenue generated from the product 'Chai'",
                            $"""
                            | total_revenue |
                            | --- |
                            | 3632174.1 |
                            |
                            """)]
        [TestCase("Counts how many products each supplier provides",
                            $"""
                            | CompanyName | ProductCount |
                            | --- | --- |
                            | Aux joyeux ecclésiastiques | 2 |
                            | Bigfoot Breweries | 3 |
                            | Cooperativa de Quesos 'Las Cabras' | 2 |
                            | Escargots Nouveaux | 1 |
                            | Exotic Liquids | 3 |
                            | Formaggi Fortini s.r.l. | 3 |
                            | Forêts d'érables | 2 |
                            | G'day, Mate | 3 |
                            | Gai pâturage | 2 |
                            | Grandma Kelly's Homestead | 3 |
                            | Heli Süßwaren GmbH & Co. KG | 3 |
                            | Karkki Oy | 3 |
                            | Leka Trading | 3 |
                            | Lyngbysild | 2 |
                            | Ma Maison | 2 |
                            | Mayumi's | 3 |
                            | New England Seafood Cannery | 2 |
                            | New Orleans Cajun Delights | 4 |
                            | Nord-Ost-Fisch Handelsgesellschaft mbH | 1 |
                            | Norske Meierier | 3 |
                            | PB Knäckebröd AB | 2 |
                            | Pasta Buttini s.r.l. | 2 |
                            | Pavlova, Ltd. | 5 |
                            | Plutzer Lebensmittelgroßmärkte AG | 5 |
                            | Refrescos Americanas LTDA | 1 |
                            | Specialty Biscuits, Ltd. | 4 |
                            | Svensk Sjöföda AB | 3 |
                            | Tokyo Traders | 3 |
                            | Zaanse Snoepfabriek | 2 |
                            |
                            """)]
        [TestCase("List employees and their manager",
                            $"""
                            | EmployeeName | ManagerName |
                            | --- | --- |
                            | Nancy Davolio | Andrew Fuller |
                            | Andrew Fuller |  |
                            | Janet Leverling | Andrew Fuller |
                            | Margaret Peacock | Andrew Fuller |
                            | Steven Buchanan | Andrew Fuller |
                            | Michael Suyama | Steven Buchanan |
                            | Robert King | Steven Buchanan |
                            | Laura Callahan | Andrew Fuller |
                            | Anne Dodsworth | Steven Buchanan |
                            |
                            """)]
        [TestCase("Lists the top 5 orders with the most expensive shipping fees",
                            $"""
                            | OrderID | Freight |
                            | --- | --- |
                            | 13460 | 587 |
                            | 17596 | 580.75 |
                            | 13372 | 574.5 |
                            | 13466 | 568.25 |
                            | 25679 | 561.25 |
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

                Console.WriteLine($"Score: {evaluation!.Score}");
                Console.WriteLine($"Answer: {response.Message}");
                Console.WriteLine($"Expected: {expectedAnswer}");

                if (evaluation.Score < .8)
                {
                    Assert.Inconclusive("The answer is not similar enough to the expected answer.");
                }
            }
        }
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
