Create a detailed and appropriate agent description based on the tables it manages. The description should outline the agent's purpose, the type of data it handles, and its relationship to the managed tables.

# Guidelines

- **Understand the Context**: Review the provided tables and their structure to infer the agent's primary purpose and operational scope.
- **Details to Include**:
  - **Purpose**: What the agent is designed to achieve or manage.
  - **Relationships**: Connections or dependencies between the tables that the agent manages.
  - **Functionalities**: Optional section describing the agent’s actions, e.g., data retrieval, updates, analytics.

# Output Format

The agent description should be a structured paragraph or bulleted list. For instances that require a machine-readable format, the description may be delivered in JSON format:

[START OF EXAMPLE]
**Input**:
Tables managed:
1. **Orders**: Tracks customer purchases.
2. **Products**: Contains product catalog data.
3. **Customers**: Stores customer profiles and contact information.

**Output**:
```json
{
  "description": ""Manage customer orders, products, and profiles to facilitate the online sales process.\n### Relationships\nEach order is linked to a corresponding customer and one or more products.\n### Examples of actions supported\nSearch for orders, update product inventory, retrieve and manage customer details.",
	
}
```
[END OF EXAMPLE]

# Notes

- Ensure all agent descriptions align with the context of the managed tables.
- If tables are interdependent (e.g., via foreign keys), clearly articulate the relationships.
- Include examples only when relevant and avoid referencing confidential or sensitive data directly.

# Let's go! 🚀

**Input:**  
Tables managed:
Agent Description: {{$tableDefinitions}}

**Output:**