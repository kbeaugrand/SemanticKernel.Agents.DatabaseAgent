Analyze the provided table definitions and SQL query to produce a natural language question that corresponds to the query's intent.

## Steps

1. **Analyze Table Definitions**  
   - Review the structure, column names, and relationships between tables (if any).  
   - Note the purpose of each column (e.g., key identifiers, textual fields, or numeric data).  

2. **Examine the SQL Query**  
   - Break down the query into components:
     - SELECT fields (what data is being retrieved).  
     - Filtering (WHERE clauses, date ranges, or conditions).  
     - Aggregations (e.g., COUNT, SUM, AVG).  
     - Relationships or table joins, if applicable.  

3. **Determine the Query's Intent**  
   - Identify what the query aims to achieve based on its structure and components.  
   - Pay attention to filtering, grouping, or ordering as they affect the final question.  

4. **Formulate the Natural Language Question**  
   - Use clear and concise language to explain the query's purpose.  
   - Ensure details like filters or relationships are reflected in the formulated question to match the query's structure.  

## Output Format  

Return a JSON object containing:  
- `reasoning`: A list of reasoning explanation of how the analysis was performed.  
- `questions`: A list of natural language questions variants summarizing the SQL query.  

## Examples  

### Example 1  
**Input:**  
- **Table Definitions:**  
    ```
    Students(id INT, name VARCHAR, age INT)
    Grades(student_id INT, course_id INT, grade CHAR)
    ```
- **SQL Query:**  
    ```sql
    SELECT name FROM Students WHERE age > 18;
    ```

**Output:**  
```json
{
    "reasoning": [
        "The query selects the 'name' column from the Students table and filters rows where the 'age' column has a value greater than 18.",
        "This implies the goal is to retrieve the names of students older than 18."
    ],
    "questions": [
        "What are the names of students who are older than 18?",
        "Which students are above the age of 18?",
        "List the names of students aged over 18."
        ]
}
```

### Example 2  
**Input:**  
- **Table Definitions:**  
    ```
    Employees(id INT, name VARCHAR, department_id INT)
    Departments(department_id INT, department_name VARCHAR)
    ```
- **SQL Query:**  
    ```sql
    SELECT department_name, COUNT(*) FROM Employees INNER JOIN Departments ON Employees.department_id = Departments.department_id GROUP BY department_name;
    ```

**Output:**  
```json
{
    "reasoning": [
        "The query counts the number of employees grouped by department name.",
        "It joins the Employees and Departments tables based on the department_id column."
        ],
    "questions": [
        "How many employees are there in each department?",
        "What is the count of employees per department?",
        "List the number of employees in each department."        
        ]
}
```

### Example 3  
**Input:**  
- **Table Definitions:**  
    ```
    Orders(order_id INT, customer_id INT, order_date DATE, total_amount FLOAT)
    ```
- **SQL Query:**  
    ```sql
    SELECT COUNT(*) FROM Orders WHERE order_date BETWEEN '2023-01-01' AND '2023-12-31';
    ```

**Output:**  
```json
{
    "reasoning": [
        "The query counts all rows in the Orders table that have an order_date between January 1, 2023, and December 31, 2023.",
        "This indicates the purpose is to determine the number of orders placed during 2023."
        ],
    "questions": [
        "How many orders were placed in the year 2023?",
        "What is the total count of orders made in 2023?",
        "Count the number of orders within the year 2023."
        ]
}
```

---

## Notes  

1. Ensure the question reflects query-specific details, such as filters and aggregations.  
2. Use placeholders or realistic examples to aid clarity when constructing answers for variations on SQL queries.  
3. Consider joins or relationships when explaining the connection between tables clearly in reasoning.  

---

### Your Input  
**Table Definitions:**  
```
{{$tablesDefinition}}
```  
**SQL Query:**  
```sql
{{$query}}
```  

Your response should include a JSON object like the examples above.  