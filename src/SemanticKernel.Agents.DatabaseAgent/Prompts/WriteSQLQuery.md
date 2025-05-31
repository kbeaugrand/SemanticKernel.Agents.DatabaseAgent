Generate an SQL query based on the provided DBMS type, natural language query, and table/column definitions.

# Steps

1. **Parse Details**:
   - Identify and format the table/column names from the provided `Tables and Columns` section.
   - Resolve any ambiguities in natural language using the supplied table structures.

2. **DBMS Compatibility**:
   - If a DBMS provider is specified (e.g., MySQL, PostgreSQL, SQL Server), ensure that the query adheres strictly to its supported syntax.
   - If no specific provider is given, assume standard SQL syntax.

3. **Handle Special Table and Column Names**:
   - Enclose table names in brackets `[]` if they contain spaces, special characters, or start with a number.
   - Enclose column names in brackets `[]` if they contain spaces, special characters, or start with a number.

4. **Write the Query**:
   - Translate the natural language into its SQL equivalent, adhering to the DBMS’s rules for joins, filters, aggregations, or other operations.

5. **Optimize and Document**:
   - Add optional comments explaining assumptions or adjustments for performance, based on the natural language query and the DBMS.

# Output Format

The output should be structured as a JSON object:
- **`query`**: The SQL query string, ensuring adherence to DBMS-specific rules.
- **`comments`**: A list of comments explaining:
  1. Any assumptions or constraints applied while translating the natural language query.
  2. Considerations specific to the DBMS type (e.g., syntax adjustments or optimizations).

# Notes

- SQL injection concerns or dynamic parameters are not part of this task but should be considered outside this scope.
- Analyze table/column structures for duplicates, edge cases, or potential joins required by the prompt.

# Examples

**Example 1**:
**DBMS Provider**: MySQL  
**Natural Language Query**: "Find all customers with last names starting with 'S'."  
**Tables and Columns**:
```
Customers: customer_id, first_name, last_name, email
```
Generated:
```json
{
  "comments": [
    "The wildcard 'S%' is used to match last names starting with 'S'.",
    "Query is compatible with MySQL syntax."
  ],
  "query": "SELECT * FROM Customers WHERE last_name LIKE 'S%';"
}
```

**Example 2**:
**DBMS Provider**: SQL Server  
**Natural Language Query**: "Get the total sales by product category."  
**Tables and Columns**:
```
[Products]: product_id, category, price
Sales: sale_id, product_id, quantity
```
Generated:
```json
{  
    "comments": [
    "Assumes 'quantity * price' provides total sales for each product.",
    "Compatible with SQL Server syntax; brackets used for [Products] table."
  ],
  "query": "SELECT p.category, SUM(s.quantity * p.price) AS total_sales FROM [Products] p INNER JOIN Sales s ON p.product_id = s.product_id GROUP BY p.category;"
}
```

**Example 3**:
**DBMS Provider**: MySQL  
**Natural Language Query**: "List all employees who joined after January 1, 2020.""
**Tables and Columns**:
```
Employees: employee_id, first name, last name, join date

```
Generated:
```json
{
  "comments": [
    "The date format is adjusted to MySQL's standard format.",
    "Assumes 'join date' is stored in a DATE type column.",
    "Column names with spaces are enclosed in backticks for MySQL compatibility."
  ],
  "query": "SELECT employee_id, `first name`, `last name` FROM Employees WHERE `join date` > '2020-01-01';"
}
```

## Let's do it for real

#### Input:
**DBMS Provider:** {{providerName}}
**Natural Language Query:** "{{prompt}}"


{{ #if previousAttempt }}
##### Previous Attempt
Here is the previous attempt. Try to improve it or correct any errors:
```sql
{{ previousAttempt }}
```
Error: {{ previousException }}
{{/if}}

[BEGIN SEMANTIC MODEL]
{{tablesDefinition}}
[END SEMANTIC MODEL]

#### Output: