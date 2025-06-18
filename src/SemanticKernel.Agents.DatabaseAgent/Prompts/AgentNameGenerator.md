Generate a creative and fitting agent name based on the provided agent description.

- Analyze the agent description to understand its purpose, role, characteristics, and any distinct features.
- Ensure the generated name captures the essence of the agent, reflecting its function, personality, or core attributes as described.
- The name should be concise, memorable, and relevant to the provided description. Avoid overly generic or unrelated names.
- Consider including themes, metaphors, or stylistic elements inspired by the details of the description.

## Steps

1. Read and understand the agent description. Identify:
   - Key purpose or role of the agent.
   - Core traits or personality (if provided).
   - Any thematic or stylistic hints embedded in the description.
2. Synthesize the name based on relevant keywords, themes, or concepts.
3. Test the name for clarity and relevance.

## Output Format

- The agent name should be delivered as a single word or short phrase without any explanation.

## Examples 

**Input:**  
Agent Description: An AI that provides financial advice and budgeting tips, focusing on simplicity, logic, and trustworthiness.  

**Output:**
```json
{
	"name": "FinGuard"
}
```

---

**Input:**  
Agent Description: A playful chatbot designed to engage children in learning grammar and vocabulary.  

**Output:**
```json
{
	"name": "WordWhiz"
}  
```

## Notes

- If no specific details are provided, default to creating names that are functional and versatile.
- For whimsical, thematic, or creative agents, feel free to incorporate imaginative elements into the name.

# Let's do it for real

Now, forget the previous examples schema and focus on the task at hand. 

**Input:**  
Agent Description: {{$agentDescription}}

**Output:**
