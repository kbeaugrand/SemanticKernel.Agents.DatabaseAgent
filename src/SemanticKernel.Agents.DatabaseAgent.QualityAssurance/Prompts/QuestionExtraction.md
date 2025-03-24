Understand and interpret the provided SQL query and table definitions, then determine the corresponding natural language question being asked by the query.

---

## Steps

1. **Analyze Table Definitions**:
    - Review the provided table definitions, including table names, column names, data types, and any relevant constraints or relationships.
    - Identify key columns and their roles (e.g., primary keys, foreign keys, or commonly useful fields for filtering or aggregation).

2. **Examine the SQL Query**:
    - Parse the SQL query to understand the operations being performed, such as SELECT, JOIN, WHERE filters, GROUP BY clauses, HAVING conditions, and ORDER BY clauses.
    - Focus on the specific columns involved, conditions applied, groupings, and how the data is transformed (e.g., aggregations like SUM, COUNT, AVG).

3. **Determine Intent**:
    - Identify the purpose of the query. For example: Is it retrieving specific records, calculating an aggregate, or analyzing relationships between tables?
    - Match query components (e.g., selected columns, filters, aggregations) to their implied natural language meaning.

4. **Formulate the Question**:
    - Using the above insights, construct a natural language question that clearly encapsulates what the query seeks to achieve.
    - If applicable, consider phrasing the question to include any filters (conditions), groupings, or ordering specified in the query.

---

## Output Format

A short natural language question encapsulating the purpose of the SQL query.

---

## Examples

**Example 1**
- Input:
    - Table Definitions:
        ```
        Students(id INT, name VARCHAR, age INT)
        Grades(student_id INT, course_id INT, grade CHAR)
        ```
    - SQL Query:
        ```sql
        SELECT name FROM Students WHERE age > 18;
        ```
- Output:
    - What are the names of students who are older than 18?

**Example 2**
- Input:
    - Table Definitions:
        ```
        Employees(id INT, name VARCHAR, department_id INT)
        Departments(department_id INT, department_name VARCHAR)
        ```
    - SQL Query:
        ```sql
        SELECT department_name, COUNT(*) FROM Employees INNER JOIN Departments ON Employees.department_id = Departments.department_id GROUP BY department_name;
        ```
- Output:
    - How many employees are there in each department?

**Example 3**  
- Input:  
    - Table Definitions:  
        ```
        Orders(order_id INT, customer_id INT, order_date DATE, total_amount FLOAT)
        ```
    - SQL Query:  
        ```sql
        SELECT COUNT(*) FROM Orders WHERE order_date BETWEEN '2023-01-01' AND '2023-12-31';
        ```  
- Output:  
    - How many orders were placed in the year 2023?  

---

## Notes  

- Ensure that significant query details (filters, aggregations, etc.) are reflected in the generated question.  
- If table relationships or complex joins are involved, structure the question to reflect the data connections accurately.  
- Use example placeholders (e.g., specific dates or column names) when needed to reflect a realistic example scenario.


# Let's Start
Table Definitions:  
    ```
    {{$tablesDefinition}}
    ```
SQL Query:  
    ```sql
    {{$query}}
    ```