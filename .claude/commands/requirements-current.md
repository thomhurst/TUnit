# View Current Requirement

Display detailed information about the active requirement.

## Instructions:

1. Read requirements/.current-requirement
2. If no active requirement:
   - Show "No active requirement"
   - Display last 3 completed requirements
   - Exit

3. For active requirement:
   - Load all files from requirement folder
   - Display comprehensive status
   - Show codebase analysis overview
   - Show all questions and answers so far
   - Display context findings if available
   - Indicate current phase and next steps

## File Structure:
- 00-initial-request.md - Original user request
- 01-discovery-questions.md - Context discovery questions
- 02-discovery-answers.md - User's answers
- 03-context-findings.md - AI's codebase analysis
- 04-detail-questions.md - Expert requirements questions
- 05-detail-answers.md - User's detailed answers
- 06-requirements-spec.md - Final requirements document

## Display Format:
```
📋 Current Requirement: [name]
⏱️  Duration: [time since start]
📊 Phase: [Initial Setup/Context Discovery/Targeted Context/Expert Requirements/Complete]
🎯 Progress: [total answered]/[total questions]

📄 Initial Request:
[Show content from 00-initial-request.md]

🏗️ Codebase Overview (Phase 1):
- Architecture: [e.g., React + Node.js + PostgreSQL]
- Main components: [identified services/modules]
- Key patterns: [discovered conventions]

✅ Context Discovery Phase (5/5 complete):
Q1: Will users interact through a visual interface? YES
Q2: Does this need to work on mobile? YES
Q3: Will this handle sensitive data? NO
Q4: Do users have a current workaround? YES (default)
Q5: Will this need offline support? IDK → NO (default)

🔍 Targeted Context Findings:
- Specific files identified: [list key files]
- Similar feature: UserProfile at components/UserProfile.tsx
- Integration points: AuthService, ValidationService
- Technical constraints: Rate limiting required

🎯 Expert Requirements Phase (2/8 answered):
Q1: Use existing ValidationService at services/validation.ts? YES
Q2: Extend UserModel at models/User.ts? YES
Q3: Add new API endpoint to routes/api/v1? [PENDING]
...

📝 Next Action:
- Continue with /requirements-status
- End early with /requirements-end
```

## Important:
- This is view-only (doesn't continue gathering)
- Shows complete history and context
- Use /requirements-status to continue