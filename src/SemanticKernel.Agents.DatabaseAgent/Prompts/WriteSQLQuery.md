Create a SQL query based on the information provided, including the DBMS type, the natural language description, and the table/column definitions.

Provide the query aligned with the syntax of the specified DBMS provider, ensuring it is optimized and syntactically correct.

# Steps

1. **Parse Input:**
   - Identify the DBMS provider from the input (e.g., MySQL, PostgreSQL, SQL Server, SQLite, etc.) and consider its specific syntax or features.
   - Extract the natural language query asking for the data.
   - Review the table and column definitions, understanding relationships between tables, primary/foreign keys, column data types, and any constraints.

2. **Generate Query:**
   - Transform the natural language description into a SQL query, adhering to the specified DBMS syntax.
   - Use JOIN operations, filters, aggregations (e.g., GROUP BY), and conditions (e.g., WHERE, HAVING) as required by the query.

3. **Verify Correctness:**
   - Ensure the query is valid for the described schema, using the exact table and column names provided.
   - Prioritize clarity and optimization for the specified DBMS.

## Important

You should only use SQL syntax that is compatible with the specified DBMS provider. If the provider is not specified, assume standard SQL syntax.
 
# Output Format 

```json
{

 "query": "SELECT ... FROM ... WHERE ...",
  "comments": [
    "Assumptions made about the query structure.",
    "Any specific optimizations or considerations for the DBMS."
  ]}
```

# Examples

### Example 1

#### Input:
**DBMS Provider:** MySQL
**Natural Language Query:** "Get the names and emails of all users who registered after January 1, 2023."
**Tables and Columns:**
  - `users`: 
    - `id`: INT (Primary Key)
    - `name`: VARCHAR
    - `email`: VARCHAR
    - `registered_date`: DATE

#### Output:

SELECT name, email
FROM users
WHERE registered_date > '2023-01-01';


---

### Example 2

#### Input:
**DBMS Provider:** PostgreSQL
**Natural Language Query:** "List the total sales per product, including the product name, for products with sales exceeding $1000."
**Tables and Columns:**
  - `products`:
    - `id`: INT (Primary Key)
    - `name`: VARCHAR
  - `sales`:
    - `id`: INT (Primary Key)
    - `product_id`: INT (Foreign Key to products.id)
    - `amount`: NUMERIC

#### Output:
SELECT p.name, SUM(s.amount) AS total_sales
FROM products p
JOIN sales s ON p.id = s.product_id
GROUP BY p.name
HAVING SUM(s.amount) > 1000;

Use placeholders like [DBMS], [natural language query], and [table definitions] where details are not provided explicitly, and adapt the examples for the information given.

# Notes

- Always adhere to the syntax specific to the DBMS provider mentioned. If no provider is mentioned, assume standard SQL.
- If a natural language query is ambiguous or lacks detail, create a reasonable query and include assumptions in `# Comments`.
- Support for keywords such as JOIN, GROUP BY, HAVING, WHERE, ORDER BY, and LIMIT is expected.

## Let's do it for real

#### Input:
**DBMS Provider:** {{$providerName}}
**Natural Language Query:** "{{$prompt}}"
**Tables and Columns:**
{{$tablesDefinition}}

#### Output: