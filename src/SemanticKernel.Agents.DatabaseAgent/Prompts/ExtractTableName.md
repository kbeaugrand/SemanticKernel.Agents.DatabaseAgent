You are an expert in {{providerName}} query formatting. Your task is to extract and format a table name from a given input according to the {{providerName}} constraints.

Extract the table name from a given input, ensuring it is properly formatted for use in SQL queries as per the guidelines below:

- **Enclosing Requirement**: If the table name contains spaces, special characters, or starts with a number, enclose it.
- **Preserve Existing Enclosing**: Do not modify names that are already correctly bracketed.
- **Restrict Output**: Do not include any explanation, return only the formatted table name in a JSON object.
- **SQL Compatibility**: Ensure formatting is compatible with {{providerName}}.
- 
# Steps

1. Extract the content inside the input.
2. Inspect the extracted table name for spaces, special characters, or if it begins with a number.
3. Always enclose the table name with the correct quoting style for the specified database provider.
4. Validate against existing enclosing formatting. Do not add enclosing characters if the name is already correctly enclosed.
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

{{ #if (equals providerName "Unknown Provider") }}
**Input**: 
```
| name |
| --- |
| Products Table |
```
**Output**:  
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in brackets for SQL Server compatibility.",
   "tableName": "[Products Table]"
}
```
---
**Input**: 
```
| name |
| --- |
| ValidTableName |
```
**Output**:  
```json
{
   "thinking": "The table name does not require brackets as it is valid without them.",
   "tableName": "[ValidTableName]"
}
```
---
**Input**:
```
| name |
| --- |
| `SpecialName` |
```
**Output**:  
```json
{
   "thinking": "The table name is already correctly bracketed.",
   "tableName": "[SpecialName]"
}
```
{{/if}}
{{ #if (or (or (equals providerName "SQL Server") (equals providerName "SQLite")) (equals providerName "OLE DB")) }}
**Input**: 
```
| name |
| --- |
| Products Table |
```
**Output**: 
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in brackets for {{providerName}} compatibility.",
   "tableName": "[Products Table]"
}
```
---
**Input**: 
```
| name |
| --- |
| ValidTableName |
```
**Output**:  
```json
{
   "thinking": "The table name does not require brackets as it is valid without them.",
   "tableName": "[ValidTableName]"
}
```
---
**Input**:
```
| name |
| --- |
| `SpecialName` |
```
**Output**:  
```json
{
   "thinking": "The table name is already correctly bracketed.",
   "tableName": "[SpecialName]"
}
```
{{/if}}
{{ #if (or (equals providerName "PostgreSQL") (equals providerName "Oracle")) }}
**Input**: 
```
| name |
| --- |
| Products Table |
```
**Output**: 
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in double quotes for {{providerName}} compatibility.",
   "tableName": "\"Products Table\""
}
```
---
**Input**: 
```
| name |
| --- |
| ValidTableName |
```
**Output**:  
```json
{
   "thinking": "The table name does not require brackets as it is valid without them.",
   "tableName": "\"ValidTableName\""
}
```
---
**Input**:
```
| name |
| --- |
| `SpecialName` |
```
**Output**:  
```json
{
   "thinking": "The table name is already correctly bracketed.",
   "tableName": "\"SpecialName\""
}
```
{{/if}}
{{ #if (or (equals providerName "MySQL") (equals providerName "Simba Spark ODBC Driver")) }}
**Input**: 
```
| name |
| --- |
| Products Table |
```
**Output**: 
```json
{
   "thinking": "The table name contains spaces, so it needs to be enclosed in backticks for MySQL compatibility.",
   "tableName": "`Products Table`"
}
```  
---
**Input**: 
```
| name |
| --- |
| ValidTableName |
```
**Output**:  
```json
{
   "thinking": "The table name does not require brackets as it is valid without them.",
   "tableName": "`ValidTableName`"
}
```
---
**Input**:
```
| name |
| --- |
| `SpecialName` |
```
**Output**:  
```json
{
   "thinking": "The table name is already correctly bracketed.",
   "tableName": "`SpecialName`"
}
```
{{/if}}

# Let's do this!

Extract the table name from: 
**Input**: 
```md
{{item}}
```
**Output**: 