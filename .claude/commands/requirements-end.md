# End Requirements Gathering

Finalize the current requirement gathering session.

## Instructions:

1. Read requirements/.current-requirement
2. If no active requirement:
   - Show "No active requirement to end"
   - Exit

3. Show current status and ask user intent:
   ```
   ⚠️ Ending requirement: [name]
   Current phase: [phase] ([X/Y] complete)
   
   What would you like to do?
   1. Generate spec with current information
   2. Mark as incomplete for later
   3. Cancel and delete
   ```

4. Based on choice:

### Option 1: Generate Spec
- Create 06-requirements-spec.md
- Include all answered questions
- Add defaults for unanswered with "ASSUMED:" prefix
- Generate implementation hints
- Update metadata status to "complete"

### Option 2: Mark Incomplete
- Update metadata status to "incomplete"
- Add "lastUpdated" timestamp
- Create summary of progress
- Note what's still needed

### Option 3: Cancel
- Confirm deletion
- Remove requirement folder
- Clear .current-requirement

## Final Spec Format:
```markdown
# Requirements Specification: [Name]

Generated: [timestamp]
Status: [Complete with X assumptions / Partial]

## Overview
[Problem statement and solution summary]

## Detailed Requirements

### Functional Requirements
[Based on answered questions]

### Technical Requirements
- Affected files: [list with paths]
- New components: [if any]
- Database changes: [if any]

### Assumptions
[List any defaults used for unanswered questions]

### Implementation Notes
[Specific guidance for implementation]

### Acceptance Criteria
[Testable criteria for completion]
```

5. Clear .current-requirement
6. Update requirements/index.md