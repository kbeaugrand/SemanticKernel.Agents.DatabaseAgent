Generate a natural language description explaining the purpose of a database table based on its column names and types.

## Steps

1. **Analyze the Input**: Examine the table's column names, data types, or any other relevant input information.
2. **Infer the Context**:
   - Identify key fields (e.g., primary key, foreign keys).
   - Look for patterns or domain-specific terminologies that suggest the table's purpose.
   - Consider how the columns might relate to one another to form a coherent description.
3. **Generate a Coherent Description**:
   - Synthesize the identified context into a concise, human-readable explanation.
   - Explain how the table might be used or the kind of data it likely stores.
4. **Handle Ambiguities**:
   - If the purpose of the table is not explicit based on column names alone, provide educated guesses or flag uncertainty.
   - Avoid being overly definitive unless there is strong contextual evidence.

## Output Format

The output should be a **short paragraph** in natural language. The description should:
- Begin with a statement clearly indicating the table's likely purpose.
- Optionally elaborate on the relationships or dependencies between columns for additional clarity.
- Avoid excessive technical jargon unless essential to the task.

## Example

**Input**:  
```
Table Definition:  
- id: INTEGER (Primary Key)  
- customer_name: VARCHAR(255)  
- email: VARCHAR(255)  
- phone_number: VARCHAR(20)  
- created_at: DATETIME  
- updated_at: DATETIME  
```

**Output**:  
This table appears to store information about customers. It includes columns for a unique customer identifier (`id`), customer contact details such as name, email, and phone number, as well as timestamps (`created_at` and `updated_at`) to track when records are created and modified. This table may be used to manage customer-related data in an application.

---

If the context or purpose is unclear, explicitly note this in the response.

# Let's Practice!

**Input**:  
```
Table Definition:  
{{$tableDefinition}}
```
**Output**: 