Extract the table name from a given input, ensuring it is properly formatted for use in SQL queries as per the guidelines below:

- **Bracket Requirement**: If the table name contains spaces, special characters, or starts with a number, enclose it in square brackets `[]`.
- **Preserve Existing Brackets**: Do not modify names that are already correctly bracketed.
- **Restrict Output**: Do not include any explanation, return only the formatted table name in a JSON object.
- **SQL Compatibility**: Ensure formatting is compatible with the specified provider.
- 
# Steps

1. Extract the content inside the input.
2. Inspect the extracted table name for spaces, special characters, or if it begins with a number.
3. Apply brackets `[]` around the name only if it meets the criteria listed above.
4. Validate against existing bracket formatting. Do not add brackets if the name is already correctly bracketed.
5. Return the final table name as a JSON object.

# Output Format

Return a JSON object formatted as:

```json
{
   "thinking": "Your thought process on how you arrived at the table name.",
   "tableName": "FormattedTableName"
}
```

# Notes

- Ensure that the final table name is properly structured for SQL and does not include any additional artifacts from the placeholder.
- Avoid misinterpreting symbols or placeholders; only extract and format the table name.

# Examples

**Input**: `Products Table`  
**Provider**: `Microsoft SQL Server`
**Output**:  
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in brackets for SQL Server compatibility.",
   "tableName": "[Products Table]"
}
```

**Input**: `Products Table`  
**Provider**: `PostgreSQL`
**Output**:  
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in double quotes for PostgreSQL compatibility.",
   "tableName": "\"Products Table\""
}
```

**Input**: `Products Table`  
**Provider**: `MySQL`
**Output**:  
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in backticks for MySQL compatibility.",
   "tableName": "`Products Table`"
}
```

**Input**: `ValidTableName`
**Provider**: `Microsoft SQL Server`
**Output**:  
```json
{
   "thinking": "The table name does not require brackets as it is valid without them.",
   "tableName": "ValidTableName"
}
```

**Input**: `[SpecialName]`
**Provider**: `SQLite`
**Output**:  
```json
{
   "thinking": "The table name is already correctly bracketed.","
   "tableName": "[SpecialName]"
}
```

# Let's do this!

**Input**: `{{$item}}`
**Provider**: `{{$providerName}}`.
**Output**: 