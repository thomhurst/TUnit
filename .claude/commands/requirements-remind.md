# Requirements Gathering Reminder

Quick correction when deviating from requirements gathering rules.

## Aliases:
- /requirements-remind
- /remind  
- /r

## Instructions:

1. Check requirements/.current-requirement
2. If no active requirement:
   - Show "No active requirement gathering session"
   - Exit

3. Display reminder based on current context:

```
🔔 Requirements Gathering Reminder

You are gathering requirements for: [active-requirement]
Current phase: [Initial Setup/Context Discovery/Targeted Context/Expert Requirements]  
Progress: [X/Y questions]

📋 PHASE-SPECIFIC RULES:

Phase 2 - Context Discovery:
- ✅ Ask 5 yes/no questions about the problem space
- ✅ Questions for product managers (no code knowledge required)
- ✅ Focus on user workflows, not technical details
- ✅ Write ALL questions before asking any
- ✅ Record answers ONLY after all questions asked

Phase 3 - Targeted Context (Autonomous):
- ✅ Use RepoPrompt tools to search and read code
- ✅ Analyze similar features and patterns
- ✅ Document findings in context file
- ❌ No user interaction during this phase

Phase 4 - Expert Requirements:
- ✅ Ask 5 detailed yes/no questions
- ✅ Questions as if speaking to PM who knows no code
- ✅ Clarify expected system behavior
- ✅ Reference specific files when relevant
- ✅ Record answers ONLY after all questions asked

🚫 GENERAL RULES:
1. ❌ Don't start coding or implementing
2. ❌ Don't ask open-ended questions
3. ❌ Don't record answers until ALL questions in phase are asked
4. ❌ Don't exceed 5 questions per phase

📍 CURRENT STATE:
- Last question: [Show last question]
- User response: [pending/answered]
- Next action: [Continue with question X of 5]

Please continue with the current question or read the next one from the file.
```

## Common Correction Scenarios:

### Open-ended question asked:
"Let me rephrase as a yes/no question..."

### Multiple questions asked:
"Let me ask one question at a time..."

### Implementation started:
"I apologize. Let me continue with requirements gathering..."

### No default provided:
"Let me add a default for that question..."

## Auto-trigger Patterns:
- Detect code blocks → remind no implementation
- Multiple "?" in response → remind one question
- Response > 100 words → remind to be concise
- Open-ended words ("what", "how") → remind yes/no only